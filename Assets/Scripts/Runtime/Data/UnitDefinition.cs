using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Authored initial values. Unit position and other mutable state belong elsewhere.</summary>
    [CreateAssetMenu(fileName = "Unit", menuName = "Yesterday Convoy/Unit Definition")]
    public sealed class UnitDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Stable, case-sensitive identifier; do not change after use.")]
        private string unitId;

        [SerializeField] private string displayName;
        [SerializeField, Tooltip("World units per simulation second. Must be finite and greater than zero.")]
        private float moveSpeed = 1f;

        [SerializeField, Tooltip("Defense removed by one entity reaching the gate. Must be greater than zero.")]
        private int gateDamage = 1;

        public string UnitId => unitId;
        public string DisplayName => displayName;
        public float MoveSpeed => moveSpeed;
        public int GateDamage => gateDamage;
    }
}
