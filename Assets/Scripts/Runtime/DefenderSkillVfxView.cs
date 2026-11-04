using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Only reads existing events and simulation time; owns no gameplay state.</summary>
    public sealed class DefenderSkillVfxView : MonoBehaviour
    {
        Func<ManualBattle> source;
        readonly List<SpriteRenderer> sprites=new List<SpriteRenderer>();
        readonly HashSet<(string,double)> attacks=new HashSet<(string,double)>();
        Material material;
        YuFirewallVfx yuWall;
        int used;
        ManualBattle observedBattle;
        int observedRun=-1;
        public void Bind(Func<ManualBattle> battle)=>source=battle;
        void Draw(string key,double age,Vector3 position,Camera cam,float scale=1,float alpha=1,int order=31,float angle=0,bool flip=false,float stretch=1)
        {
            if(alpha<=0||age<0)return;
            var frame=DefenderVfxArt.Load(key).At(age);if(frame==null)return;
            if(used==sprites.Count)
            {
                var r=new GameObject("Defender VFX",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();r.transform.SetParent(transform,false);
                if(material==null)material=new Material(Shader.Find("Sprites/Default"));r.sharedMaterial=material;sprites.Add(r);
            }
            var renderer=sprites[used++];renderer.name="VFX / "+key;renderer.sprite=frame;renderer.enabled=true;
            renderer.transform.SetPositionAndRotation(position,cam.transform.rotation*Quaternion.Euler(0,0,angle));
            renderer.transform.localScale=new Vector3(scale*stretch,scale,1);renderer.color=new Color(1,1,1,Mathf.Clamp01(alpha));renderer.flipX=flip;
            renderer.sortingOrder=DemoSpriteVisual.GetSortingOrder(cam,position)+order;
        }
        static float Envelope(double age,double remaining)=>Mathf.SmoothStep(0,1,(float)age/.22f)*Mathf.SmoothStep(0,1,(float)remaining/.35f);
        void LateUpdate()
        {
            used=0;attacks.Clear();var b=source?.Invoke();var cam=Camera.main;
            if(b!=observedBattle||observedRun!=(b?.RunId??-1))
            {
                foreach(var sprite in sprites)Destroy(sprite.gameObject);
                sprites.Clear();observedBattle=b;observedRun=b?.RunId??-1;
                yuWall?.Clear();
            }
            yuWall?.Begin();
            if(b!=null&&cam!=null)
            {
                PresentFields(b,cam);
                PresentShots(b,cam);
            }
            for(int i=used;i<sprites.Count;i++)sprites[i].enabled=false;
            yuWall?.End();
        }
        void PresentFields(ManualBattle b,Camera cam)
        {
            foreach(var d in b.Defenders)
            {
                if(d.Settings.Skill.Kind==DefenderSkillKind.YuKitchen&&
                    (b.DefenderWallActive(d)||yuWall!=null))
                {
                    if(yuWall==null){yuWall=new YuFirewallVfx(transform);yuWall.Begin();}
                    yuWall.Present(b,d,cam);
                }
                if(!b.DefenderSkillActive(d))continue;
                double age=b.Clock-d.SkillStartedAt;float alpha=Envelope(age,d.SkillActiveUntil-b.Clock);
                if(d.Settings.Skill.Kind==DefenderSkillKind.SariaCalcification)
                    Draw("saria-field",age,d.Position,cam,Mathf.Max(1,d.Settings.Skill.EffectRadius/1.45f),alpha,27);
            }
            foreach(var drone in b.DefenderDrones)
            {
                var d=b.FindDefender(drone.SourceId);if(!b.DefenderSkillActive(d))continue;
                var target=b.FindUnit(drone.TargetId);bool left=target!=null&&Vector3.Dot(target.Motion.Position-drone.Position,cam.transform.right)<0;
                Draw("wolf-wake",b.Clock-d.SkillStartedAt+drone.Index*.19,drone.Position+cam.transform.up*.5f,cam,.85f,Envelope(b.Clock-d.SkillStartedAt,d.SkillActiveUntil-b.Clock),32,0,left);
            }
            var cells=b.ChessPieces;
            for(int i=0;i<cells.Count;i++)
            {
                var cell=cells[i];if(cell.Time>0){Draw("wang-ink",b.Clock-cell.Time,cell.Position,cam,.65f,1,32);continue;}
                if(!cell.Active)continue;
                for(int j=i+1;j<cells.Count;j++)
                {
                    var other=cells[j];var delta=other.Position-cell.Position;
                    if(!other.Active||other.Time>0||other.SourceId!=cell.SourceId||delta.sqrMagnitude>1.22f||Mathf.Abs(delta.x)>.1f&&Mathf.Abs(delta.z)>.1f)continue;
                    var a=cam.WorldToScreenPoint(cell.Position);var z=cam.WorldToScreenPoint(other.Position);float angle=Mathf.Atan2(z.y-a.y,z.x-a.x)*Mathf.Rad2Deg;
                    // Link length uses camera-plane projection, preserving its native thin width.
                    float distance=Vector3.ProjectOnPlane(delta,cam.transform.forward).magnitude;
                    Draw("chess-link",b.Clock,(cell.Position+other.Position)*.5f,cam,.42f,.8f,27,angle,false,distance/(2f*.42f));
                }
            }
        }
        void PresentShots(ManualBattle b,Camera cam)
        {
            foreach(var shot in b.CombatShots)
            {
                if(!shot.FromDefender||shot.Erased)continue;
                var d=b.FindDefender(shot.SourceId);if(d==null||!DefenderVfxArt.Supports(d.Settings.Skill.Kind))continue;
                var target=b.FindUnit(shot.TargetId);Vector3 destination=shot.Healing?(b.FindDefender(shot.TargetId)?.Position??shot.Destination):target?.Motion.Position??shot.Destination;
                double age=b.Clock-shot.StartedAt,hitAge=b.Clock-shot.ImpactAt;
                bool left=Vector3.Dot(destination-shot.Origin,cam.transform.right)<0;
                switch(d.Settings.Skill.Kind)
                {
                    case DefenderSkillKind.PramanixSnow:
                        // One burst per launched attack, including multi-target shots; never loops between attacks.
                        if(shot.Empowered&&attacks.Add((shot.SourceId,shot.StartedAt)))
                        {Draw("snow-back",age,shot.Origin,cam,1,1,29);Draw("snow-front",age,shot.Origin,cam,1,1,31);}
                        break;
                    case DefenderSkillKind.MlynarGlory:
                        if(attacks.Add((shot.SourceId,shot.StartedAt)))Draw("gold-sweep",age,shot.Origin+cam.transform.up*.55f,cam,.78f,1,33,0,left);
                        if(shot.Applied)Draw("gold-hit",hitAge,destination+cam.transform.up*.5f,cam,.8f,1,36);
                        break;
                    case DefenderSkillKind.LapplandHavoc:
                        if(shot.Applied)Draw("wolf-hit",hitAge,destination+cam.transform.up*.55f,cam,.65f,1,36,0,left);
                        if(!shot.Resolved&&age>=0&&hitAge<0&&!shot.Empowered)
                        {float p=Mathf.Clamp01((float)(age/(shot.ImpactAt-shot.StartedAt)));Draw("wolf-wake",age,Vector3.Lerp(shot.Origin,destination,p)+cam.transform.up*.6f,cam,.35f,1,33,0,left);}
                        break;
                    case DefenderSkillKind.SariaCalcification:
                        if(shot.Applied&&shot.Healing)Draw("saria-heal",hitAge,destination+cam.transform.up*.08f,cam,.75f,1,32);
                        break;
                    case DefenderSkillKind.WangGambit:
                        if(shot.Applied)Draw("wang-ink",hitAge,destination,cam,.6f,1,32);
                        break;
                    case DefenderSkillKind.YuKitchen:
                        if(shot.Applied)Draw("yu-hit",hitAge,destination+cam.transform.up*.3f,cam,.7f,1,34);
                        break;
                }
            }
        }
        void OnDestroy(){yuWall?.Dispose();if(material!=null)Destroy(material);}
    }
}
