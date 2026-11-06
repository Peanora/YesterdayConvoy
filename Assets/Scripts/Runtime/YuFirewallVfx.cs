using System;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Original YuS3EffectBehaviour's grid children, attached to the existing wall geometry.</summary>
    internal sealed class YuFirewallVfx:IDisposable
    {
        readonly OriginalCombatVfxLayer layer;
        public int VisibleCount=>layer.VisibleCount;
        public YuFirewallVfx(Transform root){layer=new OriginalCombatVfxLayer(root);}
        public void Begin()=>layer.Begin();
        public void Present(ManualBattle battle,DefenderState defender,Camera cam)=>Present(battle,defender,cam,battle.Clock);
        public void Present(ManualBattle battle,DefenderState defender,Camera cam,double clock)
        {
            if((battle.Phase!=BattlePhase.Ready&&battle.Phase!=BattlePhase.Running)||!battle.DefenderWallActive(defender))return;
            // Source controller begins its children after this game's unchanged 1.6s casting lock.
            double age=clock-defender.SkillStartedAt-1.6;if(age<0)return;
            var center=battle.DefenderWallPoint(defender);
            var axis=Vector3.Cross(defender.WallNormal,Vector3.up).normalized;
            bool horizontal=Mathf.Abs(axis.x)>Mathf.Abs(axis.z);
            string body=horizontal?"yu_skill3_range_01":defender.WallNormal.x>=0?"yu_skill3_range_01_r":"yu_skill3_range_01_l";
            // Exactly the old +/-5.76 world-unit logical extent. One native child per grid, not 45 fake tongues.
            for(int i=0;i<12;i++)Draw(body,age,center+axis*(-5.5f+i),cam);
            if(horizontal)
            {
                Draw("yu_skill3_range_01_l_end",age,center-axis*5.76f,cam);
                Draw("yu_skill3_range_01_r_end",age,center+axis*5.76f,cam);
            }
            else
            {
                string prefix=defender.WallNormal.x>=0?"yu_skill3_range_01_r":"yu_skill3_range_01_l";
                Draw(prefix+"_u_end",age,center+axis*5.76f,cam);
                Draw(prefix+"_d_end",age,center-axis*5.76f,cam);
            }
        }
        void Draw(string raw,double age,Vector3 p,Camera cam)
        {
            string key="defender/D22/"+raw;
            if(OriginalCombatVfxArt.TryGet(key,out var clip))layer.ShowGround(key,age,p,cam,false,29,clip.Loop);
        }
        public void End()=>layer.End();
        public void Clear()=>layer.Clear();
        public void Dispose()=>layer.Dispose();
    }
}
