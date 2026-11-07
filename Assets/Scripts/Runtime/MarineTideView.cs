using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace YesterdayConvoy
{
    /// <summary>Original Nethersea-brand textures on authored floor cells. All timing comes from the battle; this view never grows terrain or applies injury.</summary>
    public sealed class MarineTideView:MonoBehaviour
    {
        sealed class TileView
        {
            internal MeshRenderer Renderer;
            internal readonly MaterialPropertyBlock Properties=new MaterialPropertyBlock();
        }
        readonly List<TileView> tiles=new List<TileView>();
        readonly HashSet<Vector2Int> occupied=new HashSet<Vector2Int>();
        Func<ManualBattle> source;
        ManualBattle observed;
        int run=-1;
        Mesh tileMesh;
        Material material;
        Texture2D sourceTexture;
        public bool HighlightCoverage {get;set;}
        public bool PresentationVisible {get;set;}=true;
        public int VisibleTileCount {get;private set;}
        public Texture2D SourceTexture=>sourceTexture;
        public IEnumerable<MeshRenderer> GroundRenderers {get{foreach(var tile in tiles)yield return tile.Renderer;}}

        public void Bind(Func<ManualBattle> battle,Func<double> presentationClock=null)
        {source=battle;observed=null;run=-1;HighlightCoverage=false;Hide();}

        void EnsureMaterial()
        {
            if(material!=null)return;
            var shader=Resources.Load<Shader>("MarineTide");
            sourceTexture=Resources.Load<Texture2D>("MarineTide/Special_juntan_01");
            if(shader==null||sourceTexture==null)throw new InvalidOperationException("Missing original marine-ground texture or clock-driven MarineTide shader.");
            material=new Material(shader){name="Original Nethersea brand / clock adapted"};
            material.SetTexture("_MainTex",sourceTexture);
            material.SetTexture("_DetailTex",Resources.Load<Texture2D>("MarineTide/Special_juntan_04"));
            material.SetTexture("_VeinTex",Resources.Load<Texture2D>("MarineTide/Special_juntan_03"));
            material.SetTexture("_FlowTex",Resources.Load<Texture2D>("MarineTide/flow_43_2"));
            tileMesh=new Mesh{name="Authored floor-cell marine overlay"};
            tileMesh.vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,0,.5f),new Vector3(-.5f,0,.5f)};
            tileMesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};
            tileMesh.triangles=new[]{0,2,1,0,3,2};tileMesh.RecalculateBounds();
        }

        TileView View(int index)
        {
            while(tiles.Count<=index)
            {
                var renderer=new GameObject("Original Nethersea-brand floor",typeof(MeshFilter),typeof(MeshRenderer)).GetComponent<MeshRenderer>();
                renderer.transform.SetParent(transform,false);renderer.sharedMaterial=material;
                renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                renderer.GetComponent<MeshFilter>().sharedMesh=tileMesh;renderer.enabled=false;
                tiles.Add(new TileView{Renderer=renderer});
            }
            return tiles[index];
        }

        public void Present()
        {
            var battle=source?.Invoke();
            if(observed!=battle||run!=(battle?.RunId??-1))
            {Hide();HighlightCoverage=false;observed=battle;run=battle?.RunId??-1;}
            if(!PresentationVisible||battle==null||battle.MarineTier==0||battle.MarineCells.Count==0||battle.Phase==BattlePhase.Ready)
            {Hide();return;}
            EnsureMaterial();occupied.Clear();foreach(var cell in battle.MarineCells)occupied.Add(cell.Cell);
            // Presentation tail time must not restart organic motion after the battle clock stops.
            float now=(float)battle.Clock;
            int used=0;
            foreach(var cell in battle.MarineCells)
            {
                var view=View(used++);var renderer=view.Renderer;
                renderer.name="Original Nethersea brand / "+cell.Cell.x+","+cell.Cell.y;
                renderer.transform.SetPositionAndRotation(cell.Position,Quaternion.identity);renderer.transform.localScale=Vector3.one;
                renderer.enabled=true;
                var p=view.Properties;
                p.SetFloat("_MarineTime",now);
                p.SetFloat("_Growth",Mathf.SmoothStep(0,1,Mathf.Clamp01((float)(battle.Clock-cell.BornAt)/.7f)));
                p.SetFloat("_Active",battle.MarineActive?1:0);
                p.SetFloat("_Highlight",HighlightCoverage?1:0);
                p.SetFloat("_Seed",cell.IsSeed?1:0);
                p.SetFloat("_BornAge",Mathf.Max(0,(float)(battle.Clock-cell.BornAt)));
                p.SetVector("_Edges",new Vector4(occupied.Contains(cell.Cell+Vector2Int.left)?0:1,
                    occupied.Contains(cell.Cell+Vector2Int.right)?0:1,occupied.Contains(cell.Cell+Vector2Int.down)?0:1,
                    occupied.Contains(cell.Cell+Vector2Int.up)?0:1));
                renderer.SetPropertyBlock(p);
            }
            for(int i=used;i<tiles.Count;i++)tiles[i].Renderer.enabled=false;
            VisibleTileCount=used;
        }

        void LateUpdate()=>Present();
        void Hide(){foreach(var tile in tiles)if(tile.Renderer!=null)tile.Renderer.enabled=false;VisibleTileCount=0;}
        void OnDisable(){Hide();HighlightCoverage=false;}
        void OnDestroy(){if(material!=null)Destroy(material);if(tileMesh!=null)Destroy(tileMesh);}
    }
}
