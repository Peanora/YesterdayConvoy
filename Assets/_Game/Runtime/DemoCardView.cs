using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Camera-space card, captured by the same camera as the battlefield.</summary>
    public sealed class DemoCardView : MonoBehaviour
    {
        [SerializeField] private CardDefinition card;
        [SerializeField] private Font font;
        [SerializeField] private SpriteRenderer portrait;
        [SerializeField] private TextMesh title;
        [SerializeField] private TextMesh cost;
        [SerializeField] private TextMesh caption;

        public CardDefinition Card => card;

        private void Start()
        {
            portrait.sprite = card.Portrait;
            title.text = card.DisplayName;
            cost.text = card.RecruitCost.ToString();
            caption.text = "招募费 " + card.RecruitCost + "  ·  角色卡";
            font.RequestCharactersInTexture(title.text + cost.text + caption.text, 48, FontStyle.Normal);
            foreach (TextMesh text in new[] { title, cost, caption })
            {
                text.font = font;
                text.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }
        }
    }
}
