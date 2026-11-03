using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Shifts the rendered world, never the camera transform or the IMGUI HUD.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class FireRainScreenEffect : MonoBehaviour
    {
        private Material material;
        private float warmth,flash;
        private Vector2 offset;
        internal void Present(float age,float strength,float warm)
        {
            warmth=warm;
            float since=age-.18f;
            flash=since>=0 ? Mathf.Exp(-since*9)*strength : 0;
            float pulse=0;
            for(int i=0;i<6;i++) { float elapsed=age-(.18f+i*1.05f); if(elapsed>=0) pulse+=Mathf.Exp(-elapsed*13); }
            float amplitude=5.5f*flash+strength*.65f+pulse*strength;
            offset=new Vector2(amplitude*(Mathf.Sin(age*71)*.7f+Mathf.Sin(age*97)*.3f)/1280,
                amplitude*(Mathf.Sin(age*83+.8f)*.65f+Mathf.Sin(age*49)*.35f)/720);
        }
        private void OnRenderImage(RenderTexture source,RenderTexture destination)
        {
            if(warmth<=0) { Graphics.Blit(source,destination); return; }
            if(material==null) material=new Material(Resources.Load<Shader>("FireRainScreen"));
            material.SetVector("_Fire",new Vector4(warmth,flash,offset.x,offset.y));
            Graphics.Blit(source,destination,material);
        }
        private void OnDisable() { warmth=flash=0; offset=Vector2.zero; }
        private void OnDestroy() { if(material!=null) Destroy(material); }
    }
}
