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
    public sealed class SeventhDefenderFlightSceneTests
    {
        // Explicit aerial-state fixture: the ordinary Player roster test deploys
        // ground hounds. This test does not claim a naturally earned boss phase.
        [UnityTest]public IEnumerator RealAerialInterceptionDrivesProductionMoveBodyTrailPauseAndAirBlock()
        {
            float priorTime=Time.timeScale;RenderTexture target=null;Texture2D read=null;Camera camera=null;RenderTexture oldTarget=null,oldActive=RenderTexture.active;
            try
            {
                yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;Time.timeScale=0;
                var c=Object.FindAnyObjectByType<ManualBattleController>();Assert.That(c.TryEnterDefenderSkillTrial("D33"),Is.True);yield return null;yield return null;
                var skill=new DefenderSkillSettings(DefenderSkillKind.AngelinaWishes,1,1,1,99999,attackMultiplier:1.3f,effectRadius:10);
                var guard=new DefenderSettings("D33","Aerial interception fixture","D33",Vector3.zero,100000,20,0,10,1.5f,windup:.4f,actionDuration:1.5f,skill:skill);
                var combat=new EngagementSettings(unitAttack:0,unitArmor:0,unitRange:1,unitInterval:10,projectileTime:.2f,canAttack:false,hasSkill:false,initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,effects:new MonsterEffectSettings(CombatDamageType.Arts,mandragora:true));
                var battle=new ManualBattle(new ManualBattleSettings("flight-fixture","flight-target",30,1,12,1,.1f,100000,.05f,1,1,1,1),new[]{Vector3.right*5,Vector3.right*30},Vector3.zero,combat,roster:new[]{guard});
                typeof(ManualBattleController).GetProperty("Battle",BindingFlags.Public|BindingFlags.Instance).SetValue(c,battle);
                foreach(string key in DefenderVfxArt.RequiredKeysForArtIds(new[]{"D33"}))yield return OriginalCombatVfxArt.Get(key).Sequence.Warm();
                battle.Begin();Assert.That(battle.TryDeploy(battle.RunId,1,battle.Entry).Success,Is.True);
                // Mandragora's established flying second phase supplies a legitimate
                // aerial/blockable capability to the isolated fixture.
                var airborne=battle.Units.Single();airborne.MandraPhase=MandragoraPhase.Second;airborne.StoneShield=true;
                Assert.That(battle.UnitAirborne(airborne),Is.True);
                camera=Camera.main;oldTarget=camera.targetTexture;target=new RenderTexture(640,360,24);read=new Texture2D(640,360,TextureFormat.RGB24,false);camera.targetTexture=target;
                Color32[] Capture(){camera.Render();RenderTexture.active=target;read.ReadPixels(new Rect(0,0,640,360),0,0);read.Apply();return read.GetPixels32();}
                bool bodyMoved=false,trailVisible=false,paused=false,blocked=false,restart=false;int pixels=0;Vector3 origin=battle.Defender.Position;
                for(int i=0;i<200;i++)
                {
                    battle.Advance(.05f);yield return null;
                    var d=battle.Defender;var body=Object.FindAnyObjectByType<DefenderRosterView>().FindVisual("D33");
                    if(d.SeventhMoving)
                    {
                        bodyMoved|=(d.Position-origin).magnitude>.1f&&body.transform.position==d.Position&&body.sprite.name.Contains("/skill_move/");
                        var trails=Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&r.name.EndsWith("/aglna2_skill_03_fly_trail_01",StringComparison.Ordinal)).ToArray();
                        if(trails.Length>0&&pixels<=4)
                        {
                            var shown=Capture();foreach(var r in trails)r.enabled=false;var hidden=Capture();foreach(var r in trails)r.enabled=true;int changed=0;
                            for(int p=0;p<shown.Length;p++)if(Math.Abs(shown[p].r-hidden[p].r)+Math.Abs(shown[p].g-hidden[p].g)+Math.Abs(shown[p].b-hidden[p].b)>12)changed++;
                            pixels=Math.Max(pixels,changed);trailVisible|=pixels>4;
                        }
                        if(bodyMoved&&trailVisible&&!paused)
                        {
                            battle.SetPaused(true);var position=d.Position;var sprite=body.sprite;double clock=battle.Clock;battle.Advance(5);yield return null;
                            Assert.That(battle.Clock,Is.EqualTo(clock));Assert.That(d.Position,Is.EqualTo(position));Assert.That(body.sprite,Is.SameAs(sprite));paused=true;battle.SetPaused(false);
                        }
                    }
                    if(!d.SeventhMoving&&d.SeventhMoveEndedAt>0&&battle.Clock-d.SeventhMoveEndedAt<1d/3)restart|=body.sprite.name.Contains("/skill_restart/");
                    blocked|=airborne.IsBlocked&&airborne.BlockerId==d.EntityId;
                    if(bodyMoved&&trailVisible&&paused&&blocked&&restart)break;
                }
                Debug.Log("SEVENTH_AIR_FIXTURE body="+bodyMoved+" trailPixels="+pixels+" paused="+paused+" blocked="+blocked+" restart="+restart);
                Assert.That(bodyMoved&&trailVisible&&paused&&blocked&&restart,Is.True);
                Assert.That(battle.DefenderGroundBlockCapacity(battle.Defender),Is.Zero);Assert.That(battle.DefenderAirBlockCapacity(battle.Defender),Is.EqualTo(2));
                battle.Restart();yield return null;Assert.That(battle.Defender.Position,Is.EqualTo(origin));Assert.That(battle.Defender.SeventhMoving,Is.False);Assert.That(battle.SeventhDefenderEvents,Is.Empty);
            }
            finally
            {
                if(camera!=null)camera.targetTexture=oldTarget;RenderTexture.active=oldActive;if(target!=null){target.Release();Object.Destroy(target);}if(read!=null)Object.Destroy(read);Time.timeScale=priorTime;
            }
        }
    }
}
