using System;
using System.Collections.Generic;

namespace YesterdayConvoy
{
    /// <summary>Presentation-only upward-threshold observation. It never changes a lineup or a battle.</summary>
    public sealed class SynergyActivationFeedback
    {
        public const float Lifetime = 1.78f;
        public const float Gap = .27f;
        const double BoundaryTolerance = .0000001;

        public sealed class Pulse
        {
            readonly string[] members;
            public string Name { get; }
            public int Tier { get; }
            public IReadOnlyList<string> Members { get; }

            internal Pulse(string name, int tier, string[] memberSnapshot)
            {
                Name = name;
                Tier = tier;
                members = memberSnapshot;
                Members = Array.AsReadOnly(members);
            }

            public bool ContainsMember(string cardId)
            {
                for (int i = 0; i < members.Length; i++)
                    if (string.Equals(members[i], cardId, StringComparison.Ordinal)) return true;
                return false;
            }
        }

        public sealed class Batch
        {
            public int Sequence { get; }
            public float Age { get; internal set; }
            public float Duration => Lifetime;
            public IReadOnlyList<Pulse> Activations { get; private set; }
            public IReadOnlyList<string> AddedMembers { get; private set; }

            internal Batch(int sequence, List<Pulse> activations, List<string> addedMembers)
            {
                Sequence = sequence;
                Replace(activations, addedMembers);
            }

            internal void Replace(List<Pulse> activations, List<string> addedMembers)
            {
                Activations = Array.AsReadOnly(activations.ToArray());
                AddedMembers = Array.AsReadOnly(addedMembers.ToArray());
            }

            public bool IsAddedMember(string cardId)
            {
                for (int i = 0; i < AddedMembers.Count; i++)
                    if (string.Equals(AddedMembers[i], cardId, StringComparison.Ordinal)) return true;
                return false;
            }
        }

        readonly Dictionary<string, int> observedTiers = new Dictionary<string, int>(StringComparer.Ordinal);
        readonly HashSet<string> observedMembers = new HashSet<string>(StringComparer.Ordinal);
        readonly List<Batch> queued = new List<Batch>();
        static readonly Dictionary<string, int> builtinOrder = CreateBuiltinOrder();
        float gapRemaining;
        public Batch Active { get; private set; }
        public int Revision { get; private set; }
        public int QueuedCount => queued.Count;
        public int PendingCount => (Active != null ? 1 : 0) + queued.Count;
        public bool Pending => Active != null || queued.Count > 0;
        public float ActiveAge => Active != null ? Active.Age : 0;
        public float SecondsToTransition => Active != null ? Math.Max(0, Lifetime - Active.Age) : queued.Count > 0 ? Math.Max(0, gapRemaining) : 0;

        /// <returns>True only when one or more upward crossings append new single-synergy batches.</returns>
        public bool Observe(SynergySummary[] current)
        {
            var rows = new Dictionary<string, Pulse>(StringComparer.Ordinal);
            var currentMembers = new HashSet<string>(StringComparer.Ordinal);
            var raised = new List<Pulse>();
            foreach (var row in current ?? Array.Empty<SynergySummary>())
            {
                if (row == null || string.IsNullOrEmpty(row.Name) || rows.ContainsKey(row.Name)) continue;
                string[] members = SnapshotMembers(row.Members);
                var pulse = new Pulse(row.Name, members.Length > 0 ? Math.Max(0, row.Tier) : 0, members);
                rows.Add(row.Name, pulse);
                foreach (string member in members) currentMembers.Add(member);
                observedTiers.TryGetValue(row.Name, out int previous);
                if (pulse.Tier > 0 && pulse.Tier > previous) raised.Add(pulse);
            }

            bool cancelledActive = Active != null && !PruneBatch(Active, rows);
            if (cancelledActive) Active = null;
            for (int i = queued.Count - 1; i >= 0; i--)
                if (!PruneBatch(queued[i], rows)) queued.RemoveAt(i);

            bool changed = raised.Count > 0;
            if (changed)
            {
                raised.Sort(ComparePulses);
                foreach (var pulse in raised)
                {
                    var added = new List<string>();
                    foreach (string member in pulse.Members)
                        if (!observedMembers.Contains(member)) added.Add(member);
                    queued.Add(new Batch(++Revision, new List<Pulse> { pulse }, added));
                }
            }

            // Later observations append after the existing queue. A still-achieved active threshold
            // keeps its age and sound identity; only an invalidated threshold is interrupted.
            if (Active == null && queued.Count > 0)
            {
                if (cancelledActive) gapRemaining = Math.Max(gapRemaining, Gap);
                if (gapRemaining <= 0) StartNext();
            }
            else if (Active == null) gapRemaining = 0;

            // Store the current tier rather than a historical maximum. Removing a member therefore
            // rearms the same threshold; refreshing, combining duplicates or same-tier substitutions do not.
            observedTiers.Clear();
            foreach (var row in rows.Values) observedTiers.Add(row.Name, row.Tier);
            observedMembers.Clear();
            foreach (string member in currentMembers) observedMembers.Add(member);
            return changed;
        }

        static Dictionary<string, int> CreateBuiltinOrder()
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            // This registration order matches the original nine-name order in SynergySummary.From,
            // before that method sorts its display rows by active status and member count.
            foreach (var entry in SynergyTrialCatalog.Entries) result.Add(entry.Tag, result.Count);
            return result;
        }

        static int ComparePulses(Pulse a, Pulse b)
        {
            int grade = SynergyBadgePresentation.GradeFor(b.Name, b.Tier).CompareTo(SynergyBadgePresentation.GradeFor(a.Name, a.Tier));
            if (grade != 0) return grade;
            int orderA = builtinOrder.TryGetValue(a.Name, out int first) ? first : int.MaxValue;
            int orderB = builtinOrder.TryGetValue(b.Name, out int second) ? second : int.MaxValue;
            int order = orderA.CompareTo(orderB);
            return order != 0 ? order : StringComparer.Ordinal.Compare(a.Name, b.Name);
        }

        static bool PruneBatch(Batch batch, Dictionary<string, Pulse> rows)
        {
            var pulse = batch.Activations[0];
            if (!rows.TryGetValue(pulse.Name, out var row) || row.Tier < pulse.Tier) return false;
            var members = new List<string>();
            foreach (string member in pulse.Members)
                if (row.ContainsMember(member)) members.Add(member);
            if (members.Count == 0) return false;
            var added = new List<string>();
            foreach (string member in batch.AddedMembers)
                if (members.Contains(member)) added.Add(member);
            if (members.Count != pulse.Members.Count || added.Count != batch.AddedMembers.Count)
                batch.Replace(new List<Pulse> { new Pulse(pulse.Name, pulse.Tier, members.ToArray()) }, added);
            return true;
        }

        void StartNext()
        {
            if (queued.Count == 0) return;
            Active = queued[0];
            queued.RemoveAt(0);
            gapRemaining = 0;
        }

        static string[] SnapshotMembers(string[] source)
        {
            if (source == null || source.Length == 0) return Array.Empty<string>();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<string>();
            foreach (string member in source)
                if (!string.IsNullOrEmpty(member) && unique.Add(member)) result.Add(member);
            return result.ToArray();
        }

        /// <returns>Whether a current or queued item remains, including an inter-item gap.</returns>
        public bool Advance(float delta, bool paused = false)
        {
            if (!Pending) return false;
            if (paused || delta <= 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return true;
            double remaining = delta;
            while (Pending)
            {
                if (Active != null)
                {
                    float untilEnd = Math.Max(0, Lifetime - Active.Age);
                    if (remaining + BoundaryTolerance < untilEnd)
                    {
                        Active.Age += (float)remaining;
                        if (Active.Age < Lifetime) return true;
                        // A sub-remaining float step can round Age to the endpoint. Finish that
                        // boundary now rather than exposing an active item with zero time left.
                        remaining = 0;
                    }
                    else remaining = Math.Max(0, remaining - untilEnd);
                    Active.Age = Lifetime;
                    Active = null;
                    if (queued.Count == 0) { gapRemaining = 0; return false; }
                    gapRemaining = Gap;
                }
                if (remaining + BoundaryTolerance < gapRemaining)
                {
                    gapRemaining -= (float)remaining;
                    if (gapRemaining > 0) return true;
                    remaining = 0;
                }
                else remaining = Math.Max(0, remaining - gapRemaining);
                gapRemaining = 0;
                StartNext();
                if (remaining <= 0) return Pending;
            }
            return false;
        }

        public void Clear(bool resetObserved = true)
        {
            Active = null;
            queued.Clear();
            gapRemaining = 0;
            if (!resetObserved) return;
            observedTiers.Clear();
            observedMembers.Clear();
            // Revision stays monotonic so audio/evidence consumers never reuse a cleared batch's identity.
        }
    }
}
