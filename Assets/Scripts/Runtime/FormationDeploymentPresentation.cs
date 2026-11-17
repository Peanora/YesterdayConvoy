using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        public FormationDeploymentPreview DeploymentPreview {get;private set;}
        public bool DragPreviewVisible=>deploymentGhost!=null&&deploymentGhost.enabled&&DeploymentPreview!=null;
        public SpriteRenderer DeploymentGhost=>deploymentGhost;
        SpriteRenderer deploymentGhost;
        Vector2Int deploymentGrabOffset;
        Vector2Int DeploymentAnchor(Vector2Int pointerCell)=>pointerCell-deploymentGrabOffset;

        void PrepareDeploymentGrab(int instance)
        {
            var piece=Shop?.Bench.FirstOrDefault(p=>p.Instance==instance);
            var size=piece!=null?FormationUnitCatalog.Find(piece.Id).Footprint:Vector2Int.one;
            deploymentGrabOffset=new Vector2Int((size.x-1)/2,(size.y-1)/2);
        }
        void PrepareFieldDeploymentGrab(FormationFighter unit,Vector2 point)
        {
            PrepareDeploymentGrab(unit.Instance);
            if(TryCell(point,out var cell))deploymentGrabOffset=cell-new Vector2Int(Mathf.RoundToInt(unit.Spawn.x),Mathf.RoundToInt(unit.Spawn.y));
        }
        void ClearDeploymentPreview()
        {
            DeploymentPreview=null;
            if(deploymentGhost!=null)deploymentGhost.enabled=false;
        }
        void UpdateDeploymentPreview()
        {
            ClearDeploymentPreview();
            if(!Ready||!CanEditFormationView||!pointerHeld||shopDragInstance<=0||Vector2.Distance(formationShopPointer,pointerStart)<=6||!TryCell(formationShopPointer,out var pointerCell))return;
            var piece=Shop.Bench.FirstOrDefault(p=>p.Instance==shopDragInstance);if(piece==null)return;
            DeploymentPreview=Session.PreviewDeploy(piece.Instance,DeploymentAnchor(pointerCell));
            if(DeploymentPreview.Cells.width<1||DeploymentPreview.Cells.height<1)return;
            var definition=FormationUnitCatalog.Find(piece.Id);
            if(!FormationArtAvailable(definition.ArtId))return;
            var source=formationMonsterArt[definition.ArtId];var sprite=source.Frame("idle",Time.unscaledTimeAsDouble,true);
            if(sprite==null)return;
            if(deploymentGhost==null)
            {
                deploymentGhost=new GameObject("Animated deployment preview",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                deploymentGhost.transform.SetParent(actorRoot,false);deploymentGhost.sharedMaterial=actorMaterial;
                ActorGrounding.Attach(deploymentGhost,definition.VisualHeight);
            }
            var cells=DeploymentPreview.Cells;
            var center=FormationDeployment.Center(new Vector2(cells.x,cells.y),new Vector2Int(cells.width,cells.height));
            deploymentGhost.sprite=sprite;deploymentGhost.transform.position=WorldPoint(center)+Vector3.up*.035f;
            ActorGrounding.Attach(deploymentGhost,definition.VisualHeight);
            deploymentGhost.transform.rotation=battleCamera.transform.rotation;
            float scale=definition.VisualHeight/Mathf.Max(.5f,source.Height);
            float breath=definition.ArtId=="WISADEL_APRIL"?1+Mathf.Sin(Time.unscaledTime*2.6f)*.007f:1;
            deploymentGhost.transform.localScale=new Vector3(scale,scale*breath,scale);
            deploymentGhost.flipX=false;deploymentGhost.sortingOrder=12000;
            deploymentGhost.color=DeploymentPreview.Valid?new Color(.84f,1,.95f,.78f):new Color(1,.46f,.39f,.65f);
            deploymentGhost.enabled=true;
        }
        void DrawDeploymentGrid()
        {
            if(!CanEditFormationView)return;
            if(DeploymentPreview==null)
            {
                var selected=Model.Units.FirstOrDefault(u=>u.Enabled&&u.Slot==SelectedSlot);
                if(selected!=null&&(selected.Footprint.x>1||selected.Footprint.y>1))DrawDeploymentCells(selected.DeploymentCells,new Color(.22f,.75f,.85f));
                return;
            }
            var color=DeploymentPreview.Valid?DeploymentPreview.SwapInstance>0?new Color(1,.72f,.26f):new Color(.24f,.95f,.67f):new Color(1,.28f,.22f);
            DrawDeploymentCells(DeploymentPreview.Cells,color);
            if(DeploymentPreview.Valid&&DeploymentPreview.SwapInstance>0)DrawDeploymentCells(DeploymentPreview.SwapCells,new Color(1,.72f,.26f));
        }
        void DrawDeploymentCells(RectInt cells,Color color)
        {
            lineMaterial.SetPass(0);GL.PushMatrix();GL.MultMatrix(Matrix4x4.identity);
            GL.Begin(GL.QUADS);GL.Color(new Color(color.r,color.g,color.b,.20f));
            for(int x=cells.xMin;x<cells.xMax;x++)for(int y=cells.yMin;y<cells.yMax;y++)
            {
                var c=new Vector2(x,y);
                foreach(var d in new[]{new Vector2(-.48f,-.48f),new Vector2(-.48f,.48f),new Vector2(.48f,.48f),new Vector2(.48f,-.48f)})GL.Vertex(WorldPoint(c+d)+Vector3.up*.024f);
            }
            GL.End();GL.Begin(GL.LINES);GL.Color(new Color(color.r,color.g,color.b,.95f));
            for(int x=cells.xMin;x<cells.xMax;x++)for(int y=cells.yMin;y<cells.yMax;y++)
            {
                var c=new Vector2(x,y);var a=WorldPoint(c+new Vector2(-.48f,-.48f))+Vector3.up*.03f;
                var b=WorldPoint(c+new Vector2(-.48f,.48f))+Vector3.up*.03f;
                var d=WorldPoint(c+new Vector2(.48f,-.48f))+Vector3.up*.03f;
                var e=WorldPoint(c+new Vector2(.48f,.48f))+Vector3.up*.03f;
                GL.Vertex(a);GL.Vertex(b);GL.Vertex(b);GL.Vertex(e);GL.Vertex(e);GL.Vertex(d);GL.Vertex(d);GL.Vertex(a);
            }
            GL.End();GL.PopMatrix();
        }
        void DrawDeploymentHint()
        {
            if(DeploymentPreview==null)return;
            var cells=DeploymentPreview.Cells;if(cells.width<1||cells.height<1)return;
            var center=FormationDeployment.Center(new Vector2(cells.x,cells.y),new Vector2Int(cells.width,cells.height));
            var p=CellScreen(center);var box=new Rect(Mathf.Clamp(p.x-134,195,728),Mathf.Clamp(p.y+13,78,452),268,28);
            var color=DeploymentPreview.Valid?DeploymentPreview.SwapInstance>0?Gold:Cyan:new Color(1,.48f,.38f);
            Fill(box,new Color(.035f,.065f,.085f,.94f));ApprovedUi.Border(box,color,1);
            string message=DeploymentPreview.Valid?(DeploymentPreview.SwapInstance>0?"松开交换":"松开放置")+$" · {cells.width}×{cells.height} · 第{cells.x+1}列/{cells.y+1}行":DeploymentPreview.Failure;
            Label(new Rect(box.x+6,box.y+3,box.width-12,22),message,11,color,TextAnchor.MiddleCenter);
        }
    }
}
