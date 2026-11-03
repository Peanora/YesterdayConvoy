using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Read-only presentation assets. No combat or mutable animation state is stored here.</summary>
    [CreateAssetMenu(menuName="Yesterday Convoy/Talulah Art")]
    public sealed class TalulahArtDefinition : ScriptableObject
    {
        [SerializeField] private Sprite[] parts, fireFrames, impactFrames;
        public IReadOnlyList<Sprite> Parts => parts;
        public IReadOnlyList<Sprite> FireFrames => fireFrames;
        public IReadOnlyList<Sprite> ImpactFrames => impactFrames;
        public bool Complete => parts?.Length==13 && fireFrames?.Length==32 && impactFrames?.Length==16;
    }
}
