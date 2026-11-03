using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace YesterdayConvoy
{
    // Authored scene geometry; only temporary route feedback is created during play.
    public sealed class StrongholdMapEnvironment : MonoBehaviour
    {
        [SerializeField] Material amber,cyan;
        [SerializeField] int normalMappedSurfaces;
        readonly List<LineRenderer> preview=new List<LineRenderer>();
        public bool RouteVisible {get;private set;}
        public int NormalMappedSurfaceCount=>normalMappedSurfaces;
        public void Configure(Material warm,Material cool,int normals){amber=warm;cyan=cool;normalMappedSurfaces=normals;}
        public void ShowLane(DeploymentLane lane)
        {
            foreach(var line in preview)line.enabled=false;RouteVisible=lane!=null;if(lane==null)return;
            int portal=StrongholdChallengeLayout.PortalSource(lane.Points);
            // The portal connector is deliberately omitted: it is not a walkable bridge.
            for(int i=1;i<lane.Points.Count;i++)
            {
                if(i==portal+1)continue;
                while(preview.Count<i)preview.Add(NewLine(transform,cyan,.045f));
                var line=preview[i-1];line.enabled=true;line.sharedMaterial=i<=portal?amber:cyan;line.startColor=line.endColor=Color.white;line.SetPositions(new[]{lane.Points[i-1]+Vector3.up*.08f,lane.Points[i]+Vector3.up*.08f});
            }
        }
        static LineRenderer NewLine(Transform parent,Material material,float width)
        {
            var line=new GameObject("Map line").AddComponent<LineRenderer>();line.transform.SetParent(parent,false);line.sharedMaterial=material;
            line.positionCount=2;line.widthMultiplier=width;line.shadowCastingMode=ShadowCastingMode.Off;return line;
        }
    }
}
