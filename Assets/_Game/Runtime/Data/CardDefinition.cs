using UnityEngine;

namespace YesterdayConvoy
{
    [CreateAssetMenu(fileName = "Card", menuName = "Yesterday Convoy/Card Definition")]
    public sealed class CardDefinition : ScriptableObject
    {
        [SerializeField] private string cardId;
        [SerializeField] private string displayName;
        [SerializeField, Range(1, 5)] private int recruitCost = 5;
        [SerializeField] private UnitDefinition unit;
        [SerializeField] private Sprite portrait;
        [SerializeField] private Sprite battleSprite;

        public string CardId => cardId;
        public string DisplayName => displayName;
        public int RecruitCost => recruitCost;
        public UnitDefinition Unit => unit;
        public Sprite Portrait => portrait;
        public Sprite BattleSprite => battleSprite;
    }
}
