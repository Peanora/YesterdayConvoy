using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        public FormationDeploymentPreview DeploymentPreview {get;private set;}
        public bool DragPreviewVisible=>deploymentGhost!=null&&(deploymentGhost.enabled||deploymentOverlay.width>0);
        public SpriteRenderer DeploymentGhost=>deploymentGhost;
        SpriteRenderer deploymentGhost,deploymentShadow;
        MaterialPropertyBlock deploymentProperties;
        Rect deploymentOverlay;
        Vector2Int deploymentGrabOffset;
        Vector3 deploymentGrabGroundOffset;
        const float DeploymentLiftHeight=.62f;
        Vector2Int DeploymentAnchor(Vector2Int pointerCell)=>pointerCell-deploymentGrabOffset;

        void PrepareDeploymentGrab(int instance)
        {
            var piece=Shop?.Bench.FirstOrDefault(p=>p.Instance==instance);
            var size=piece!=null?FormationUnitCatalog.Find(piece.Id).Footprint:Vector2Int.one;
            deploymentGrabOffset=new Vector2Int((size.x-1)/2,(size.y-1)/2);
            deploymentGrabGroundOffset=Vector3.zero;
        }
        void PrepareFieldDeploymentGrab(FormationFighter unit,Vector2 point)
        {
            PrepareDeploymentGrab(unit.Instance);
            if(TryCell(point,out var cell))deploymentGrabOffset=cell-new Vector2Int(Mathf.RoundToInt(unit.Spawn.x),Mathf.RoundToInt(unit.Spawn.y));
            if(TryDeploymentGroundPoint(point,out var ground))deploymentGrabGroundOffset=ActorWorldPoint(unit.SpawnCenter)-ground;
        }
        bool TryDeploymentGroundPoint(Vector2 point,out Vector3 ground)
        {
            var ray=battleCamera.ScreenPointToRay(TacticalViewport.Current.ToScreen(point));
            var plane=new Plane(Vector3.up,WorldPoint(Vector2.zero));
            if(plane.Raycast(ray,out float distance)){ground=ray.GetPoint(distance);return true;}
            ground=default;return false;
        }
        void ClearDeploymentPreview()
        {
            DeploymentPreview=null;
            deploymentOverlay=Rect.zero;
            if(deploymentGhost!=null)deploymentGhost.enabled=false;
            if(deploymentShadow!=null)deploymentShadow.enabled=false;
        }
        void UpdateDeploymentPreview()
        {
            ClearDeploymentPreview();
            if(!CanEditFormationView||formationMenuOpen||CampaignInteractionBlocked||!pointerHeld||shopDragInstance<=0||
                Vector2.Distance(formationShopPointer,pointerStart)<=6||!TryDeploymentGroundPoint(formationShopPointer,out var pointerGround))return;
            var piece=Shop.Bench.FirstOrDefault(p=>p.Instance==shopDragInstance);if(piece==null)return;
            bool overBoard=TryCell(formationShopPointer,out var pointerCell);
            if(overBoard)DeploymentPreview=Session.PreviewDeploy(piece.Instance,DeploymentAnchor(pointerCell));
            var definition=FormationUnitCatalog.Find(piece.Id);
            if(!FormationArtAvailable(definition.ArtId))return;
            var source=formationMonsterArt[definition.ArtId];var sprite=source.Frame("idle",Time.unscaledTimeAsDouble,true);
            if(sprite==null)return;
            if(deploymentGhost==null)
            {
                deploymentGhost=new GameObject("Animated deployment preview",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                deploymentGhost.transform.SetParent(actorRoot,false);deploymentGhost.sharedMaterial=actorMaterial;
                ActorGrounding.Attach(deploymentGhost,definition.VisualHeight);
                // Reuse the existing contact-shadow sprite, but keep it on the floor while the
                // held character is raised. Other actors retain their ordinary grounding.
                var grounding=deploymentGhost.GetComponent<ActorGrounding>();grounding.enabled=false;
                deploymentShadow=grounding.Shadow;deploymentShadow.transform.SetParent(actorRoot,false);
            }
            var ghostGround=pointerGround+deploymentGrabGroundOffset;
            // Only the landing footprint snaps to cells. The held body follows every pointer
            // movement continuously above its separate ground shadow, without moving the roster.
            deploymentGhost.sprite=sprite;deploymentGhost.transform.position=ghostGround+Vector3.up*DeploymentLiftHeight;
            deploymentGhost.transform.rotation=battleCamera.transform.rotation;
            float scale=definition.VisualHeight/Mathf.Max(.5f,source.Height);
            float breath=definition.ArtId=="WISADEL_APRIL"?1+Mathf.Sin(Time.unscaledTime*2.6f)*.007f:1;
            deploymentGhost.transform.localScale=new Vector3(scale,scale*breath,scale);
            deploymentGhost.flipX=false;deploymentGhost.sortingOrder=12000;
            deploymentGhost.color=DeploymentPreview?.Valid==false?new Color(1,.72f,.66f,.88f):new Color(1,1,1,.94f);
            if(!overBoard)
            {
                // Project the same lifted sprite onto the UI while it crosses the gap and
                // bench. The bench background is drawn after the board camera.
                var bounds=sprite.bounds;
                var viewport=TacticalViewport.Current;
                var topLeft=viewport.FromScreen(battleCamera.WorldToScreenPoint(
                    deploymentGhost.transform.TransformPoint(new Vector3(bounds.min.x,bounds.max.y))));
                var bottomRight=viewport.FromScreen(battleCamera.WorldToScreenPoint(
                    deploymentGhost.transform.TransformPoint(new Vector3(bounds.max.x,bounds.min.y))));
                deploymentOverlay=Rect.MinMaxRect(topLeft.x,topLeft.y,bottomRight.x,bottomRight.y);
                return;
            }
            if(deploymentProperties==null)deploymentProperties=new MaterialPropertyBlock();
            deploymentGhost.GetPropertyBlock(deploymentProperties);
            deploymentProperties.SetColor("_EnvironmentTint",new Color(.88f,.93f,.99f,1));deploymentProperties.SetFloat("_BodyHeight",definition.VisualHeight);deploymentGhost.SetPropertyBlock(deploymentProperties);
            deploymentGhost.enabled=true;
            deploymentShadow.enabled=true;deploymentShadow.transform.position=ghostGround+Vector3.up*.016f;
            deploymentShadow.transform.rotation=Quaternion.Euler(90,0,0);
            deploymentShadow.transform.localScale=new Vector3(definition.VisualHeight*.75f,definition.VisualHeight*.44f,1);
            deploymentShadow.color=new Color(0,0,0,.25f);deploymentShadow.sortingOrder=11999;
        }
        void DrawHeldFormationActor()
        {
            if(deploymentOverlay.width<=0||Event.current.type!=EventType.Repaint)return;
            var sprite=deploymentGhost.sprite;
            if(sprite==null||sprite.texture==null)return;
            var source=sprite.textureRect;
            var texture=sprite.texture;
            var previous=GUI.color;
            GUI.color=deploymentGhost.color;
            GUI.DrawTextureWithTexCoords(deploymentOverlay,texture,new Rect(
                source.x/texture.width,source.y/texture.height,source.width/texture.width,source.height/texture.height));
            GUI.color=previous;
        }
        void DrawDeploymentGrid()
        {
            if(!CanEditFormationView)return;
            if(DeploymentPreview==null)
            {
                var selected=Model.Units.FirstOrDefault(u=>u.Enabled&&u.Slot==SelectedSlot);
                if(selected!=null)DrawDeploymentCorners(selected.DeploymentCells,new Color(.86f,.83f,.72f,.78f));
                return;
            }
            var color=DeploymentPreview.Valid?DeploymentPreview.SwapInstance>0?new Color(.85f,.71f,.47f):new Color(.66f,.80f,.69f):new Color(.86f,.43f,.36f);
            DrawDeploymentCells(DeploymentPreview.Cells,color);
            if(DeploymentPreview.Valid&&DeploymentPreview.SwapInstance>0)DrawDeploymentCells(DeploymentPreview.SwapCells,new Color(.85f,.71f,.47f));
        }
        void DrawDeploymentCells(RectInt cells,Color color)
        {
            lineMaterial.SetPass(0);GL.PushMatrix();GL.MultMatrix(Matrix4x4.identity);
            GL.Begin(GL.QUADS);GL.Color(new Color(color.r,color.g,color.b,.13f));
            for(int x=cells.xMin;x<cells.xMax;x++)for(int y=cells.yMin;y<cells.yMax;y++)
            {
                var c=new Vector2(x,y);
                foreach(var d in new[]{new Vector2(-.48f,-.48f),new Vector2(-.48f,.48f),new Vector2(.48f,.48f),new Vector2(.48f,-.48f)})GL.Vertex(WorldPoint(c+d)+Vector3.up*.024f);
            }
            GL.End();GL.Begin(GL.LINES);GL.Color(new Color(color.r,color.g,color.b,.45f));
            for(int x=cells.xMin;x<cells.xMax;x++)for(int y=cells.yMin;y<cells.yMax;y++)
            {
                var c=new Vector2(x,y);var a=WorldPoint(c+new Vector2(-.48f,-.48f))+Vector3.up*.03f;
                var b=WorldPoint(c+new Vector2(-.48f,.48f))+Vector3.up*.03f;
                var d=WorldPoint(c+new Vector2(.48f,-.48f))+Vector3.up*.03f;
                var e=WorldPoint(c+new Vector2(.48f,.48f))+Vector3.up*.03f;
                GL.Vertex(a);GL.Vertex(b);GL.Vertex(b);GL.Vertex(e);GL.Vertex(e);GL.Vertex(d);GL.Vertex(d);GL.Vertex(a);
            }
            GL.End();GL.PopMatrix();
            DrawDeploymentCorners(cells,new Color(color.r,color.g,color.b,.9f));
        }
        void DrawDeploymentCorners(RectInt cells,Color color)
        {
            if(cells.width<1||cells.height<1)return;
            lineMaterial.SetPass(0);GL.PushMatrix();GL.MultMatrix(Matrix4x4.identity);
            GL.Begin(GL.LINES);GL.Color(color);
            foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})
            {
                var corner=new Vector2(x<0?cells.xMin-.45f:cells.xMax-.55f,y<0?cells.yMin-.45f:cells.yMax-.55f);
                var p=WorldPoint(corner)+Vector3.up*.035f;
                GL.Vertex(p);GL.Vertex(p+Vector3.right*(-x*.24f));
                GL.Vertex(p);GL.Vertex(p+Vector3.forward*(-y*.24f));
            }
            GL.End();GL.PopMatrix();
        }
    }
}
