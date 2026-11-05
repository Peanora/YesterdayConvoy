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
            float pulse=0;
            for(int i=0;i<6;i++) { float elapsed=age-(.25f+i); if(elapsed>=0) pulse+=Mathf.Exp(-elapsed*5); }
            flash=Mathf.Min(1,pulse)*strength;
            float amplitude=Mathf.Min(3.2f,2.4f*Mathf.Exp(-age/.23f)+2.8f*pulse)*strength;
            offset=new Vector2(amplitude*Mathf.Sin(age*86)/1280,
                amplitude*.55f*Mathf.Sin(age*119+.6f)/720);
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
