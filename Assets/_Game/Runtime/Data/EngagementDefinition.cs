using UnityEngine;

namespace YesterdayConvoy
{
    [CreateAssetMenu(fileName = "Engagement", menuName = "Yesterday Convoy/Engagement Definition")]
    public sealed class EngagementDefinition : ScriptableObject
    {
        [SerializeField] private Sprite kroosSprite;
        [SerializeField] private Vector3 defenderPosition = new Vector3(-1, 0.35f, 1);
        [Header("Trial combat values, not original-game balance")]
        [SerializeField] private float unitAttack = 14, unitArmor = 2, unitRange = 3.2f, unitInterval = 1.7f;
        [SerializeField] private float defenderHealth = 180, defenderAttack = 22, defenderArmor = 3, defenderRange = 3.2f, defenderInterval = 1;
        [SerializeField] private int blockCount = 0;
        [SerializeField] private float blockRadius = 0.65f;
        [Header("SP is skill charge, independent from deployment DP")]
        [SerializeField] private float initialSP = 8, requiredSP = 12, spPerSecond = 1;
        [SerializeField] private int skillAmmo = 6;
        [SerializeField] private float skillAttack = 105, skillInterval = 3.2f, skillRange = 3.8f, projectileTime = 0.22f;
        [SerializeField] private bool fireRain;
        [SerializeField] private bool hasSkill = true;
        [SerializeField] private bool melee;
        [Tooltip("Support/non-attacking art sample. Does not disable defender blocking or damage.")]
        [SerializeField] private bool noOrdinaryAttack;
        [SerializeField] private float attackDuration, attackWindup;
        [Header("Independent ordinary attack and death effects; zero keeps legacy behavior")]
        [SerializeField] private CombatDamageType damageType;
        [SerializeField] private float defenderResistance, armorShred, blastAttack, blastRadius, blastDelay, coldDuration;
        [SerializeField] private float frozenAttackMultiplier;
        [SerializeField] private int coldAttackEvery;
        [SerializeField] private float attackColdDuration;
        [SerializeField] private bool frostNova;
        [SerializeField] private float regenerationFraction;
        [SerializeField] private Vector3[] extraDefenders = new Vector3[0];
        public System.Collections.Generic.IReadOnlyList<Vector3> ExtraDefenders => extraDefenders;
        public bool FireRain => fireRain;
        public Sprite KroosSprite => kroosSprite;
        public Vector3 DefenderPosition => defenderPosition;
        public EngagementSettings Snapshot() => new EngagementSettings(unitAttack, unitArmor, unitRange, unitInterval,
            defenderHealth, defenderAttack, defenderArmor, defenderRange, defenderInterval, blockCount, blockRadius,
            initialSP, requiredSP, spPerSecond, skillAmmo, skillAttack, skillInterval, skillRange, projectileTime, fireRain, hasSkill, melee, attackDuration, attackWindup,
            new MonsterEffectSettings(damageType,defenderResistance,armorShred,blastAttack,blastRadius,blastDelay,coldDuration,frozenAttackMultiplier,
                coldAttackEvery,attackColdDuration,frostNova,regenerationFraction), canAttack: !noOrdinaryAttack);
    }
}
