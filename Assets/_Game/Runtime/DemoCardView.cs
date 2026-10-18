using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>One equipped card in the bottom screen-space roster. Does not perform deployment transactions.</summary>
    public sealed class DemoCardView : MonoBehaviour
    {
        [SerializeField] private CardDefinition card;
        [SerializeField] private Font font;
        [SerializeField] private SpriteRenderer portrait;
        [SerializeField] private TextMesh title;
        [SerializeField] private TextMesh cost;
        [SerializeField] private TextMesh caption;
        private GUIStyle nameStyle, costStyle, labelStyle;

        public CardDefinition Card => card;

        private void Awake()
        {
            // Retain the previous prefab's serialized references, but avoid a second camera-space card.
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = false;
        }

        // Drawn by InitialDemo after its HUD panels, using the same reference-resolution matrix.
        public void DrawRoster()
        {
            if (card == null || card.Portrait == null) return;
            if (nameStyle == null)
            {
                nameStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = 17, alignment = TextAnchor.MiddleCenter };
                costStyle = new GUIStyle(nameStyle) { fontSize = 26, fontStyle = FontStyle.Bold };
                labelStyle = new GUIStyle(nameStyle) { fontSize = 15, alignment = TextAnchor.MiddleRight };
            }
            GUI.Label(new Rect(966,566,140,25), "出战角色  /  01", labelStyle);
            GUI.Label(new Rect(966,596,140,25), "招募费 " + card.RecruitCost, labelStyle);
            Rect slot = new Rect(1126,556,112,144);
            Fill(slot, new Color(0.48f,0.70f,0.77f));
            Fill(new Rect(slot.x+2,slot.y+2,slot.width-4,slot.height-4),new Color(0.07f,0.09f,0.12f));
            Sprite image=card.Portrait;
            Rect source=image.textureRect;
            // Sample the sprite rect, preserving its aspect rather than stretching the portrait.
            Rect target=new Rect(slot.x+5,slot.y+5,102,111);
            float aspect=source.width/source.height;
            float width=Mathf.Min(target.width,target.height*aspect),height=Mathf.Min(target.height,target.width/aspect);
            GUI.DrawTextureWithTexCoords(new Rect(target.center.x-width*0.5f,target.center.y-height*0.5f,width,height),image.texture,
                new Rect(source.x/image.texture.width,source.y/image.texture.height,source.width/image.texture.width,source.height/image.texture.height));
            Fill(new Rect(slot.x+2,slot.y+116,108,26),new Color(0.16f,0.22f,0.27f));
            GUI.Label(new Rect(slot.x+2,slot.y+116,108,26),card.DisplayName,nameStyle);
            Fill(new Rect(slot.x+77,slot.y+2,33,32),new Color(0.14f,0.18f,0.22f));
            GUI.Label(new Rect(slot.x+77,slot.y+1,33,32),card.RecruitCost.ToString(),costStyle);
        }

        private static void Fill(Rect rect,Color color)
        {
            Color previous=GUI.color;
            GUI.color=color;
            GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color=previous;
        }
    }
}
