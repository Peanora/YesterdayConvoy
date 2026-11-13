using System;
using System.Collections.Generic;
using UnityEngine;
using Object=UnityEngine.Object;

namespace YesterdayConvoy
{
    // The original live material, width curve and gradient render a real timed
    // path. Engine Time.time never assigns particle/trail ages in this layer.
    internal sealed class OriginalNativeFxTrail:IDisposable
    {
        readonly TrailRenderer source;readonly Transform root;readonly GameObject display;readonly Mesh mesh;readonly MeshRenderer renderer;
        readonly List<OriginalNativeFxPathPoint> history=new List<OriginalNativeFxPathPoint>();
        readonly List<OriginalNativeFxPathPoint> visible=new List<OriginalNativeFxPathPoint>();
        double lastClock=double.NegativeInfinity;
        double lastPathAt=double.NegativeInfinity;int lastPathOrdinal;
        internal float Lifetime=>source.time;
        internal OriginalNativeFxTrail(TrailRenderer source,string path)
        {
            this.source=source;root=source.GetComponentInParent<OriginalNativeFxTag>(true).transform;
            display=new GameObject("Original native trail mesh / "+path,typeof(MeshFilter),typeof(MeshRenderer));display.transform.SetParent(source.transform,false);
            mesh=new Mesh{name="Original timed trail / "+path};mesh.MarkDynamic();display.GetComponent<MeshFilter>().sharedMesh=mesh;renderer=display.GetComponent<MeshRenderer>();
            renderer.sharedMaterials=source.sharedMaterials;renderer.sortingLayerID=source.sortingLayerID;renderer.sortingOrder=source.sortingOrder;
            source.emitting=false;source.Clear();source.enabled=false;
        }
        internal void Present(double clock,IReadOnlyList<OriginalNativeFxPathPoint> path,OriginalNativeFxPose pose,Camera camera,bool emitting=true)
        {
            if(camera==null)return;double life=Math.Max(.001,source.time),cut=clock-life;
            renderer.sortingOrder=source.sortingOrder;
            if(path!=null&&path.Count>0)
            {
                // Once emitted, a trail point is a world-space snapshot. Reusing
                // today's animated/rotated emitter for the entire old path made
                // the bend pull the already emitted tail around with the head.
                Vector3 offset=source.transform.position-pose.Position;double groupAt=double.NaN;int ordinal=0;
                for(int i=0;i<path.Count;i++)
                {
                    var point=path[i];if(point.At>clock+1e-6)break;
                    if(groupAt!=point.At){groupAt=point.At;ordinal=0;}ordinal++;
                    if(point.At<lastPathAt-1e-7||Math.Abs(point.At-lastPathAt)<=1e-7&&ordinal<=lastPathOrdinal)continue;
                    bool corner=point.Turn||i+1<path.Count&&path[i+1].Turn&&Math.Abs(path[i+1].At-point.At)<1e-7;
                    Vector3 direction=point.Direction;
                    if(direction.sqrMagnitude<1e-8f)direction=i>0?point.Position-path[i-1].Position:i+1<path.Count?path[i+1].Position-point.Position:pose.Direction;
                    if(direction.sqrMagnitude<1e-8f)direction=pose.Direction;
                    Vector3 oldDirection=pose.Direction;oldDirection.y=direction.y=0;
                    var rotation=oldDirection.sqrMagnitude>1e-8f&&direction.sqrMagnitude>1e-8f?Quaternion.AngleAxis(Vector3.SignedAngle(oldDirection,direction,Vector3.up),Vector3.up):Quaternion.identity;
                    Vector3 emitter=point.Position+rotation*offset;
                    if(history.Count==0||corner||Vector3.Distance(history[history.Count-1].Position,emitter)>=source.minVertexDistance)
                        history.Add(new OriginalNativeFxPathPoint(emitter,point.At,direction,corner));
                    lastPathAt=point.At;lastPathOrdinal=ordinal;
                }
            }
            else if(emitting&&clock>lastClock+1e-7)
            {
                Vector3 position=source.transform.position;
                if(history.Count==0||Vector3.Distance(history[history.Count-1].Position,position)>=source.minVertexDistance)
                    history.Add(new OriginalNativeFxPathPoint(position,clock));
                // The current head position is rendered even before it is far
                // enough to become a permanent trail vertex.
                else if(history.Count>1&&clock-history[history.Count-1].At>life)history.Add(new OriginalNativeFxPathPoint(position,clock));
            }
            lastClock=clock;
            while(history.Count>1&&history[1].At<=cut)history.RemoveAt(0);
            visible.Clear();
            for(int i=0;i<history.Count;i++)
            {
                var point=history[i];if(point.At<cut)
                {
                    if(i+1>=history.Count)continue;var next=history[i+1];float t=(float)((cut-point.At)/Math.Max(1e-9,next.At-point.At));point=new OriginalNativeFxPathPoint(Vector3.LerpUnclamped(point.Position,next.Position,t),cut);
                }
                if(point.At<=clock+1e-6)visible.Add(point);
            }
            // Keep the current head continuous between the source's .03s history
            // samples, without mutating the previous permanent sample.
            if(emitting&&visible.Count>0&&visible[visible.Count-1].Position!=source.transform.position)visible.Add(new OriginalNativeFxPathPoint(source.transform.position,clock));
            if(visible.Count<2){renderer.enabled=false;mesh.Clear();return;}
            int count=visible.Count;var vertices=new Vector3[count*2];var colors=new Color[count*2];var uv=new Vector2[count*2];var triangles=new int[(count-1)*6];var lengths=new float[count];
            float length=0;for(int i=1;i<count;i++){length+=Vector3.Distance(visible[i-1].Position,visible[i].Position);lengths[i]=length;}
            var width=source.widthCurve;var gradient=source.colorGradient;
            for(int i=0;i<count;i++)
            {
                Vector3 tangent=visible[Math.Min(count-1,i+1)].Position-visible[Math.Max(0,i-1)].Position;
                Vector3 normal=source.alignment==LineAlignment.TransformZ?source.transform.forward:camera.transform.forward;
                Vector3 side=Vector3.Cross(normal,tangent).normalized;
                float age=Mathf.Clamp01((float)((clock-visible[i].At)/life)),half=source.widthMultiplier*width.Evaluate(age)*.5f;
                vertices[i*2]=display.transform.InverseTransformPoint(visible[i].Position-side*half);vertices[i*2+1]=display.transform.InverseTransformPoint(visible[i].Position+side*half);
                colors[i*2]=colors[i*2+1]=gradient.Evaluate(age);
                float u=source.textureMode==LineTextureMode.Tile?lengths[i]:length>1e-6f?lengths[i]/length:0;
                uv[i*2]=new Vector2(u,0);uv[i*2+1]=new Vector2(u,1);
                if(i<count-1){int j=i*6,k=i*2;triangles[j]=k;triangles[j+1]=k+1;triangles[j+2]=k+2;triangles[j+3]=k+1;triangles[j+4]=k+3;triangles[j+5]=k+2;}
            }
            mesh.Clear();mesh.vertices=vertices;mesh.colors=colors;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateBounds();mesh.RecalculateNormals();renderer.enabled=true;
        }
        public void Dispose(){renderer.enabled=false;Object.Destroy(mesh);Object.Destroy(display);history.Clear();visible.Clear();}
    }
}
