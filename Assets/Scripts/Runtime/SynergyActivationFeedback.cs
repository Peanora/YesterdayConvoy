using System;
using System.Collections.Generic;

namespace YesterdayConvoy
{
    /// <summary>Presentation-only tier-change scheduling. It never changes a lineup or a battle.</summary>
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
        readonly Dictionary<string, int> shownTiers = new Dictionary<string, int>(StringComparer.Ordinal);
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

        public bool HasShown(string name, int tier) => !string.IsNullOrEmpty(name) && tier > 0 &&
            shownTiers.TryGetValue(name, out int shown) && shown == tier;

        /// <returns>True when a real tier change replaces the pending schedule, even if no item remains.</returns>
        public bool Observe(SynergySummary[] current)
        {
            var rows = new Dictionary<string, Pulse>(StringComparer.Ordinal);
            var currentMembers = new HashSet<string>(StringComparer.Ordinal);
            var changedNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in current ?? Array.Empty<SynergySummary>())
            {
                if (row == null || string.IsNullOrEmpty(row.Name) || rows.ContainsKey(row.Name)) continue;
                string[] members = SnapshotMembers(row.Members);
                var pulse = new Pulse(row.Name, members.Length > 0 ? Math.Max(0, row.Tier) : 0, members);
                rows.Add(row.Name, pulse);
                foreach (string member in members) currentMembers.Add(member);
                observedTiers.TryGetValue(row.Name, out int previous);
                if (pulse.Tier != previous) changedNames.Add(pulse.Name);
            }
            foreach (var previous in observedTiers)
                if (previous.Value > 0 && !rows.ContainsKey(previous.Key)) changedNames.Add(previous.Key);

            bool changed = changedNames.Count > 0;
            if (changed)
            {
                Rebuild(rows, changedNames);
            }
            else
            {
                bool cancelledActive = Active != null && !RefreshUnchangedBatch(Active, rows);
                if (cancelledActive) Active = null;
                for (int i = queued.Count - 1; i >= 0; i--)
                    if (!RefreshUnchangedBatch(queued[i], rows)) queued.RemoveAt(i);
                if (Active == null && queued.Count > 0)
                {
                    if (cancelledActive) gapRemaining = Math.Max(gapRemaining, Gap);
                    if (gapRemaining <= 0) StartNext();
                }
                else if (Active == null) gapRemaining = 0;
            }

            // Store current tiers rather than historical maxima. Upgrades, downgrades and
            // reactivation reschedule; unchanged refreshes and duplicate combinations do not.
            observedTiers.Clear();
            foreach (var row in rows.Values) observedTiers.Add(row.Name, row.Tier);
            observedMembers.Clear();
            foreach (string member in currentMembers) observedMembers.Add(member);
            return changed;
        }

        void Rebuild(Dictionary<string, Pulse> rows, HashSet<string> changedNames)
        {
            var candidates = new HashSet<string>(StringComparer.Ordinal);
            var pendingAdded = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            RememberPending(Active, candidates, pendingAdded);
            foreach (var batch in queued) RememberPending(batch, candidates, pendingAdded);
            foreach (string name in changedNames)
            {
                shownTiers.Remove(name);
                if (rows.TryGetValue(name, out var row) && row.Tier > 0) candidates.Add(name);
            }

            var latest = new List<Pulse>();
            foreach (string name in candidates)
                if (rows.TryGetValue(name, out var row) && row.Tier > 0 && !HasShown(name, row.Tier)) latest.Add(row);
            latest.Sort(ComparePulses);

            // A tier transaction replaces the whole schedule immediately. A partially displayed,
            // unchanged trait is already shown and is not replayed after the new higher-priority item.
            Active = null;
            queued.Clear();
            gapRemaining = 0;
            foreach (var pulse in latest)
            {
                pendingAdded.TryGetValue(pulse.Name, out var retainedAdded);
                var added = new List<string>();
                foreach (string member in pulse.Members)
                    if (!observedMembers.Contains(member) || retainedAdded != null && retainedAdded.Contains(member)) added.Add(member);
                queued.Add(new Batch(++Revision, new List<Pulse> { pulse }, added));
            }
            StartNext();
        }

        static void RememberPending(Batch batch, HashSet<string> candidates,
            Dictionary<string, HashSet<string>> pendingAdded)
        {
            if (batch == null) return;
            string name = batch.Activations[0].Name;
            candidates.Add(name);
            if (batch.Age > 0) return;
            if (!pendingAdded.TryGetValue(name, out var added))
            {
                added = new HashSet<string>(StringComparer.Ordinal);
                pendingAdded.Add(name, added);
            }
            foreach (string member in batch.AddedMembers) added.Add(member);
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

        bool RefreshUnchangedBatch(Batch batch, Dictionary<string, Pulse> rows)
        {
            var pulse = batch.Activations[0];
            if (!rows.TryGetValue(pulse.Name, out var row) || row.Tier != pulse.Tier) return false;
            var members = new List<string>();
            if (batch.Age <= 0) members.AddRange(row.Members);
            else foreach (string member in pulse.Members)
                    if (row.ContainsMember(member)) members.Add(member);
            if (members.Count == 0) return false;
            var added = new List<string>();
            foreach (string member in members)
                if (batch.IsAddedMember(member) || batch.Age <= 0 && !observedMembers.Contains(member)) added.Add(member);
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
                    var pulse = Active.Activations[0];
                    // Reaching Age zero after an arrival/modal wait is not a display. Only an
                    // actual positive presentation step consumes this exact tier's one showing.
                    shownTiers[pulse.Name] = pulse.Tier;
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
            shownTiers.Clear();
            // Revision stays monotonic so audio/evidence consumers never reuse a cleared batch's identity.
        }
    }
}
