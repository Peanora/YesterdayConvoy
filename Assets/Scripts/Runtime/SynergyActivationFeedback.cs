using System;
using System.Collections.Generic;

namespace YesterdayConvoy
{
    /// <summary>Presentation-only upward-threshold observation. It never changes a lineup or a battle.</summary>
    public sealed class SynergyActivationFeedback
    {
        public const float Lifetime = 1.8f;

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
        public Batch Active { get; private set; }
        public int Revision { get; private set; }
        public bool Pending => Active != null;
        public float ActiveAge => Active != null ? Active.Age : 0;

        /// <returns>True only when one or more upward crossings create a new batch.</returns>
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

            bool changed = raised.Count > 0;
            if (changed)
            {
                var added = new List<string>();
                foreach (var row in rows.Values)
                    foreach (string member in row.Members)
                        if (!observedMembers.Contains(member) && !added.Contains(member)) added.Add(member);
                Active = new Batch(++Revision, raised, added);
            }
            else PruneActive(rows, currentMembers);

            // Store the current tier rather than a historical maximum. Removing a member therefore
            // rearms the same threshold; refreshing, combining duplicates or same-tier substitutions do not.
            observedTiers.Clear();
            foreach (var row in rows.Values) observedTiers.Add(row.Name, row.Tier);
            observedMembers.Clear();
            foreach (string member in currentMembers) observedMembers.Add(member);
            return changed;
        }

        void PruneActive(Dictionary<string, Pulse> rows, HashSet<string> currentMembers)
        {
            if (Active == null) return;
            var retained = new List<Pulse>();
            bool changed = false;
            foreach (var pulse in Active.Activations)
            {
                if (!rows.TryGetValue(pulse.Name, out var row) || row.Tier != pulse.Tier)
                {
                    changed = true;
                    continue;
                }
                var members = new List<string>();
                foreach (string member in pulse.Members)
                    if (row.ContainsMember(member)) members.Add(member);
                if (members.Count == 0) { changed = true; continue; }
                if (members.Count != pulse.Members.Count)
                {
                    changed = true;
                    retained.Add(new Pulse(pulse.Name, pulse.Tier, members.ToArray()));
                }
                else retained.Add(pulse);
            }
            if (retained.Count == 0) { Active = null; return; }
            var added = new List<string>();
            foreach (string member in Active.AddedMembers)
                if (currentMembers.Contains(member)) added.Add(member);
            if (changed || added.Count != Active.AddedMembers.Count) Active.Replace(retained, added);
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

        /// <returns>Whether a batch remains active after this presentation step.</returns>
        public bool Advance(float delta, bool paused = false)
        {
            if (Active == null) return false;
            if (paused || delta <= 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return true;
            Active.Age = Math.Min(Lifetime, Active.Age + delta);
            if (Active.Age >= Lifetime) Active = null;
            return Active != null;
        }

        public void Clear(bool resetObserved = true)
        {
            Active = null;
            if (!resetObserved) return;
            observedTiers.Clear();
            observedMembers.Clear();
            // Revision stays monotonic so audio/evidence consumers never reuse a cleared batch's identity.
        }
    }
}
