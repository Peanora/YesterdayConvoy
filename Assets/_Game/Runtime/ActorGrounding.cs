using UnityEngine;
namespace YesterdayConvoy
{
    // Presentation only: no collider, no combat timers, no changes to the route root.
    [DefaultExecutionOrder(1000)]
    public sealed class ActorGrounding : MonoBehaviour
    {
        static Sprite softShadow;
        SpriteRenderer target,shadow;
        MaterialPropertyBlock properties;
        float height;
        public SpriteRenderer Shadow=>shadow;
        public static void Attach(SpriteRenderer renderer,float bodyHeight=1.25f)
        {
            var view=renderer.GetComponent<ActorGrounding>();if(view==null)view=renderer.gameObject.AddComponent<ActorGrounding>();
            view.target=renderer;view.height=Mathf.Max(.5f,bodyHeight);view.EnsureShadow();
        }
        void EnsureShadow()
        {
            if(shadow!=null)return;
            if(softShadow==null)
            {
                var texture=new Texture2D(64,64,TextureFormat.RGBA32,false){name="Procedural soft contact shadow",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
                for(int y=0;y<64;y++)for(int x=0;x<64;x++)
                {float a=(x-31.5f)/31.5f,b=(y-31.5f)/31.5f;float opacity=Mathf.Clamp01((Mathf.Exp(-3*(a*a+b*b))-.05f)/.95f);texture.SetPixel(x,y,new Color(0,0,0,opacity));}
                texture.Apply(false,true);softShadow=Sprite.Create(texture,new Rect(0,0,64,64),new Vector2(.5f,.5f),64);
            }
            shadow=new GameObject("Actor contact shadow",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            shadow.transform.SetParent(transform,false);shadow.sprite=softShadow;shadow.color=new Color(0,0,0,.45f);
            properties=new MaterialPropertyBlock();
        }
        void LateUpdate()
        {
            if(target==null||shadow==null)return;
            shadow.enabled=target.enabled&&target.color.a>.05f;
            shadow.transform.position=target.transform.position+Vector3.up*.016f;
            shadow.transform.rotation=Quaternion.Euler(90,0,0);shadow.transform.localScale=new Vector3(height*.65f,height*.34f,1);
            shadow.sortingLayerID=target.sortingLayerID;shadow.sortingOrder=target.sortingOrder-1;
            target.GetPropertyBlock(properties);properties.SetColor("_EnvironmentTint",new Color(.88f,.93f,.99f,1));properties.SetFloat("_BodyHeight",height);target.SetPropertyBlock(properties);
        }
    }
}
