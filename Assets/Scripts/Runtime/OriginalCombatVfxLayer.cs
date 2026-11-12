using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy
{
    // One director owns one pool and explicitly supplies simulated age and world attachment.
    // No hit, timer or logical position is created here.
    internal sealed class OriginalCombatVfxLayer:IDisposable
    {
        sealed class View
        {
            internal Transform Basis;internal SpriteRenderer Renderer;
            internal readonly MaterialPropertyBlock Properties=new MaterialPropertyBlock();
        }
        readonly List<View> views=new List<View>();readonly Transform parent;readonly Material material;Material pramanixMaterial;
        int used;bool disposed;
        internal int VisibleCount=>used;
        internal IEnumerable<SpriteRenderer> Renderers {get{foreach(var view in views)yield return view.Renderer;}}
        internal OriginalCombatVfxLayer(Transform root)
        {
            parent=root;var shader=Resources.Load<Shader>("TalulahNative");
            if(shader==null)throw new InvalidOperationException("Missing native baked-effect shader.");
            material=new Material(shader){name="Original combat effects / HDR baked"};
        }
        internal void Begin(){if(disposed)throw new ObjectDisposedException(nameof(OriginalCombatVfxLayer));used=0;}
        View Draw(OriginalCombatVfxArt.Clip clip,double age,Vector3 position,Camera camera,bool flip,int order,bool loop,float roll)
        {
            if(camera==null)return null;var sprite=clip.At(age,loop);if(sprite==null)return null;
            // V5's approved visual foot is .025 above the .72 logical platform.
            // Keep caster-attached planes above its .739 cap without moving combat.
            if(SceneManager.GetActiveScene().name=="Map05HeatExchange"&&
                (clip.Key=="defender/D27/sbell2_skill_03_start_01"||clip.Key=="defender/D27/sbell2_skill_03_buff_01"||clip.Key=="defender/D27/sbell2_skill_03_range"))
                position+=Vector3.up*.025f;
            if(used==views.Count)
            {
                var basis=new GameObject("Original VFX attachment").transform;basis.SetParent(parent,false);
                var renderer=new GameObject("Original VFX frame",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();renderer.transform.SetParent(basis,false);
                renderer.sharedMaterial=material;views.Add(new View{Basis=basis,Renderer=renderer});
            }
            var view=views[used++];var r=view.Renderer;r.name="Original VFX / "+clip.Key;r.enabled=true;r.sprite=sprite;r.flipX=flip;r.color=Color.white;
            if(clip.Key.StartsWith("defender/D27/",StringComparison.Ordinal))
            {
                if(pramanixMaterial==null)pramanixMaterial=new Material(Resources.Load<Shader>("PramanixNative")){name="Original combat effects / HDR baked / exact Pramanix"};
                r.sharedMaterial=pramanixMaterial;
            }
            else r.sharedMaterial=material;
            view.Basis.SetPositionAndRotation(position,camera.transform.rotation*Quaternion.Euler(0,0,roll));view.Basis.localScale=Vector3.one;
            r.transform.localPosition=Vector3.zero;r.transform.localRotation=Quaternion.identity;r.transform.localScale=Vector3.one;
            r.sortingOrder=DemoSpriteVisual.GetSortingOrder(camera,position)+order;
            view.Properties.SetFloat("_HdrRange",clip.HdrRange);view.Properties.SetVector("_PramanixAnchor",position+Vector3.up*.012f);r.SetPropertyBlock(view.Properties);return view;
        }
        internal bool Show(string key,double age,Vector3 position,Camera camera,bool flipX,int sortingOffset,bool loop=false,float rollDegrees=0)
        {if(disposed)throw new ObjectDisposedException(nameof(OriginalCombatVfxLayer));return Draw(OriginalCombatVfxArt.Get(key),age,position,camera,flipX,sortingOffset,loop,rollDegrees)!=null;}
        // Explicit horizontal-ground projection, preserving the billboard's screen-space
        // PPU at this camera pitch. The caller supplies the real ground height; no lift
        // or depth override hides geometry that should occlude the effect.
        internal bool ShowGround(string key,double age,Vector3 position,Camera camera,bool flipX,int sortingOffset,bool loop=false,float rollDegrees=0)
            =>ShowGroundScaled(key,age,position,camera,flipX,sortingOffset,1,loop,rollDegrees);
        // Explicit producer-requested uniform size adaptation. Existing ground calls keep scale=1;
        // this never edits the source PPU/pivot or the size of another pooled effect.
        internal bool ShowGroundScaled(string key,double age,Vector3 position,Camera camera,bool flipX,int sortingOffset,float scale,bool loop=false,float rollDegrees=0)
        {
            if(disposed)throw new ObjectDisposedException(nameof(OriginalCombatVfxLayer));
            if(float.IsNaN(scale)||float.IsInfinity(scale)||scale<=0)throw new ArgumentOutOfRangeException(nameof(scale));
            var view=Draw(OriginalCombatVfxArt.Get(key),age,position,camera,flipX,sortingOffset,loop,0);if(view==null)return false;
            Vector3 forward=Vector3.ProjectOnPlane(camera.transform.up,Vector3.up).normalized;
            float projection=Mathf.Abs(Vector3.Dot(camera.transform.up,forward));
            if(projection<.0001f)throw new InvalidOperationException("A ground effect needs a camera with a downward viewing angle.");
            view.Basis.rotation=Quaternion.LookRotation(-Vector3.up,forward);
            view.Basis.localScale=new Vector3(scale,scale/projection,scale);
            view.Renderer.transform.localRotation=Quaternion.Euler(0,0,rollDegrees);
            return true;
        }
        internal bool ShowOriented(string key,double age,Vector3 position,Camera camera,Vector3 worldDirection,int sortingOffset,bool loop=false)
        {
            if(disposed)throw new ObjectDisposedException(nameof(OriginalCombatVfxLayer));var clip=OriginalCombatVfxArt.Get(key);float roll=0;
            if(camera!=null&&clip.Directional)
            {
                float x=Vector3.Dot(worldDirection,camera.transform.right),y=Vector3.Dot(worldDirection,camera.transform.up);
                if(x*x+y*y>1e-8f)roll=Mathf.Atan2(y,x)*Mathf.Rad2Deg-clip.ForwardDegrees;
            }
            return Draw(clip,age,position,camera,false,sortingOffset,loop,roll)!=null;
        }
        // Explicit beam adaptation: the producer's pivot is its start point, and
        // referenceLength is the captured endpoint distance in the billboard plane.
        // Width/texture are original; projected endpoint length is adapted, never hidden in Show.
        internal bool ShowBeam(string key,double age,Vector3 start,Vector3 end,Camera camera,float referenceLength,int sortingOffset,bool loop=false)
        {
            if(disposed)throw new ObjectDisposedException(nameof(OriginalCombatVfxLayer));
            if(float.IsNaN(referenceLength)||float.IsInfinity(referenceLength)||referenceLength<=0)throw new ArgumentOutOfRangeException(nameof(referenceLength));
            if(camera==null)return false;var clip=OriginalCombatVfxArt.Get(key);
            if(!clip.Directional)throw new InvalidOperationException("A beam needs a calibrated native forward: "+key);
            Vector3 delta=end-start;float x=Vector3.Dot(delta,camera.transform.right),y=Vector3.Dot(delta,camera.transform.up),length=Mathf.Sqrt(x*x+y*y);
            if(length<1e-5f)return false;
            var view=Draw(clip,age,start,camera,false,sortingOffset,loop,Mathf.Atan2(y,x)*Mathf.Rad2Deg);if(view==null)return false;
            view.Basis.localScale=new Vector3(length/referenceLength,1,1);
            view.Renderer.transform.localRotation=Quaternion.Euler(0,0,-clip.ForwardDegrees);return true;
        }
        internal void End(){for(int i=used;i<views.Count;i++)if(views[i].Renderer!=null)views[i].Renderer.enabled=false;}
        internal void Clear()
        {
            foreach(var view in views)if(view.Renderer!=null){view.Renderer.enabled=false;UnityEngine.Object.Destroy(view.Basis.gameObject);}
            views.Clear();used=0;
        }
        public void Dispose(){if(disposed)return;Clear();UnityEngine.Object.Destroy(material);if(pramanixMaterial!=null)UnityEngine.Object.Destroy(pramanixMaterial);disposed=true;}
    }
}
