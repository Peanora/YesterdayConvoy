using UnityEngine;
namespace YesterdayConvoy
{
    [RequireComponent(typeof(Camera))]
    public sealed class FrostSealScreenEffect:MonoBehaviour
    {
        Material material;
        float age=-1;
        internal void Present(float sample)=>age=sample;
        void OnRenderImage(RenderTexture source,RenderTexture destination)
        {
            if(age<0||age>=.35f){Graphics.Blit(source,destination);return;}
            if(material==null)material=new Material(Resources.Load<Shader>("FrostSealScreen"));
            float strength=Mathf.Clamp01(1-age/.22f);
            material.SetVector("_Frost",new Vector4(Mathf.Clamp01(1-age/.35f),Mathf.Clamp01(1-age/.07f),Mathf.Sin(age*75)*strength*2/1280,Mathf.Cos(age*89)*strength*1.1f/720));
            Graphics.Blit(source,destination,material);
        }
        void OnDisable()=>age=-1;
        void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
