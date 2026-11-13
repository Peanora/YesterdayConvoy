using System;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Original YuS3EffectBehaviour's grid children, attached to the existing wall geometry.</summary>
    internal sealed class YuFirewallVfx:IDisposable
    {
        readonly OriginalNativeFxLayer layer;
        string context;ManualBattle observedBattle;Bounds board;bool hasBoard;
        void ReadBoard(ManualBattle battle)
        {
            if(observedBattle==battle)return;observedBattle=battle;hasBoard=false;
            // Read the same saved tile meshes used by the battlefield adapter.
            // Decorative bevels do not shorten a one-cell visual footprint.
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if(!renderer.gameObject.activeInHierarchy||(!renderer.name.StartsWith("Cell ",StringComparison.Ordinal)&&!renderer.name.StartsWith("Platform cap ",StringComparison.Ordinal)))continue;
                var c=renderer.bounds.center;var tile=new Bounds(new Vector3(c.x,0,c.z),new Vector3(1,10,1));
                if(!hasBoard){board=tile;hasBoard=true;}else board.Encapsulate(tile);
            }
        }
        public int VisibleCount=>layer.VisibleCount;
        public YuFirewallVfx(Transform root){layer=new OriginalNativeFxLayer(root);}
        public void Begin(double clock,Camera camera)=>layer.Begin(clock,camera);
        public void Present(ManualBattle battle,DefenderState defender,Camera cam)=>Present(battle,defender,cam,battle.Clock);
        public void Present(ManualBattle battle,DefenderState defender,Camera cam,double clock)
        {
            if((battle.Phase!=BattlePhase.Ready&&battle.Phase!=BattlePhase.Running)||!battle.DefenderWallActive(defender))return;
            // Source controller begins its children after this game's unchanged 1.6s casting lock.
            double age=clock-defender.SkillStartedAt-1.6;if(age<0)return;
            context="yu-wall/"+defender.EntityId+"/"+defender.SkillCasts;
            ReadBoard(battle);var center=battle.DefenderWallPoint(defender);
            var axis=Vector3.Cross(defender.WallNormal,Vector3.up).normalized;
            bool horizontal=Mathf.Abs(axis.x)>Mathf.Abs(axis.z);
            string body=horizontal?"yu_skill3_range_01":defender.WallNormal.x>=0?"yu_skill3_range_01_r":"yu_skill3_range_01_l";
            // Keep combat's existing extent, but end the visible original
            // grid children at the saved board edge instead of spawning in water.
            float low=-5.76f,high=5.76f;
            if(hasBoard)
            {
                float a=horizontal?(board.min.x-center.x)/axis.x:(board.min.z-center.z)/axis.z;
                float b=horizontal?(board.max.x-center.x)/axis.x:(board.max.z-center.z)/axis.z;
                low=Mathf.Max(low,Mathf.Min(a,b));high=Mathf.Min(high,Mathf.Max(a,b));
            }
            if(high<=low)return;
            for(float at=low+.5f;at<=high-.5f+.001f;at+=1)Draw(body,age,center+axis*at,cam);
            if(horizontal)
            {
                Draw("yu_skill3_range_01_l_end",age,center+axis*low,cam);
                Draw("yu_skill3_range_01_r_end",age,center+axis*high,cam);
            }
            else
            {
                string prefix=defender.WallNormal.x>=0?"yu_skill3_range_01_r":"yu_skill3_range_01_l";
                Draw(prefix+"_u_end",age,center+axis*high,cam);
                Draw(prefix+"_d_end",age,center+axis*low,cam);
            }
        }
        void Draw(string raw,double age,Vector3 p,Camera cam)
        {
            string key="defender/D22/"+raw;
            if(!OriginalNativeFxCatalog.Supports(key))return;
            string instance=context+"/"+raw+"/"+p.ToString("R");
            layer.TryShow(key,instance,age,new OriginalNativeFxPose(p,Quaternion.Euler(90,0,0),Vector3.right),true);
        }
        public void End()=>layer.End();
        public void Clear(){layer.Clear();observedBattle=null;hasBoard=false;}
        public void Dispose()=>layer.Dispose();
    }
}
