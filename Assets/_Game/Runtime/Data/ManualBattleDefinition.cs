using System;
using UnityEngine;

namespace YesterdayConvoy
{
    [CreateAssetMenu(fileName = "ManualBattle", menuName = "Yesterday Convoy/Manual Battle Definition")]
    public sealed class ManualBattleDefinition : ScriptableObject
    {
        [SerializeField] private string levelId = "MANUAL_DEMO_01";
        [SerializeField] private CardDefinition card;
        [SerializeField] private EngagementDefinition engagement;
        [Header("Trial values for the first single-card level; not final balance")]
        [SerializeField] private int initialDP = 12;
        [SerializeField] private int initialDefense = 3;
        [SerializeField] private int deploymentCost = 2;
        [SerializeField] private int deploymentLimit = 6;
        [SerializeField] private float deploymentCooldown = 2;
        [SerializeField] private float unitHealth = 100;
        [SerializeField] private float towerRange = 2.8f;
        [SerializeField] private float towerDamage = 16;
        [SerializeField] private float shotInterval = 1;

        public string LevelId => levelId;
        public CardDefinition Card => card;
        public EngagementDefinition Engagement => engagement;
        public ManualBattleSettings Snapshot()
        {
            if (string.IsNullOrWhiteSpace(levelId) || card == null || card.Unit == null || card.Portrait == null || card.BattleSprite == null)
                throw new InvalidOperationException("Manual battle: missing level ID, card, unit or art reference.");
            return new ManualBattleSettings(card.CardId, card.Unit.UnitId, initialDP, initialDefense, deploymentLimit, deploymentCost,
                deploymentCooldown, unitHealth, card.Unit.MoveSpeed, card.Unit.GateDamage, towerRange, towerDamage, shotInterval);
        }
    }
}
