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
        [SerializeField] private TalulahArtDefinition articulatedArt;
        public TalulahArtDefinition ArticulatedArt => articulatedArt;
        [Header("Clock-driven comic flipbook")]
        [SerializeField] private Sprite[] walkFrames, attackFrames, skillFrames, comicImpactFaces;

        public string CardId => cardId;
        public string DisplayName => displayName;
        public int RecruitCost => recruitCost;
        public UnitDefinition Unit => unit;
        public Sprite Portrait => LocalMonsterArt.For(cardId)?.Portrait ?? OriginalSlugArt.For(cardId)?.Portrait ?? portrait;
        public Sprite BattleSprite => LocalMonsterArt.For(cardId)?["idle"].Frames[0] ?? OriginalSlugArt.For(cardId)?["idle"].Frames[0] ?? battleSprite;
        public System.Collections.Generic.IReadOnlyList<Sprite> WalkFrames => walkFrames;
        public System.Collections.Generic.IReadOnlyList<Sprite> AttackFrames => attackFrames;
        public System.Collections.Generic.IReadOnlyList<Sprite> SkillFrames => skillFrames;
        public System.Collections.Generic.IReadOnlyList<Sprite> ComicImpactFaces => comicImpactFaces;
        public bool HasFrameAnimation => LocalMonsterArt.For(cardId)!=null || OriginalSlugArt.For(cardId)!=null || (walkFrames?.Length>0 && attackFrames?.Length>0);
    }
}
