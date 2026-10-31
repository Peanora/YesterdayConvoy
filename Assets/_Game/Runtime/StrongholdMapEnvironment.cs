using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace YesterdayConvoy
{
    public sealed class StrongholdMapEnvironment : MonoBehaviour
    {
        readonly List<Material> materials=new List<Material>();
        readonly List<LineRenderer> preview=new List<LineRenderer>();
        Material floor,side,steel,dark,amber,red,blue,cyan;
        public void Build(DefenderTrialDefinition data)
        {
            floor=Mat(new Color(.43f,.49f,.5f),data.floorTexture);side=Mat(new Color(.18f,.23f,.25f));
            steel=Mat(new Color(.56f,.62f,.61f));dark=Mat(new Color(.055f,.085f,.1f));
            amber=Glow(new Color(1,.54f,.05f));red=Glow(new Color(1,.12f,.16f));blue=Glow(new Color(.1f,.75f,1));cyan=Glow(new Color(.23f,.95f,.85f));
            Box("Surrounding service basin",new Vector3(0,-.9f,0),new Vector3(40,.2f,22),dark);
            for(int row=1;row<=5;row++)for(int col=2;col<=18;col++)
            {
                char tile=StrongholdMapLayout.Tile(col,row);Vector3 p=StrongholdMapLayout.Cell(col,row,0);
                if(tile=='#')
                {
                    Box("Closed bulkhead "+col+","+row,p+Vector3.up*.35f,new Vector3(.99f,.9f,.99f),side);
                    Box("Bulkhead lid",p+Vector3.up*.82f,new Vector3(.97f,.05f,.97f),steel);
                    Box("Bulkhead seam",p+new Vector3(0,.852f,.22f),new Vector3(.75f,.012f,.035f),dark);continue;
                }
                Box("Floor "+col+","+row,p-Vector3.up*.17f,new Vector3(.98f,.34f,.98f),side);
                Box("Tile surface",p,new Vector3(.95f,.035f,.95f),tile=='b'?dark:floor);
                foreach(float x in new[]{-.44f,.44f})Box("Steel seam",p+new Vector3(x,.022f,0),new Vector3(.018f,.012f,.89f),steel);
                if(tile=='a')
                {
                    Box("Active originium warning tile",p+Vector3.up*.03f,new Vector3(.89f,.025f,.89f),dark);
                    for(int n=0;n<5;n++)Box("Amber hazard stripe",p+new Vector3(-.36f+n*.18f,.048f,-.35f),new Vector3(.08f,.015f,.15f),amber);
                    for(int n=0;n<3;n++)
                    {
                        var crystal=Box("Active originium crystal",p+new Vector3(-.2f+n*.2f,.17f,.16f-n*.12f),new Vector3(.14f,.35f+n*.08f,.14f),amber);
                        crystal.localRotation=Quaternion.Euler(12+n*10,30+n*31,15-n*12);
                    }
                }
                if(tile=='I')Gate(p,cyan,false);
                if(tile=='E')Gate(p,blue,true);
                if(tile=='x')for(int n=0;n<5;n++)Box("Drain channel",p+new Vector3(-.3f+n*.15f,.026f,.15f),new Vector3(.045f,.018f,.45f),dark);
            }
            // Authored original barricade locations are shown as fixed routing structures.
            foreach(int col in new[]{3,4,8,12,16,17})
            {
                Vector3 p=StrongholdMapLayout.Cell(col,5,0);
                Box("Routing barricade",p+Vector3.up*.22f,new Vector3(.72f,.44f,.38f),steel);
                Box("Barricade warning",p+new Vector3(0,.25f,-.2f),new Vector3(.6f,.12f,.025f),amber);
            }
            foreach(var lane in data.lanes)Gate(lane.points[0],red,true);
            for(int n=0;n<8;n++)
            {
                float x=-9+n*2.6f;
                Box("Rear industrial wall",new Vector3(x,.5f,4.5f),new Vector3(2.5f,1.7f,1.8f),side);
                Box("Rear light rail",new Vector3(x,.45f,3.58f),new Vector3(2.15f,.06f,.04f),cyan);
                Box("Service fascia",new Vector3(x,-.4f,-3),new Vector3(2.45f,.45f,.5f),steel);
            }
            for(int sideIndex=-1;sideIndex<=1;sideIndex+=2)
            {
                Box("Maintenance side pier",new Vector3(sideIndex*9.5f,-.1f,0),new Vector3(1.3f,.9f,7),side);
                for(int row=0;row<4;row++)Box("Side equipment",new Vector3(sideIndex*9.5f,.65f,-2+row*1.6f),new Vector3(.8f,1,.8f),steel);
            }
        }
        public void ShowLane(DeploymentLane lane)
        {
            foreach(var line in preview)line.enabled=false;if(lane==null)return;
            // The portal connector is deliberately omitted: it is not a walkable bridge.
            for(int i=1;i<lane.Points.Count;i++)
            {
                if(i==7)continue;
                while(preview.Count<i)preview.Add(NewLine(transform,cyan,.045f));
                var line=preview[i-1];line.enabled=true;line.SetPositions(new[]{lane.Points[i-1]+Vector3.up*.08f,lane.Points[i]+Vector3.up*.08f});
            }
        }
        void Gate(Vector3 p,Material color,bool tall)
        {
            float height=tall?.85f:.12f;
            Vector3[] corners={new Vector3(-.43f,.04f,-.43f),new Vector3(.43f,.04f,-.43f),new Vector3(.43f,.04f,.43f),new Vector3(-.43f,.04f,.43f)};
            for(int i=0;i<4;i++)
            {
                var a=p+corners[i];var b=p+corners[(i+1)%4];var line=NewLine(transform,color,.025f);line.SetPositions(new[]{a,b});
                line=NewLine(transform,color,.025f);line.SetPositions(new[]{a+Vector3.up*height,b+Vector3.up*height});
                if(tall){line=NewLine(transform,color,.025f);line.SetPositions(new[]{a,a+Vector3.up*height});}
            }
        }
        static LineRenderer NewLine(Transform parent,Material material,float width)
        {
            var line=new GameObject("Map line").AddComponent<LineRenderer>();line.transform.SetParent(parent,false);line.sharedMaterial=material;
            line.positionCount=2;line.widthMultiplier=width;line.shadowCastingMode=ShadowCastingMode.Off;return line;
        }
        Material Mat(Color color,Texture texture=null){var m=new Material(Shader.Find("Standard")){color=color,mainTexture=texture};m.SetFloat("_Glossiness",.15f);materials.Add(m);return m;}
        Material Glow(Color color){var m=new Material(Shader.Find("Sprites/Default")){color=color};materials.Add(m);return m;}
        Transform Box(string name,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(transform,false);go.transform.localPosition=position;go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=material;Destroy(go.GetComponent<Collider>());return go.transform;
        }
        void OnDestroy(){foreach(var m in materials)Destroy(m);}
    }
}
