using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Transient authored industrial trial; restores all borrowed scene transforms on exit.</summary>
    public sealed class DefenseTrialMap : MonoBehaviour
    {
        private InitialDemo scene;
        private GameObject original;
        private bool originalActive;
        private bool restored;
        private Camera borrowedCamera;
        private Vector3 red, blue, cameraPosition;
        private Material[] materials;
        public void Initialize(InitialDemo bindings,DefenderTrialDefinition data)
        {
            scene=bindings; original=GameObject.Find("Industrial Map");
            if(original!=null) { originalActive=original.activeSelf; original.SetActive(false); }
            borrowedCamera=Camera.main;
            red=scene.RedGate.position; blue=scene.BlueGate.position; cameraPosition=borrowedCamera.transform.position;
            scene.RedGate.position=data.route[0]; scene.BlueGate.position=data.route[data.route.Length-1];
            borrowedCamera.transform.position-=borrowedCamera.transform.forward*2.8f;
            materials=(Material[])data.surfaces.Clone();
            materials[0]=new Material(materials[0]); materials[0].color*=1.12f;
            materials[1]=new Material(materials[1]); materials[1].color*=.72f;
            Box("Deep canal",new Vector3(0,-.8f,0),new Vector3(40,.1f,40),4);
            Box("Concrete foundation",new Vector3(0,-.37f,0),new Vector3(15.4f,.5f,9.5f),2);
            var raised=new HashSet<Vector2Int>();
            foreach(var d in data.defenders) if(d.position.y>.2f) raised.Add(new Vector2Int(Mathf.RoundToInt(d.position.x),Mathf.RoundToInt(d.position.z)));
            for(int x=-6;x<=-4;x++) for(int z=0;z<=1;z++) raised.Add(new Vector2Int(x,z));
            for(int x=-1;x<=0;x++) for(int z=-1;z<=1;z++) raised.Add(new Vector2Int(x,z));
            for(int x=3;x<=5;x++) for(int z=0;z<=1;z++) raised.Add(new Vector2Int(x,z));
            raised.Add(new Vector2Int(5,-1));
            for(int x=-7;x<=7;x++) for(int z=-4;z<=4;z++)
            {
                bool route=OnRoute(x,z,data.route),high=raised.Contains(new Vector2Int(x,z));
                float h=high ? .43f : 0;
                Box($"Tile {x},{z}",new Vector3(x,h*.5f-.12f,z),new Vector3(.975f,.24f+h,.975f),route ? 0 : high ? 2 : 1);
                if(high)
                {
                    Box($"Raised deck {x},{z}",new Vector3(x,h+.012f,z),new Vector3(.89f,.025f,.89f),3);
                    Box("Platform front bevel",new Vector3(x,h,z-.485f),new Vector3(.91f,.045f,.025f),5);
                    Box("Platform marker",new Vector3(x+.28f,h+.03f,z-.35f),new Vector3(.15f,.009f,.025f),6);
                }
                else
                {
                    Box("Tile seam",new Vector3(x-.475f,.006f,z),new Vector3(.012f,.008f,.93f),2);
                    Box("Tile seam",new Vector3(x,.006f,z-.475f),new Vector3(.93f,.008f,.012f),2);
                    if(route && (x+z)%3==0)
                        Box("Subtle route inlay",new Vector3(x,.013f,z),new Vector3(.22f,.008f,.05f),5);
                }
            }
            // Ground-height curbs keep the foreground clear of character feet and health bars.
            for(int x=-7;x<=7;x++)
            {
                Box("Front retaining ledge",new Vector3(x,-.05f,-4.6f),new Vector3(.97f,.18f,.28f),3);
                Box("Rear parapet",new Vector3(x,.37f,4.8f),new Vector3(.97f,.8f,.4f),1);
                Box("Rear parapet cap",new Vector3(x,.8f,4.8f),new Vector3(1.01f,.07f,.48f),5);
            }
            for(int side=-1;side<=1;side+=2)
            {
                Box("Service canal wall",new Vector3(side*8.2f,-.17f,0),new Vector3(.45f,.8f,14),2);
                Box("Outer service walk",new Vector3(side*9.4f,-.14f,0),new Vector3(1.7f,.4f,14),1);
                for(int z=-5;z<=5;z+=2)
                {
                    Box("Pump pier",new Vector3(side*9.4f,.7f,z),new Vector3(.8f,1.6f,.9f),2);
                    Box("Pump cap",new Vector3(side*9.4f,1.55f,z),new Vector3(1.05f,.12f,1.1f),5);
                }
            }
            // Rear plant equipment: silhouettes and surface detail, away from the route.
            for(int i=0;i<3;i++)
            {
                float x=-5.6f+i*5.4f;
                Box("Ventilation housing",new Vector3(x,.55f,3.5f),new Vector3(1.5f,1.1f,.75f),2);
                Box("Vent panel",new Vector3(x,.65f,3.1f),new Vector3(1.28f,.6f,.035f),3);
                for(int k=0;k<7;k++) Box("Vent fin",new Vector3(x-.5f+k*.16f,.65f,3.07f),new Vector3(.045f,.48f,.02f),5);
            }
            for(int i=0;i<5;i++)
                Box("Service conduit",new Vector3(-6+i*3,.14f,-3.65f),new Vector3(2.8f,.13f,.13f),2);
            Box("Central maintenance pit",new Vector3(-1,-.035f,-3),new Vector3(1.8f,.025f,.8f),2);
            for(int i=0;i<8;i++) Box("Drain grille",new Vector3(-1.75f+i*.22f,.004f,-3),new Vector3(.07f,.02f,.72f),3);
        }
        private static bool OnRoute(int x,int z,IReadOnlyList<Vector3> route)
        {
            Vector2 p=new Vector2(x,z);
            for(int i=1;i<route.Count;i++)
            {
                Vector2 a=new Vector2(route[i-1].x,route[i-1].z),b=new Vector2(route[i].x,route[i].z),v=b-a;
                float t=Mathf.Clamp01(Vector2.Dot(p-a,v)/v.sqrMagnitude);
                if(Vector2.Distance(p,a+t*v)<.1f) return true;
            }
            return false;
        }
        private void Box(string name,Vector3 position,Vector3 scale,int material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(transform,false);
            go.transform.localPosition=position; go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=materials[material]; Destroy(go.GetComponent<Collider>());
        }
        public void Restore()
        {
            if(restored) return;
            restored=true;
            if(scene!=null)
            {
                if(scene.RedGate!=null) scene.RedGate.position=red;
                if(scene.BlueGate!=null) scene.BlueGate.position=blue;
            }
            if(original!=null) original.SetActive(originalActive);
            if(borrowedCamera!=null) borrowedCamera.transform.position=cameraPosition;
            scene=null; gameObject.SetActive(false);
        }
        private void OnDestroy()
        {
            Restore();
            if(materials!=null) { Destroy(materials[0]); Destroy(materials[1]); }
        }
    }
}
