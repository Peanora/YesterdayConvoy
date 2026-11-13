using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    // The texture is a native rendering of the six original trail materials,
    // width curves and colour gradients at the original 1.5-unit speed.
    // Only its geometry is rebuilt from the real projectile's timed path.
    internal sealed class ChenDawnDragonVfx:IDisposable
    {
        const string Key="defender/D32/chen3_skill_03_tail_ribbon";
        const float Speed=1.5f,Lifetime=2f;
        sealed class View
        {
            internal GameObject Root;internal Mesh Mesh;internal MeshRenderer Renderer;
            internal readonly MaterialPropertyBlock Properties=new MaterialPropertyBlock();
            internal bool Used;
        }
        readonly Transform parent;
        readonly Dictionary<int,View> views=new Dictionary<int,View>();
        readonly List<double> times=new List<double>();
        Material material;ManualBattle observed;int run=-1;bool disposed;
        public double PendingUntil {get;private set;}=double.NegativeInfinity;
        public int VisibleCount {get;private set;}
        internal ChenDawnDragonVfx(Transform parent){this.parent=parent;}
        internal void Present(ManualBattle battle,Camera camera,double clock)
        {
            if(disposed)throw new ObjectDisposedException(nameof(ChenDawnDragonVfx));
            if(observed!=battle||run!=(battle?.RunId??-1)){Clear();observed=battle;run=battle?.RunId??-1;}
            VisibleCount=0;PendingUntil=double.NegativeInfinity;
            foreach(var v in views.Values)v.Used=false;
            if(battle!=null&&camera!=null&&OriginalCombatVfxArt.TryGet(Key,out var clip))
            {
                foreach(var wave in battle.ChenDawnWaves)
                {
                    if(clock<wave.StartedAt||wave.History.Count<2)continue;
                    var owner=battle.FindDefender(wave.SourceId);
                    bool live=wave.Active&&battle.Phase==BattlePhase.Running&&owner!=null&&owner.Alive&&clock<wave.EndsAt;
                    double end=live?Math.Min(clock,wave.EndsAt):Math.Min(wave.EndedAt,Math.Min(wave.EndsAt,battle.Clock));
                    if(owner!=null&&!owner.Alive)end=Math.Min(end,owner.DeathTime);
                    if(clock>end+Lifetime)continue;
                    var sprite=clip.At(clock-wave.StartedAt,true);if(sprite==null)continue;
                    float width=sprite.rect.width/sprite.pixelsPerUnit,height=sprite.rect.height/sprite.pixelsPerUnit;
                    float left=-sprite.pivot.x/sprite.pixelsPerUnit,right=left+width;
                    double first=Math.Max(wave.StartedAt,Math.Max(clock-Lifetime,clock+left/Speed));
                    double last=Math.Min(end,clock+right/Speed);
                    if(last<=first)continue;
                    times.Clear();times.Add(first);
                    foreach(var point in wave.History)if(point.At>first&&point.At<last)times.Add(point.At);
                    if(times.Count==0||last>times[times.Count-1]+1e-7)times.Add(last);
                    // The source trail emitters are slightly ahead of the projectile
                    // origin. Keep that source positive-X section at the live head.
                    if(live&&right>0)times.Add(clock+right/Speed);
                    if(!views.TryGetValue(wave.Sequence,out var view))
                    {
                        if(material==null)
                        {
                            var shader=Resources.Load<Shader>("ChenDawnTrailNative");
                            if(shader==null)throw new InvalidOperationException("Missing native Chen trail shader.");
                            material=new Material(shader){name="Chen Dawn / original six-layer trail composite"};
                        }
                        var go=new GameObject("Original VFX / "+Key+" / "+wave.Sequence,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
                        view=new View{Root=go,Mesh=new Mesh{name="Chen Dawn / actual projectile history"},Renderer=go.GetComponent<MeshRenderer>()};
                        view.Mesh.MarkDynamic();go.GetComponent<MeshFilter>().sharedMesh=view.Mesh;view.Renderer.sharedMaterial=material;views.Add(wave.Sequence,view);
                    }
                    int count=times.Count;var vertices=new Vector3[count*2];var uv=new Vector2[count*2];var triangles=new int[(count-1)*6];
                    Rect rect=sprite.rect;Vector2 texSize=new Vector2(sprite.texture.width,sprite.texture.height);
                    float bottom=-sprite.pivot.y/sprite.pixelsPerUnit;
                    for(int i=0;i<count;i++)
                    {
                        Vector3 position=PositionAt(wave,times[i],live);
                        Vector3 previous=PositionAt(wave,times[Math.Max(0,i-1)],live),next=PositionAt(wave,times[Math.Min(count-1,i+1)],live);
                        Vector3 tangent=next-previous;if(tangent.sqrMagnitude<1e-8f)tangent=wave.Direction;
                        Vector3 side=Vector3.Cross(camera.transform.forward,tangent).normalized;
                        float x=(float)((times[i]-clock)*Speed),u=Mathf.Clamp01((x-left)/width);
                        vertices[i*2]=view.Root.transform.InverseTransformPoint(position+side*bottom);
                        vertices[i*2+1]=view.Root.transform.InverseTransformPoint(position+side*(bottom+height));
                        uv[i*2]=new Vector2((rect.x+u*rect.width)/texSize.x,rect.y/texSize.y);
                        uv[i*2+1]=new Vector2((rect.x+u*rect.width)/texSize.x,(rect.y+rect.height)/texSize.y);
                        if(i<count-1){int k=i*2,j=i*6;triangles[j]=k;triangles[j+1]=k+1;triangles[j+2]=k+2;triangles[j+3]=k+1;triangles[j+4]=k+3;triangles[j+5]=k+2;}
                    }
                    view.Mesh.Clear();view.Mesh.vertices=vertices;view.Mesh.uv=uv;view.Mesh.triangles=triangles;view.Mesh.RecalculateBounds();
                    view.Properties.SetTexture("_MainTex",sprite.texture);view.Properties.SetFloat("_HdrRange",clip.HdrRange);
                    view.Renderer.SetPropertyBlock(view.Properties);view.Renderer.sortingOrder=DemoSpriteVisual.GetSortingOrder(camera,wave.Position)+33;
                    view.Renderer.enabled=true;view.Used=true;VisibleCount++;
                    PendingUntil=Math.Max(PendingUntil,end+Lifetime);
                }
            }
            foreach(var view in views.Values)if(!view.Used)view.Renderer.enabled=false;
        }
        static Vector3 PositionAt(ChenDawnWaveState wave,double at,bool live)
        {
            var points=wave.History;
            if(at<=points[0].At)return points[0].Position;
            var last=points[points.Count-1];
            if(at>=last.At)return last.Position+(live?wave.Direction*(float)((at-last.At)*Speed):Vector3.zero);
            int lo=0,hi=points.Count-1;
            while(hi-lo>1){int mid=(lo+hi)/2;if(points[mid].At<=at)lo=mid;else hi=mid;}
            var a=points[lo];var b=points[hi];return Vector3.LerpUnclamped(a.Position,b.Position,(float)((at-a.At)/Math.Max(1e-9,b.At-a.At)));
        }
        public void Clear()
        {
            foreach(var view in views.Values){if(view.Renderer!=null)view.Renderer.enabled=false;UnityEngine.Object.Destroy(view.Mesh);UnityEngine.Object.Destroy(view.Root);}
            views.Clear();times.Clear();VisibleCount=0;PendingUntil=double.NegativeInfinity;
        }
        public void Dispose(){if(disposed)return;Clear();if(material!=null)UnityEngine.Object.Destroy(material);disposed=true;}
    }
}
