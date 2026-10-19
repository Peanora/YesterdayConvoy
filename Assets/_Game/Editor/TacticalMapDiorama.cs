using UnityEngine;
using UnityEditor;

namespace YesterdayConvoy.Editor
{
    public static partial class TacticalMapTools
    {
        // Presentation only: an authored waterworks deck around the existing, unchanged route.
        private static void BuildDiorama(Transform map, Vector3[] points)
        {
            while(map.childCount>0) UnityEngine.Object.DestroyImmediate(map.GetChild(0).gameObject);
            Material slab = Mat("Deck Limestone", new Color(.34f,.39f,.39f), "concrete-color.jpg");
            Material slabAlt = Mat("Deck Limestone Alternate", new Color(.30f,.35f,.36f), "concrete-color.jpg");
            Material lane = Mat("Route Concrete", new Color(.49f,.53f,.50f), "concrete-color.jpg");
            Material laneAlt = Mat("Route Concrete Alternate", new Color(.45f,.49f,.47f), "concrete-color.jpg");
            Material sidewall = Mat("Deck Sidewall", new Color(.27f,.32f,.31f), "concrete-color.jpg");
            Material bevel = Mat("Pale Edges", new Color(.43f,.50f,.48f));
            Material top = Mat("Platform Surface", new Color(.29f,.37f,.38f), "asphalt-color.jpg");
            Material inset = Mat("Platform Inset", new Color(.24f,.30f,.31f), "concrete-color.jpg");
            Material marking = Mat("Faded Paint", new Color(.54f,.53f,.37f), unlit:true, opaque:true);
            Material shadow = Mat("Architecture Crease", new Color(.10f,.16f,.16f), unlit:true, opaque:true);
            Material water = Mat("Canal Water", new Color(.055f,.22f,.24f), unlit:true, opaque:true);
            water.shader=Shader.Find("YesterdayConvoy/Static Canal");
            water.SetColor("_Deep",new Color(.018f,.085f,.10f));
            water.SetColor("_Shallow",new Color(.055f,.24f,.23f));
            EditorUtility.SetDirty(water);
            Material ripple = Mat("Water Reflections", new Color(.08f,.30f,.31f), unlit:true, opaque:true);
            Material rust = Mat("Pipe Oxide", new Color(.33f,.29f,.20f));
            Shape("Foundation",map,new Vector3(0,-.9f,0),new Vector3(40,.2f,40),dark);
            Shape("Reference water",map,new Vector3(0,-.39f,0),new Vector3(40,.04f,40),water);
            Shape("Deck retaining mass",map,new Vector3(0,-.24f,.5f),new Vector3(13.06f,.43f,8.08f),sidewall);

            for(int x=-6;x<=6;x++) for(int z=-3;z<=4;z++)
            {
                bool route=IsOnRoute(x,z,points);
                bool raised=!route && (x>=-4 && x<=-3 && z>=0 && z<=1 ||
                    x>=-1 && x<=0 && z>=1 && z<=2 || x>=3 && x<=4 && z>=1 && z<=2 ||
                    x>=0 && x<=1 && z==-2);
                float h=raised ? .30f : 0;
                Material ground=route ? ((x+z)%2==0 ? lane : laneAlt) : ((x*3+z)%3==0 ? slabAlt : slab);
                // The route stays at y=0. Raise only authored non-route tiles.
                Shape($"Cell {x},{z}",map,new Vector3(x,h*.5f-.13f,z),new Vector3(.985f,.26f+h,.985f),raised ? sidewall : ground);
                if(raised)
                {
                    Shape($"Platform cap {x},{z}",map,new Vector3(x,h+.016f,z),new Vector3(.94f,.035f,.94f),top);
                    Shape($"Platform recess {x},{z}",map,new Vector3(x,h+.036f,z),new Vector3(.77f,.008f,.77f),inset);
                    // Four edges break the long vertical bars into readable square deployment tiles.
                    TileFrame(map,$"Platform rim {x},{z}",x,z,h+.044f,.89f,.026f,bevel);
                    Shape($"Warning lip {x},{z}",map,new Vector3(x,h+.047f,z-.43f),new Vector3(.30f,.012f,.024f),marking);
                    for(int k=0;k<4;k++)
                    {
                        float sx=k%2==0 ? -1 : 1, sz=k<2 ? -1 : 1;
                        Shape($"Cap corner {x},{z} {k}",map,new Vector3(x+sx*.37f,h+.048f,z+sz*.37f),new Vector3(.065f,.014f,.065f),bevel);
                    }
                    Shape($"Platform front joint {x},{z}",map,new Vector3(x,h*.5f,z-.495f),new Vector3(.68f,.018f,.012f),shadow);
                }
                else
                {
                    // A shallow perimeter bevel and two fine joins replace the black oversized grid.
                    Shape($"Tile edge x {x},{z}",map,new Vector3(x,-.002f,z-.477f),new Vector3(.91f,.005f,.016f),bevel);
                    Shape($"Tile edge z {x},{z}",map,new Vector3(x-.477f,-.002f,z),new Vector3(.016f,.005f,.91f),bevel);
                    if(!route && (x*5+z*3)%7==0)
                        Shape($"Slab patch {x},{z}",map,new Vector3(x+.21f,.003f,z+.15f),new Vector3(.13f,.006f,.28f),slabAlt);
                }
            }

            // Deliberate boundary: heavy dock walls, side canals, and cropped architecture at the frame.
            for(int side=-1;side<=1;side+=2)
            {
                Shape($"Dock side coping {side}",map,new Vector3(side*6.58f,.035f,.5f),new Vector3(.19f,.16f,8.28f),bevel);
                Shape($"Outer service walk {side}",map,new Vector3(side*9.35f,-.1f,.5f),new Vector3(2.4f,.35f,13),slabAlt);
                Shape($"Outer canal wall {side}",map,new Vector3(side*8.13f,-.15f,.5f),new Vector3(.22f,.6f,14),sidewall);
                for(int pier=0;pier<2;pier++)
                {
                    float z=-1.8f+pier*4.5f;
                    Shape($"Canal pier footing {side} {pier}",map,new Vector3(side*8.15f,-.03f,z),new Vector3(1.3f,.42f,1.35f),sidewall);
                    Shape($"Canal pier shaft {side} {pier}",map,new Vector3(side*8.35f,1.03f,z),new Vector3(.86f,1.85f,.96f),slabAlt);
                    Shape($"Canal pier crown {side} {pier}",map,new Vector3(side*8.35f,2.02f,z),new Vector3(1.08f,.19f,1.15f),bevel);
                    Shape($"Canal pier recess {side} {pier}",map,new Vector3(side*8.35f,1.0f,z-.49f),new Vector3(.5f,1.33f,.018f),sidewall);
                    Shape($"Canal pier band {side} {pier}",map,new Vector3(side*8.35f,.24f,z),new Vector3(1.02f,.12f,1.11f),bevel);
                }
                for(int i=0;i<7;i++)
                {
                    float z=-3+i*1.2f;
                    Shape($"Dock buttress {side} {i}",map,new Vector3(side*6.68f,-.24f,z),new Vector3(.28f,.45f,.28f),sidewall);
                    Shape($"Water gleam {side} {i}",map,new Vector3(side*(7.35f+(i%3)*.13f),-.362f,z+.1f),new Vector3(.62f-(i%3)*.1f,.005f,.026f),ripple);
                    Shape($"Water gleam fine {side} {i}",map,new Vector3(side*7.45f,-.361f,z+.18f),new Vector3(.28f,.005f,.013f),ripple);
                    if(i%2==0)
                    {
                        Shape($"Outer pier {side} {i}",map,new Vector3(side*9.15f,.7f,z),new Vector3(.64f,1.55f,.72f),sidewall);
                        Shape($"Pier cornice {side} {i}",map,new Vector3(side*9.15f,1.5f,z),new Vector3(.78f,.13f,.86f),bevel);
                    }
                }
                // Conduits, fixings and broad cover plates keep industrial silhouettes cohesive.
                for(int i=0;i<3;i++)
                {
                    float x=side*(2.5f+i*1.2f);
                    Shape($"Rear parapet {side} {i}",map,new Vector3(x,.43f,4.65f),new Vector3(1.17f,.94f,.46f),slabAlt);
                    Shape($"Rear parapet cap {side} {i}",map,new Vector3(x,.94f,4.65f),new Vector3(1.23f,.09f,.56f),bevel);
                    Shape($"Parapet inset {side} {i}",map,new Vector3(x,.46f,4.41f),new Vector3(.91f,.48f,.02f),sidewall);
                    Pipe($"Rear conduit {side} {i}",map,new Vector3(x,.33f,4.20f),1.1f);
                    map.Find($"Rear conduit {side} {i}").GetComponent<Renderer>().sharedMaterial=rust;
                }
                Shape($"Front dock edge {side}",map,new Vector3(side*3.25f,-.07f,-3.58f),new Vector3(6.4f,.31f,.20f),sidewall);
                for(int i=0;i<4;i++)
                {
                    Shape($"Front edge block {side} {i}",map,new Vector3(side*(.6f+i*1.55f),.06f,-3.63f),new Vector3(.36f,.16f,.28f),bevel);
                    Shape($"Front edge joint {side} {i}",map,new Vector3(side*(.6f+i*1.55f),-.16f,-3.70f),new Vector3(.045f,.16f,.012f),shadow);
                }
            }
            Shape("Rear canal bridge",map,new Vector3(0,-.13f,5.7f),new Vector3(2.1f,.3f,2.2f),slab);
            Shape("Rear bulkhead",map,new Vector3(0,.85f,7.0f),new Vector3(5.2f,2.0f,.65f),sidewall);
            for(int i=0;i<5;i++)
                Shape($"Bulkhead rib {i}",map,new Vector3(-2+i,.8f,6.6f),new Vector3(.12f,1.85f,.16f),bevel);
            Prop("Ventilation west","Prop_Vent_Big.fbx",map,new Vector3(-5.5f,.02f,3.4f),1.25f,.65f,180);
            Prop("Ventilation east","Prop_Vent_Big.fbx",map,new Vector3(5.5f,.02f,3.4f),1.25f,.65f,180);
            Prop("Supply crate west","Prop_Crate3.fbx",map,new Vector3(-3.6f,.02f,3.6f),.65f,.65f,7);
            Prop("Supply crate east","Prop_Crate3.fbx",map,new Vector3(4.6f,.02f,3.4f),.65f,.65f,-8);
            Prop("Control console","Prop_Computer.fbx",map,new Vector3(1.4f,.02f,3.5f),.8f,.8f,180);
            for(int x=-5;x<=5;x+=2)
            {
                Shape($"Drain frame {x}",map,new Vector3(x,.008f,-2.9f),new Vector3(.72f,.025f,.22f),sidewall);
                for(int i=0;i<6;i++)
                    Shape($"Drain slat {x} {i}",map,new Vector3(x-.28f+i*.11f,.025f,-2.9f),new Vector3(.045f,.012f,.17f),shadow);
            }
            // Retain semantic route markers for preview/regression, but make them faint and sparse.
            for(int i=1;i<points.Length;i++)
            {
                Vector3 direction=(points[i]-points[i-1]).normalized;
                Vector3 center=(points[i]+points[i-1])*.5f;
                Vector3 side=Vector3.Cross(Vector3.up,direction)*.39f;
                Stroke($"Lane {i} L",map,new[]{center+side-direction*.12f,center+side+direction*.12f},amber,.014f);
                Stroke($"Lane {i} R",map,new[]{center-side-direction*.12f,center-side+direction*.12f},amber,.014f);
            }
            // A few small fixtures, rather than a yellow stripe on every tile.
            for(int side=-1;side<=1;side+=2)
            {
                Shape($"Entry sill {side}",map,new Vector3(side*5,-.01f,1),new Vector3(1.04f,.06f,1.04f),sidewall);
                for(int i=0;i<3;i++)
                    Shape($"Dock safety {side} {i}",map,new Vector3(side*6.59f,.124f,2.8f+i*.16f),new Vector3(.21f,.01f,.055f),marking);
            }
        }

        private static void TileFrame(Transform root,string name,float x,float z,float y,float size,float width,Material material)
        {
            for(int side=-1;side<=1;side+=2)
            {
                Shape(name+" x "+side,root,new Vector3(x+side*size*.5f,y,z),new Vector3(width,.009f,size),material);
                Shape(name+" z "+side,root,new Vector3(x,y,z+side*size*.5f),new Vector3(size,.009f,width),material);
            }
        }
    }
}
