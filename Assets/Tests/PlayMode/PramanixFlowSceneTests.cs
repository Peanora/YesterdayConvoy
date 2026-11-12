using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    public sealed class PramanixFlowSceneTests
    {
        const string Key="defender/D27/sbell2_skill_03_flow";
        const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        const BindingFlags Instance=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        static object FlowClip()=>typeof(ManualBattle).Assembly.GetType("YesterdayConvoy.OriginalCombatVfxArt")
            .GetMethod("Get",Static).Invoke(null,new object[]{Key});
        static IEnumerator Warm(object clip)
        {
            var sequence=clip.GetType().GetField("Sequence",Instance).GetValue(clip);
            return (IEnumerator)sequence.GetType().GetMethod("Warm",Instance).Invoke(sequence,null);
        }
        static Sprite At(object clip,double age)=>(Sprite)clip.GetType().GetMethod("At",Instance).Invoke(clip,new object[]{age,true});
        static SpriteRenderer[] Flow(GameObject root)=>root.GetComponentsInChildren<SpriteRenderer>().Where(r=>
            r.enabled&&r.sprite!=null&&r.name=="Original VFX / "+Key).ToArray();
        static DefenderSettings Caster(string id,Vector3 position,float initialSP,float duration,float health=100000)
            =>new DefenderSettings(id,id,"D27",position,health,1,0,20,2,windup:.15f,actionDuration:.5f,
                skill:new DefenderSkillSettings(DefenderSkillKind.PramanixSnow,initialSP,20,1,duration,effectRadius:20),canAttack:false);
        static ManualBattle Battle(DefenderSettings[] roster,bool attacker=false)
            =>new ManualBattle(new ManualBattleSettings("flow-card","flow-unit",30,1,20,1,.1f,100000,.1f,1,1,1,1),
                new[]{Vector3.zero,Vector3.forward*100},Vector3.one,
                new EngagementSettings(unitAttack:attacker?10:1,unitRange:attacker?20:.01f,unitInterval:1,
                    attackWindup:.3f,attackDuration:.5f,projectileTime:.2f),roster:roster);
        static void AssertActiveFrames(GameObject root,ManualBattle battle,object clip)
        {
            var active=battle.Defenders.Where(battle.DefenderSkillActive).ToArray();var flow=Flow(root);
            Assert.That(flow.Length,Is.EqualTo(active.Length),"Each active caster owns one loop; idle/ended casters own none.");
            foreach(var caster in active)
            {
                var renderer=flow.Single(r=>Vector3.Distance(r.transform.position,caster.Position)<.001f);
                Assert.That(renderer.sprite,Is.SameAs(At(clip,battle.Clock-caster.SkillStartedAt)),
                    "Flow age belongs to the caster's real skill start, not a shot or another caster.");
                Assert.That(renderer.transform.lossyScale,Is.EqualTo(Vector3.one));
            }
        }

        [UnityTest]public IEnumerator StaggeredCastersKeepIndependentSkillAgeAcrossLoopAttacksPauseEndAndRestart()
        {
            float prior=Time.timeScale;GameObject root=null;
            try
            {
                yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;Time.timeScale=0;
                var clip=FlowClip();yield return Warm(clip);
                Assert.That((float)clip.GetType().GetProperty("Duration",Instance).GetValue(clip),Is.EqualTo(3).Within(.00001f));
                // Only the immutable fixture differs: SP makes the second real cast begin two seconds later.
                var battle=Battle(new[]{Caster("snow-a",Vector3.right*2,20,5),Caster("snow-b",Vector3.left*2,18,5)});
                root=new GameObject("Two real Pramanix flow events");
                root.AddComponent<AdvancedDefenderView>().Bind(()=>battle,()=>battle.Clock);
                battle.Begin();Assert.That(battle.TryDeploy(battle.RunId,1,battle.Entry).Success,Is.True);
                while(battle.Clock<4.2)
                {
                    battle.Advance(.05f);yield return null;AssertActiveFrames(root,battle,clip);
                }
                var first=battle.FindDefender("snow-a");var second=battle.FindDefender("snow-b");
                Assert.That(second.SkillStartedAt-first.SkillStartedAt,Is.GreaterThan(1.8));
                Assert.That(battle.Clock-first.SkillStartedAt,Is.GreaterThan(3),"The first loop must cross the source's three-second boundary.");
                Assert.That(battle.CombatShots.Where(s=>s.FromDefender&&s.SourceId==first.EntityId&&s.Empowered)
                    .Select(s=>s.StartedAt).Distinct().Count(),Is.GreaterThanOrEqualTo(2),"Real attacks must not restart the persistent loop.");
                var flow=Flow(root);var sprites=flow.Select(r=>r.sprite).ToArray();var positions=flow.Select(r=>r.transform.position).ToArray();
                double clock=battle.Clock;float hp=battle.Units[0].Health;battle.SetPaused(true);
                for(int i=0;i<3;i++){battle.Advance(1);yield return null;}
                Assert.That(battle.Clock,Is.EqualTo(clock));Assert.That(battle.Units[0].Health,Is.EqualTo(hp));
                for(int i=0;i<flow.Length;i++)
                {Assert.That(flow[i].sprite,Is.SameAs(sprites[i]));Assert.That(flow[i].transform.position,Is.EqualTo(positions[i]));}
                battle.SetPaused(false);
                while(battle.Clock<=first.SkillActiveUntil+.1)
                {battle.Advance(.05f);yield return null;AssertActiveFrames(root,battle,clip);}
                Assert.That(first.SkillCasts,Is.EqualTo(1));Assert.That(battle.DefenderSkillActive(first),Is.False);
                Assert.That(battle.DefenderSkillActive(second),Is.True,"One caster ending cannot stop the other's loop.");
                Assert.That(Flow(root).Length,Is.EqualTo(1));
                battle.Restart();yield return null;
                Assert.That(Flow(root),Is.Empty,"Restart must hide the still-active second loop before deferred destruction.");
            }
            finally{if(root!=null)Object.Destroy(root);Time.timeScale=prior;}
            yield return null;
        }

        [UnityTest]public IEnumerator RealAttackerDeathContactRemovesTheCastersLoopBeforeSkillExpiry()
        {
            float prior=Time.timeScale;GameObject root=null;
            try
            {
                yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;Time.timeScale=0;
                var clip=FlowClip();yield return Warm(clip);
                var battle=Battle(new[]{Caster("snow-mortal",Vector3.right*2,20,20,5)},attacker:true);
                root=new GameObject("Pramanix flow actual death event");
                root.AddComponent<AdvancedDefenderView>().Bind(()=>battle,()=>battle.Clock);
                battle.Begin();Assert.That(battle.TryDeploy(battle.RunId,1,battle.Entry).Success,Is.True);
                var caster=battle.Defender;bool seen=false;
                for(int tick=0;tick<400&&caster.Alive;tick++)
                {
                    battle.Advance(.05f);yield return null;
                    seen|=Flow(root).Length==1;AssertActiveFrames(root,battle,clip);
                }
                Assert.That(seen,Is.True,"The real skill must have rendered before the ordinary attack kills its owner.");
                Assert.That(caster.Alive,Is.False);Assert.That(battle.Clock,Is.LessThan(caster.SkillActiveUntil));
                Assert.That(battle.CombatShots.Any(s=>!s.FromDefender&&s.TargetId==caster.EntityId&&s.Applied),Is.True,
                    "A normal applied unit attack must cause this death; no state/HP injection is used.");
                Assert.That(Flow(root),Is.Empty,"A dead owner cannot retain a looping field even before its original skill deadline.");
            }
            finally{if(root!=null)Object.Destroy(root);Time.timeScale=prior;}
            yield return null;
        }
    }
}
