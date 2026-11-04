using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    // Read-only presentation of the approved V8 flipbook. Never schedules damage.
    internal sealed class TalulahAttackVfx:IDisposable
    {
        sealed class View
        {
            internal SpriteRenderer Renderer;
            internal readonly MaterialPropertyBlock Properties=new MaterialPropertyBlock();
        }
        readonly Transform parent;
        readonly List<View> views=new List<View>();
        readonly Sprite canvas;
        readonly Material material;
        int used;
        static readonly int Frame=Shader.PropertyToID("_Frame"),Opacity=Shader.PropertyToID("_Opacity");
        public TalulahAttackVfx(Transform root)
        {
            parent=root;
            var texture=Resources.Load<Texture2D>("TalulahAttackFlipbook");
            var shader=Resources.Load<Shader>("TalulahAttackFlipbook");
            if(texture==null||shader==null)throw new InvalidOperationException("Talulah V8 attack resources are missing.");
            material=new Material(shader){name="Talulah V8 painted fire",mainTexture=texture};
            canvas=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),384,0,SpriteMeshType.FullRect);
        }
        static float Smooth(float x)=>Mathf.SmoothStep(0,1,Mathf.Clamp01(x));
        public void Begin()=>used=0;
        public void Present(CombatShot shot,double clock,Camera camera,float sourceRecoveryScale)
        {
            float age=(float)(clock-shot.ImpactAt)*sourceRecoveryScale;
            if(camera==null||shot.Erased||!shot.Applied||age<0||age>.68f)return;
            float frame=age<.045f?age/.045f*3:age<.10f?3+(age-.045f)/.055f*2:
                age<.31f?5+(age-.10f)/.21f*3:age<.54f?8+(age-.31f)/.23f*3:11;
            float opacity=Smooth(age/.015f)*(1-Smooth((age-.54f)/.14f));
            if(opacity<=0)return;
            if(used==views.Count)
            {
                var renderer=new GameObject("Talulah attack / painted fire",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                renderer.transform.SetParent(parent,false);renderer.sprite=canvas;renderer.sharedMaterial=material;
                views.Add(new View{Renderer=renderer});
            }
            var view=views[used++];var r=view.Renderer;r.enabled=true;
            // Source actor faces right, painted fire faces left; mirror anchor and body together.
            float dx=shot.Destination.x-shot.Origin.x;
            if(Mathf.Abs(dx)<.08f)dx=Vector3.Dot(shot.Destination-shot.Origin,camera.transform.right);
            float mirror=dx<0?1:-1;
            // A browser canvas has no floor. Fit the full painted tail above the
            // actor's ground point instead of disabling depth tests to hide clipping.
            r.transform.SetPositionAndRotation(shot.Origin+camera.transform.right*(-.175f*mirror)+
                camera.transform.up*1.50f,camera.transform.rotation);
            r.transform.localScale=new Vector3(mirror*3.70f/canvas.bounds.size.x,
                3.29f/canvas.bounds.size.y,1);
            r.sortingOrder=DemoSpriteVisual.GetSortingOrder(camera,shot.Origin)+8;
            view.Properties.SetFloat(Frame,Mathf.Min(11,frame));view.Properties.SetFloat(Opacity,opacity);
            r.SetPropertyBlock(view.Properties);
        }
        public void End(){for(int i=used;i<views.Count;i++)views[i].Renderer.enabled=false;}
        public void Clear()
        {
            foreach(var view in views)if(view.Renderer!=null){view.Renderer.enabled=false;UnityEngine.Object.Destroy(view.Renderer.gameObject);}
            views.Clear();used=0;
        }
        public void Dispose(){Clear();UnityEngine.Object.Destroy(canvas);UnityEngine.Object.Destroy(material);}
    }
}
