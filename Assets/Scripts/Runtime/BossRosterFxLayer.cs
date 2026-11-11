using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    // Uses the existing HDR decoder and bounded ArtSequence cache, with independent pooled renderers.
    internal sealed class BossRosterFxLayer:IDisposable
    {
        sealed class Item {internal Transform Basis;internal SpriteRenderer Renderer;internal readonly MaterialPropertyBlock Properties=new MaterialPropertyBlock();}
        readonly List<Item> pool=new List<Item>();readonly Transform parent;readonly Material material;int used;bool disposed;
        internal int VisibleCount=>used;
        internal IEnumerable<SpriteRenderer> Renderers {get{foreach(var item in pool)yield return item.Renderer;}}
        internal BossRosterFxLayer(Transform root)
        {
            parent=root;var shader=Resources.Load<Shader>("TalulahNative");if(shader==null)throw new InvalidOperationException("Missing existing HDR effect decoder.");
            material=new Material(shader){name="Boss roster native HDR effects"};
        }
        internal void Begin(){if(disposed)throw new ObjectDisposedException(nameof(BossRosterFxLayer));used=0;}
        internal bool Show(BossRosterFxArt.Clip clip,double age,Vector3 position,Camera camera,bool flip,bool loop,Vector3 direction,bool orient,int sortingOffset=24,float sourceForward=0)
        {
            if(disposed)throw new ObjectDisposedException(nameof(BossRosterFxLayer));if(camera==null)return false;var sprite=clip.At(age,loop);if(sprite==null)return false;
            if(used==pool.Count)
            {
                var basis=new GameObject("Boss FX attachment").transform;basis.SetParent(parent,false);
                var renderer=new GameObject("Boss FX frame",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();renderer.transform.SetParent(basis,false);renderer.sharedMaterial=material;
                pool.Add(new Item{Basis=basis,Renderer=renderer});
            }
            var view=pool[used++];float roll=0;
            if(orient){float x=Vector3.Dot(direction,camera.transform.right),y=Vector3.Dot(direction,camera.transform.up);if(x*x+y*y>1e-8f)roll=Mathf.Atan2(y,x)*Mathf.Rad2Deg-sourceForward;}
            view.Basis.SetPositionAndRotation(position,camera.transform.rotation*Quaternion.Euler(0,0,roll));view.Basis.localScale=Vector3.one;
            var r=view.Renderer;r.name="Boss FX / "+clip.Key;r.enabled=true;r.sprite=sprite;r.flipX=flip;r.color=Color.white;
            r.transform.localPosition=Vector3.zero;r.transform.localRotation=Quaternion.identity;r.transform.localScale=Vector3.one;
            r.sortingOrder=DemoSpriteVisual.GetSortingOrder(camera,position)+sortingOffset;
            view.Properties.SetFloat("_HdrRange",clip.HdrRange);r.SetPropertyBlock(view.Properties);return true;
        }
        internal bool ShowBeam(BossRosterFxArt.Clip clip,double age,Vector3 start,Vector3 end,Camera camera,float referenceLength,bool loop=false)
        {
            if(camera==null||referenceLength<=0)return false;
            var delta=end-start;float x=Vector3.Dot(delta,camera.transform.right),y=Vector3.Dot(delta,camera.transform.up),length=Mathf.Sqrt(x*x+y*y);
            if(length<1e-5f||!Show(clip,age,start,camera,false,loop,delta,true))return false;
            pool[used-1].Basis.localScale=new Vector3(length/referenceLength,1,1);return true;
        }
        internal void End(){for(int i=used;i<pool.Count;i++)if(pool[i].Renderer!=null)pool[i].Renderer.enabled=false;}
        internal void Clear(){foreach(var item in pool)if(item.Basis!=null){item.Renderer.enabled=false;UnityEngine.Object.Destroy(item.Basis.gameObject);}pool.Clear();used=0;}
        public void Dispose(){if(disposed)return;Clear();UnityEngine.Object.Destroy(material);disposed=true;}
    }
}
