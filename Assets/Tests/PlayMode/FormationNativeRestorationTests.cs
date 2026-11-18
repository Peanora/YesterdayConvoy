using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace YesterdayConvoy.Tests
{
    // Uses the production source reader, showcase fixture, timing bridge and effect presenter.
    // The fixture supplies targets/equipment; damage, SP, attacks and skills remain real events.
    public sealed class FormationNativeRestorationTests
    {
        const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        FormationBattleController controller;
        float previousScale;
        bool previousEnabled;
        bool capturedDriver;

        [SetUp] public void SaveClock() { previousScale = Time.timeScale;capturedDriver=false; }
        [TearDown] public void Freeze()
        {
            if (controller != null)
            {
                if (controller.Model != null) controller.Model.Paused = true;
                controller.enabled = previousEnabled;
            }
            Time.timeScale = previousScale;
        }

        static object Invoke(object owner, string name, params object[] arguments)
        {
            var method = owner.GetType().GetMethod(name, Members);
            Assert.That(method, Is.Not.Null, "Production method disappeared: " + name);
            return method.Invoke(owner, arguments);
        }
        static T Field<T>(object owner, string name)
        {
            var field = owner.GetType().GetField(name, Members);
            Assert.That(field, Is.Not.Null, "Production field disappeared: " + name);
            return (T)field.GetValue(owner);
        }
        static float Contact(FormationUnitArt source, string action) => (float)Invoke(source, "ContactTime", action);
        static void RequireGpu() => Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null),
            "These source-frame checks require the real GPU path; do not run with -nographics.");
        static IEnumerator WarmFrame(FormationUnitArt source, string action, double time, bool loop = false)
        {
            Assert.That(source.HasAction(action), Is.True, action);
            yield return (IEnumerator)Invoke(source, "WarmWindow", action, time, loop);
            Assert.That(source.Frame(action, time, loop), Is.Not.Null, "Production source window did not upload: " + action);
        }
        static void AssertPainted(Sprite sprite)
        {
            var visible = FormationArtAssertions.VisibleLocalBounds(sprite);
            Assert.That(visible.size.x, Is.GreaterThan(.01f), sprite.name);
            Assert.That(visible.size.y, Is.GreaterThan(.01f), sprite.name);
        }

        IEnumerator Fixture(string[] ids, int talulahCost = 3, int wisadelSkill = 1)
        {
            if (controller == null)
            {
                FormationShopSession.StartNew(20261118, fullCampaign: true);
                yield return SceneManager.LoadSceneAsync(FormationBattleController.SceneName);
                double deadline = Time.realtimeSinceStartupAsDouble + 600;
                while (Time.realtimeSinceStartupAsDouble < deadline)
                {
                    controller = Object.FindAnyObjectByType<FormationBattleController>();
                    if (controller != null && controller.Ready) break;
                    yield return null;
                }
                Assert.That(controller, Is.Not.Null);
                Assert.That(controller.Ready, Is.True);
            }
            if(!capturedDriver){previousEnabled=controller.enabled;capturedDriver=true;}
            // NUnit can reuse this test instance and scene. Re-disable the automatic
            // unscaled driver on every fixture, including after TearDown restored it.
            controller.enabled=false;
            yield return (IEnumerator)Invoke(controller, "PrepareShowcaseFixture", ids, talulahCost, wisadelSkill);
            Invoke(controller, "RefreshFormationAnimationProfiles");
        }
        void Begin()
        {
            Invoke(controller, "BeginShowcaseFixture");
            Invoke(controller, "RefreshFormationAnimationProfiles");
            Assert.That(controller.Model.Phase, Is.EqualTo(FormationPhase.Fighting));
            Assert.That(controller.Model.Speed, Is.EqualTo(1));
        }
        void Step(float seconds)
        {
            Invoke(controller, "RefreshFormationAnimationProfiles");
            controller.Model.Advance(seconds);
            Invoke(controller, "SyncActors");
        }
        IEnumerator Until(Func<bool> condition, double seconds = 50)
        {
            double end = controller.Model.Clock + seconds;
            for (int tick = 0; !condition() && controller.Model.Clock < end; tick++)
            {
                Step(.025f);
                if (tick % 8 == 0) yield return null;
            }
            Assert.That(condition(), Is.True, "The real production event did not occur before the fixture deadline.");
        }
        FormationAnimationProfile Profile(string artId) =>
            (FormationAnimationProfile)Field<IDictionary>(controller.Model, "animationProfiles")[artId];
        SpriteRenderer Body(FormationActor actor) => Field<Dictionary<int, SpriteRenderer>>(controller, "bodies")[actor.Id];
        (string key, double age, bool loop) Selected(FormationUnitArt source, FormationActor actor, string logical)
        {
            object[] args = { source, actor, controller.Model.Clock, false, logical, 0d, false };
            Invoke(controller, "SelectNativeFormationMotion", args);
            return ((string)args[4], (double)args[5], (bool)args[6]);
        }

        [UnityTest] public IEnumerator AprilOutfitExposesRealSkillAndDeathFramesWithOriginalMarkers()
        {
            RequireGpu();
            yield return Fixture(new[] { "WIS" }, wisadelSkill: 3);
            var source = FormationUnitArt.For("WISADEL_APRIL");
            Assert.That(source.Root, Is.EqualTo("BossRoster/WIS_APRIL_G9/main"));
            Assert.That(source.HasAction("walk"), Is.True, "The previous user-approved walk is layered onto the April outfit.");
            Assert.That(Contact(source, "attack"), Is.EqualTo(.600000024f).Within(.00001f));
            Assert.That(Contact(source, "Skill_1"), Is.EqualTo(.600000024f).Within(.00001f));
            Assert.That(Contact(source, "Skill_2_Loop"), Is.EqualTo(.266666681f).Within(.00001f));
            Assert.That(Contact(source, "Skill_3_Loop"), Is.EqualTo(.533333361f).Within(.00001f));
            Assert.That(float.IsNaN(Contact(source, "Skill_2_Begin")), Is.True);
            Assert.That(float.IsNaN(Contact(source, "Skill_3_Begin")), Is.True);
            Assert.That(Profile("WISADEL_APRIL").SkillContact,
                Is.EqualTo(source.Duration("Skill_3_Begin") + Contact(source, "Skill_3_Loop")).Within(.00001f),
                "The production S3 profile must include the genuine startup segment before its release marker.");
            foreach (string action in new[] { "Skill_1", "Skill_2_Loop", "Skill_3_Loop", "death" })
            {
                double time = action == "death" ? .5 : Contact(source, action);
                yield return WarmFrame(source, action, time);
                var frame = source.Frame(action, time, false);
                Assert.That(frame.texture.name.Replace('\\','/'), Does.StartWith("BossRoster/WIS_APRIL_G9/main/"));
                Assert.That(frame.texture.name, Does.Not.Contain("B20"));
                AssertPainted(frame);
            }
        }

        [UnityTest] public IEnumerator NaturalAprilAttackUsesTwoTimesSourceFramesAndSeparateLaunchImpact()
        {
            RequireGpu();
            yield return Fixture(new[] { "WIS" });
            Begin();
            var actor = controller.Model.Units.Single();
            var source = FormationUnitArt.For(actor.ArtId);
            Step(.35f);
            Assert.That(actor.IsMoving, Is.True);
            Assert.That(Selected(source, actor, "walk").key, Is.EqualTo("walk"));
            double walkAge=Field<double>(actor,"GroundDistanceTravelled")/.75;
            Assert.That(walkAge,Is.LessThan(controller.Model.Clock*2));
            Assert.That(Body(actor).sprite, Is.SameAs(source.Frame("walk",walkAge,true)));
            Assert.That(source.Frame("walk",0), Is.Not.SameAs(source.Frame("walk",.3)));
            yield return Until(() => controller.Model.PresentationActions.Any(a => a.ActorId == actor.Id && !a.Skill));
            var action = controller.Model.PresentationActions.First(a => a.ActorId == actor.Id && !a.Skill);
            Assert.That(action.Rate, Is.EqualTo(2).Within(.00001));
            Assert.That(controller.Model.Speed, Is.EqualTo(1));
            Assert.That(action.LaunchedAt - action.At, Is.EqualTo(.3).Within(.0001));
            Assert.That(action.ContactAt, Is.GreaterThan(action.LaunchedAt), "Windup and projectile travel are separate intervals.");
            Step(.1f);
            var motion = Selected(source, actor, "attack");
            Assert.That(motion.age, Is.EqualTo((controller.Model.Clock - action.At) * 2).Within(.0001));
            yield return WarmFrame(source, motion.key, motion.age, motion.loop);
            Invoke(controller, "SyncActors");
            Assert.That(Body(actor).sprite, Is.SameAs(source.Frame(motion.key, motion.age, motion.loop)));
            AssertPainted(Body(actor).sprite);
            Assert.That(controller.Model.Hits.Any(h => h.FromId == actor.Id), Is.False, "No damage exists before the real launch/impact.");
            bool VisibleLine(string name) => controller.GetComponentsInChildren<LineRenderer>().Any(line=>line.enabled&&line.name==name);
            yield return Until(()=>VisibleLine("April comic / approved ordinary shot"),1);
            Assert.That(controller.Model.Clock,Is.InRange(action.LaunchedAt,action.ContactAt));
            Assert.That(VisibleLine("April comic / approved black ink"),Is.False);
            Assert.That(controller.Model.Hits.Any(h=>h.FromId==actor.Id),Is.False);
            yield return Until(() => controller.Model.Hits.Any(h => h.FromId == actor.Id), 3);
            var hit = controller.Model.Hits.First(h => h.FromId == actor.Id);
            Assert.That(hit.At, Is.GreaterThanOrEqualTo(action.ContactAt - .0001));
            Assert.That(hit.Action, Is.SameAs(action), "The applied hit keeps its own release even when another action starts.");
            Assert.That(hit.Amount, Is.GreaterThan(0));
            Assert.That(VisibleLine("April comic / approved ordinary shot"),Is.False);
            Assert.That(VisibleLine("April comic / approved black ink"),Is.True);
            Assert.That(VisibleLine("April comic / approved yellow burst"),Is.True);
            Assert.That(LegacyLayers().Any(r=>r.sprite.name.Contains("wisdel_attack_")),Is.False);
        }

        [UnityTest] public IEnumerator AllThreeAprilSkillsDrawTheirOwnLayersDuringRealCombat()
        {
            RequireGpu();
            for (int skill=1;skill<=3;skill++)
            {
                yield return Fixture(new[]{"WIS"},wisadelSkill:skill);
                Begin();
                string mark="wisdel_skill_0"+skill;
                bool owner=false,impact=false,linked=false,headAtTarget=skill!=3,redTrail=false;
                FormationPresentationAction shell=null;
                yield return Until(()=>
                {
                    var layers=LegacyLayers().Where(r=>r.sprite.name.Contains(mark)).ToArray();
                    owner|=layers.Any(r=>(r.sprite.name.Contains("start")||r.sprite.name.Contains("buff"))&&HasPaint(r.sprite));
                    impact|=layers.Any(r=>r.sprite.name.Contains("hit")&&HasPaint(r.sprite));
                    if(skill==3&&!headAtTarget)
                    {
                        shell??=controller.Model.PresentationActions.FirstOrDefault(a=>a.ProjectileOnly);
                        if(shell!=null&&controller.Model.Clock>=shell.LaunchedAt&&controller.Model.Clock<shell.ContactAt)
                        {
                            redTrail|=layers.Any(r=>r.sprite.name.Contains("trail_game#9")&&HasPaint(r.sprite));
                            Assert.That(LegacyLayers().Any(r=>r.sprite.name.StartsWith("face-",StringComparison.Ordinal)),Is.False,
                                "The expression first appears at impact; only the projectile trail travels from the muzzle.");
                        }
                        if(shell!=null&&controller.Model.Clock>=shell.ContactAt)
                        {
                            var face=controller.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled&&
                                r.name=="Formation Wisadel / S3 impact face").ToArray();
                            if(face.Length>0)
                            {
                                var hit=controller.Model.Hits.First(h=>h.Action==shell&&!h.Secondary);
                                var target=controller.Model.Defenders.First(d=>d.Id==hit.ToId);
                                Vector3 point=(Vector3)Invoke(controller,"FormationFeedbackContactPoint",target,hit.To,.55f);
                                Assert.That(face.Length,Is.EqualTo(1),"One shell shows one target face, even when it hits several defenders.");
                                Assert.That(face[0].sprite.name,Does.StartWith("face-"));
                                Assert.That(face[0].sprite.bounds.size.y*face[0].transform.localScale.y,Is.GreaterThanOrEqualTo(1.6f));
                                Assert.That(controller.Model.Clock-hit.At,Is.InRange(.079,.451));
                                Assert.That(Vector3.Distance(face[0].transform.position,point),Is.LessThan(.01f));
                                Assert.That(redTrail,Is.True,"The current shell visibly travels before its actual damage and cut-in.");
                                headAtTarget=true;
                            }
                        }
                    }
                    linked|=controller.Model.Hits.Any(h=>!h.Heal&&h.SourceArtId=="WISADEL_APRIL"&&
                        (skill==2?h.Action!=null&&!h.Skill&&h.At>h.Action.At:h.Action?.ProjectileOnly==true));
                    return controller.Model.SkillCasts.Count>0&&owner&&impact&&linked&&headAtTarget;
                },60);
            }
        }

        [UnityTest] public IEnumerator FiveCostRainUsesGroundStreamsAcrossAllLanesAndFreezesWithCombat()
        {
            RequireGpu();
            yield return Fixture(new[]{"TAL"},talulahCost:5);
            Begin();
            var model=controller.Model;
            var caster=model.Units.Single();
            SpriteRenderer[] fire=Array.Empty<SpriteRenderer>();
            float first=float.PositiveInfinity,last=float.NegativeInfinity;
            double waveStart=double.PositiveInfinity;
            bool sawContact=false;
            var camera=Camera.main;
            yield return Until(()=>
            {
                sawContact|=controller.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.enabled&&
                    r.name=="Formation Talulah / rain impact"&&HasPaint(r.sprite))&&model.Hits.Any(h=>h.FromId==caster.Id&&h.Skill);
                fire=LegacyLayers().Where(r=>r.sprite.name.Contains("original-energy-stream")).ToArray();
                if(fire.Length>0)waveStart=Math.Min(waveStart,model.Clock);
                if(fire.Length!=2*FormationBattleModel.Height)return false;
                first=Mathf.Min(first,fire.Min(r=>r.transform.position.x));
                last=Mathf.Max(last,fire.Max(r=>r.transform.position.x));
                return last-first>8&&fire.Any(r=>HasPaint(r.sprite));
            },45);
            Assert.That(model.SkillCasts.Count,Is.EqualTo(1));
            Assert.That(sawContact,Is.True,"A committed hit should show the dedicated ground impact.");
            Assert.That(model.Clock-waveStart,Is.GreaterThan(2.3),"The fire front crosses the board over a slower, readable sweep.");
            Assert.That(model.Clock-waveStart,Is.LessThan(3.5),"The next wave starts before the first has finished.");
            Assert.That(fire.All(r=>r.sharedMaterial.shader.name=="YesterdayConvoy/TalulahGroundFlame"),Is.True);
            Assert.That(fire.Max(r=>r.transform.position.z)-fire.Min(r=>r.transform.position.z),Is.EqualTo(7).Within(.01));
            foreach(var stream in fire)
            {
                var ground=stream.transform.position-camera.transform.up*.12f;
                var cell=new Vector2(ground.x+(FormationBattleModel.Width-1)*.5f,ground.z+(FormationBattleModel.Height-1)*.5f);
                Assert.That(ground.y,Is.EqualTo(controller.ActorWorldPoint(cell).y).Within(.01));
            }
            Assert.That(controller.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.enabled&&r.name=="Falling fire"),Is.False);
            Vector3 point=fire[0].transform.position;
            var sprite=fire[0].sprite;
            model.Paused=true;
            Step(.4f);
            Assert.That(fire[0].transform.position,Is.EqualTo(point));
            Assert.That(fire[0].sprite,Is.SameAs(sprite));
            model.Paused=false;
            var sustainedBody=Body(caster).sprite;
            yield return Until(()=>Body(caster).sprite!=sustainedBody,.6);
            var cast=model.PresentationActions.Single(a=>a.ActorId==caster.Id&&a.Skill);
            double rainEnd=(double)Invoke(model,"TalulahRainEnd",caster);
            Assert.That(rainEnd-cast.ContactAt,Is.EqualTo(8).Within(.026));
            bool OrdinaryDuringRain() => model.PresentationActions.Any(a=>a.ActorId==caster.Id&&!a.Skill&&
                a.At>=cast.At&&a.At<rainEnd-1e-5);
            yield return Until(()=>
            {
                Assert.That(OrdinaryDuringRain(),Is.False);
                return model.Clock>=rainEnd;
            },10);
            yield return Until(()=>model.PresentationActions.Any(a=>a.ActorId==caster.Id&&!a.Skill&&a.At>=rainEnd),5);
            yield return Fixture(new[]{"TAL"},talulahCost:4);
            Assert.That(fire.All(r=>r==null||!r.enabled),Is.True);
        }

        [UnityTest] public IEnumerator FlamerazerPaintsAContinuousJetDuringTheActualSkill()
        {
            RequireGpu();
            yield return Fixture(new[]{"M31"});
            Begin();
            var actor=controller.Model.Units.Single();
            SpriteRenderer jet=null;
            yield return Until(()=>
            {
                jet=controller.GetComponentsInChildren<SpriteRenderer>().FirstOrDefault(r=>r.enabled&&r.name=="Formation Flamerazer / fire jet");
                return jet!=null&&controller.Model.Hits.Any(hit=>hit.FromId==actor.Id&&hit.Skill)&&HasPaint(jet.sprite);
            },45);
            var cast=controller.Model.PresentationActions.Single(a=>a.ActorId==actor.Id&&a.Skill);
            var firstFrame=jet.sprite;
            yield return Until(()=>
            {
                var flames=controller.GetComponentsInChildren<SpriteRenderer>();
                return flames.Any(r=>r.enabled&&r.name=="Formation Flamerazer / fire jet"&&r.sprite!=firstFrame)&&
                    flames.Any(r=>r.enabled&&r.name=="Formation Flamerazer / outer flame"&&HasPaint(r.sprite));
            },.6);
            Assert.That(controller.Model.Clock,Is.LessThan(cast.EndAt));
            yield return Until(()=>controller.Model.Clock>=cast.EndAt,4);
            Assert.That(controller.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.enabled&&r.name=="Formation Flamerazer / fire jet"),Is.False);
        }

        [UnityTest] public IEnumerator ProductionEquipmentSpeedsAprilFramesWithoutChangingBattleClockSpeed()
        {
            yield return Fixture(new[] { "WIS" });
            var piece = controller.Shop.Bench.Single();
            // Explicit equipment fixture: actual allocation and Equip transactions,
            // not a fabricated hit, forced skill charge or direct Interval assignment.
            for (int i = 0; i < 3; i++)
            {
                var item = controller.Campaign.AddItem("B");
                Assert.That(controller.Campaign.Equip(item.Instance, piece.Instance).Success, Is.True);
            }
            Begin();
            var actor = controller.Model.Units.Single();
            yield return Until(() => controller.Model.PresentationActions.Any(a => a.ActorId == actor.Id && !a.Skill));
            var action = controller.Model.PresentationActions.First(a => a.ActorId == actor.Id && !a.Skill);
            Assert.That(Profile(actor.ArtId).BaseRate, Is.EqualTo(2));
            Assert.That(action.Rate, Is.GreaterThan(2));
            Assert.That(actor.PresentationRate, Is.EqualTo(action.Rate));
            Assert.That(action.LaunchedAt - action.At, Is.EqualTo(.6 / action.Rate).Within(.0001));
            Assert.That(controller.Model.Speed, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator SameModelAprilStarChangeRefreshesTheTwelveShellSourceProfile()
        {
            yield return Fixture(new[] { "WIS" }, wisadelSkill: 3);
            var model = controller.Model;
            var actor = model.Units.Single();
            Assert.That(actor.Star, Is.EqualTo(1));
            float sixShellDuration = Profile(actor.ArtId).SkillDuration;
            // Explicit presentation metadata fixture: change only the source's shell
            // count input on this same actor/model. HP, attack, charge and cadence stay
            // untouched; the production profile refresh must invalidate its cache.
            actor.Star = 3;
            Invoke(controller, "RefreshFormationAnimationProfiles");
            Assert.That(controller.Model, Is.SameAs(model));
            Assert.That(Profile(actor.ArtId).SkillDuration - sixShellDuration,
                Is.EqualTo(6 * 1.1f * 2).Within(.0001f),
                "The twelve-shell body channel needs the six additional real loop intervals after an in-place star change.");
        }

        [UnityTest] public IEnumerator SuiKeepsItsDeploymentClawAndLandsOneWaveOnTheRealDefenderHits()
        {
            RequireGpu();
            yield return Fixture(new[] { "B21" });
            var actor = controller.Model.Units.Single();
            var source = FormationUnitArt.For(actor.ArtId);
            Assert.That(Body(actor).enabled, Is.True, "Preparation still shows the board's deployable claw.");
            AssertPainted(Body(actor).sprite);
            yield return WarmFrame(source, "Skill_Begin_A", .13333333);
            Begin();
            var model=controller.Model;
            var effects=Field<Dictionary<string,SpriteRenderer>>(controller,"formationAuxBodies");
            object ActiveCue() => Invoke(model,"ActiveSuiClawStrike",actor);
            Assert.That(Body(actor).enabled, Is.False, "Combat must not drag the giant deployment sprite along the board.");

            yield return Until(() => ActiveCue()!=null);
            var opening=ActiveCue();
            Assert.That(Field<int>(opening,"Wave"),Is.Zero);
            double firstImpact=Field<double>(opening,"ImpactAt");
            Assert.That(firstImpact-Field<double>(opening,"WarningAt"),Is.GreaterThanOrEqualTo(.35));

            object pulse=null;
            yield return Until(() =>
            {
                pulse=ActiveCue();
                return pulse!=null && Field<int>(pulse,"Wave")==1 &&
                    Field<double>(pulse,"ImpactAt")-model.Clock<=.15;
            },45);
            var drop=effects[actor.Id+"/sui/descent"];
            var shadow=effects[actor.Id+"/sui/impact"];
            double impact=Field<double>(pulse,"ImpactAt");
            Assert.That(impact-firstImpact,Is.EqualTo(4).Within(.026));
            var landing=controller.ActorWorldPoint(Field<Vector2>(pulse,"Center"));
            var sourcePosition=actor.Position;
            var before=drop.transform.position;
            var living=model.Defenders.Where(defender=>defender.Alive).Select(defender=>defender.Id).ToArray();
            double busyUntil=(double)Invoke(model,"PresentationBusyUntil",actor);
            Assert.That(drop.enabled && shadow.enabled, Is.True);
            AssertPainted(drop.sprite);
            Assert.That(before.y - landing.y, Is.GreaterThan(1));
            model.Paused=true;
            Step(.2f);
            Assert.That(drop.transform.position, Is.EqualTo(before));
            Assert.That(Field<bool>(pulse,"Resolved"), Is.False);
            model.Paused=false;

            yield return Until(() => Field<bool>(pulse,"Resolved"),1);
            Assert.That(Field<bool>(pulse,"Landed"), Is.True);
            double resolved=Field<double>(pulse,"ImpactAt");
            Assert.That(resolved,Is.EqualTo(impact).Within(.026));
            Assert.That(drop.transform.position.y-landing.y, Is.InRange(0,.32f));
            var ground=controller.GetComponentsInChildren<SpriteRenderer>().Single(r=>r.enabled&&
                r.name=="Boss FX / boss/B21/enemy_dyshhj_skill_01_hit_01");
            Assert.That(ground.transform.position.y-landing.y,Is.InRange(.025f,.075f));
            Assert.That(HasPaint(ground.sprite),Is.True);
            CollectionAssert.AreEquivalent(living,model.Hits.Where(hit=>hit.FromId==actor.Id && hit.Skill &&
                Math.Abs(hit.At-resolved)<.0001).Select(hit=>hit.ToId).ToArray(),"A single periodic slam still reaches every living defender once.");
            Assert.That(actor.Position,Is.EqualTo(sourcePosition),"The claw's impact position must not move the real unit to the guard.");
            var planted=drop.transform.position;
            Step(.90f);
            Assert.That(drop.enabled,Is.True,"The claw holds its planted pose before retreating.");
            Assert.That(drop.transform.position,Is.EqualTo(planted));
            Assert.That(drop.sprite.texture.name,Does.Contain("Skill_Idle_A"));
            yield return Until(()=>drop.enabled&&drop.sprite.texture.name.Contains("Skill_End_A"),1);
            yield return Until(() => !drop.enabled,1);
            Assert.That(model.SkillCasts.Count(c=>c.UnitId=="B21"),Is.EqualTo(1));
            Assert.That((double)Invoke(model,"PresentationBusyUntil",actor),Is.EqualTo(busyUntil));
        }

        [UnityTest] public IEnumerator NaturalAprilSecondSkillKeepsItsEightSecondBuffAndPaintsTheSourceEnding()
        {
            RequireGpu();
            yield return Fixture(new[] { "WIS" }, wisadelSkill: 2);
            Begin();
            var actor = controller.Model.Units.Single();
            var source = FormationUnitArt.For(actor.ArtId);
            yield return Until(() => controller.Model.PresentationActions.Any(a => a.ActorId == actor.Id && a.Skill), 40);
            var action = controller.Model.PresentationActions.First(a => a.ActorId == actor.Id && a.Skill);
            yield return Until(() => (float)Invoke(controller.Model, "PresentationBuff", actor, "wisadel") > 0, 3);
            double buffEnd = action.ContactAt + 8;
            yield return Until(() => controller.Model.Clock >= buffEnd - .1, 10);
            Assert.That((float)Invoke(controller.Model, "PresentationBuff", actor, "wisadel"), Is.GreaterThan(0),
                "The real S2 buff must still exist near the end of all eight gameplay seconds.");
            Assert.That(action.EndAt, Is.GreaterThanOrEqualTo(buffEnd - .0001));
            yield return Until(() => controller.Model.Clock > buffEnd + .001, 1);
            Assert.That((float)Invoke(controller.Model, "PresentationBuff", actor, "wisadel"), Is.EqualTo(0));
            Assert.That(controller.Model.Clock, Is.LessThan(buffEnd + source.Duration("Skill_2_End") / 2));
            controller.Model.Paused=true;
            var ending = Selected(source, actor, "idle");
            Assert.That(ending.key, Is.EqualTo("Skill_2_End"));
            yield return WarmFrame(source, ending.key, ending.age);
            Invoke(controller, "SyncActors");
            Assert.That(Body(actor).sprite, Is.SameAs(source.Frame(ending.key, ending.age, false)));
        }

        [UnityTest] public IEnumerator RiftbeastUsesItsNativeHeightInsteadOfTheFormerSmallBodyClamp()
        {
            RequireGpu();
            var definition = FormationUnitCatalog.Find("B22");
            var source = FormationUnitArt.For("B22");
            Assert.That(definition.VisualHeight, Is.EqualTo(source.Height).Within(.00001f));
            Assert.That(definition.VisualHeight, Is.GreaterThan(2.5f), "The former 1.10-body normalization must not return.");
            yield return WarmFrame(source, "idle", .2, true);
            var bounds = FormationArtAssertions.VisibleLocalBounds(source.Frame("idle", .2, true));
            Assert.That(bounds.size.y, Is.GreaterThan(2), "Validate painted GPU pixels, not an empty atlas cell.");
        }

        [UnityTest] public IEnumerator AllFireUsesTheRecapturedHighDensityPageWithPaintedPixels()
        {
            RequireGpu();
            var source = FormationUnitArt.For("B08");
            yield return WarmFrame(source, "idle", .2, true);
            var frame = source.Frame("idle", .2, true);
            Assert.That(frame.texture.name.Replace('\\','/'), Does.Contain("/hq-A_Idle-"));
            Assert.That(frame.pixelsPerUnit, Is.GreaterThan(90), "The old globally empty viewport had PPU20.397.");
            var bounds = FormationArtAssertions.VisibleLocalBounds(frame);
            Assert.That(bounds.size.y, Is.GreaterThan(2), "The source idle body is about2.387 high;3.030 is the full motion envelope.");
            Assert.That(source.Height, Is.GreaterThan(3));
        }

        [UnityTest] public IEnumerator AllFireSecondaryBurnShowsTheExistingFireAtItsAppliedContact()
        {
            RequireGpu();
            yield return Fixture(new[] { "B08" });
            Begin();
            var actor = controller.Model.Units.Single();
            yield return Until(() =>
            {
                var model = controller.Model;
                var burn = model.Hits.LastOrDefault(hit => hit.FromId == actor.Id && hit.Skill && hit.Secondary && !hit.Heal);
                if (burn == null || model.Clock - burn.At >= .35) return false;
                var point = controller.ActorWorldPoint(burn.To);
                return controller.GetComponentsInChildren<SpriteRenderer>().Any(renderer => renderer.enabled &&
                    renderer.name == "Boss FX / boss/B08/enemy_fthlgj_phase_1_skill_1_hit" &&
                    Vector3.Distance(renderer.transform.position, point) < .25f && HasPaint(renderer.sprite));
            }, 45);
        }

        [UnityTest] public IEnumerator PocketKingdomShowsEachActualCellAndClearsOnExpiry()
        {
            RequireGpu();
            yield return Fixture(new[] { "B15" });
            Begin();
            IReadOnlyList<Vector2Int> cells=null;
            SpriteRenderer[] Tiles()=>LegacyLayers().Where(renderer=>renderer.sprite.name.Contains("enemy_hlystp_skill01_range")).ToArray();
            yield return Until(()=>
            {
                var active=((IEnumerable)Invoke(controller.Model,"LegacyZones")).Cast<object>();
                var zone=active.FirstOrDefault(value=>Field<string>(value,"Kind")=="kingdom");
                if(zone==null)return false;
                cells=Field<IReadOnlyList<Vector2Int>>(zone,"Cells");
                return Tiles().Any(tile=>HasPaint(tile.sprite));
            },45);
            Assert.That(cells.Count,Is.EqualTo(7));
            var tiles=Tiles();
            Assert.That(tiles.Length,Is.EqualTo(cells.Count));
            foreach(var cell in cells)
            {
                var point=controller.ActorWorldPoint((Vector2)cell);
                Assert.That(tiles.Count(tile=>Vector3.Distance(tile.transform.position,point)<.04f),Is.EqualTo(1));
            }
            var frames=tiles.Select(tile=>tile.sprite).ToArray();
            controller.Model.Paused=true;
            Step(1);
            CollectionAssert.AreEqual(frames,Tiles().Select(tile=>tile.sprite));
            controller.Model.Paused=false;
            yield return Until(()=>Tiles().Length==0,11);
        }

        [UnityTest] public IEnumerator TalulahRealActionsShowRestoredLayersPauseAndClearThroughProductionReset()
        {
            RequireGpu();
            yield return Fixture(new[] { "TAL" }, talulahCost: 3);
            Begin();
            var actor = controller.Model.Units.Single();
            bool ordinary = false, caster = false, impact = false;
            yield return Until(() =>
            {
                var live = LegacyLayers();
                ordinary |= live.Any(r => r.sprite.name.Contains("attack-A-caster"));
                caster |= live.Any(r => r.sprite.name.Contains("ignite-A-caster"));
                impact |= live.Any(r => r.sprite.name.Contains("hit"));
                return ordinary && caster && impact && controller.Model.Hits.Any(h => h.FromId == actor.Id && h.Skill);
            }, 45);
            Assert.That(controller.Model.PresentationActions.Any(a => a.ActorId == actor.Id && a.Skill), Is.True);
            Assert.That(controller.Model.Hits.Any(h => h.FromId == actor.Id && !h.Skill && h.Amount > 0), Is.True);
            var layers = LegacyLayers();
            Assert.That(layers, Is.Not.Empty);
            var painted = layers.FirstOrDefault(r => HasPaint(r.sprite));
            Assert.That(painted, Is.Not.Null, "A restored production layer must contain real GPU pixels.");
            var before = layers.ToDictionary(r => r, r => (r.sprite, r.transform.position, r.transform.localScale));
            double clock = controller.Model.Clock;
            int actions = controller.Model.PresentationActions.Count, hits = controller.Model.Hits.Count;
            controller.Model.Paused = true;
            Step(1);
            yield return null;
            Assert.That(controller.Model.Clock, Is.EqualTo(clock));
            Assert.That(controller.Model.PresentationActions.Count, Is.EqualTo(actions));
            Assert.That(controller.Model.Hits.Count, Is.EqualTo(hits));
            foreach (var snapshot in before)
            {
                Assert.That(snapshot.Key.enabled, Is.True);
                Assert.That(snapshot.Key.sprite, Is.SameAs(snapshot.Value.sprite));
                Assert.That(snapshot.Key.transform.position, Is.EqualTo(snapshot.Value.position));
                Assert.That(snapshot.Key.transform.localScale, Is.EqualTo(snapshot.Value.localScale));
            }
            yield return Fixture(new[] { "TAL" }, talulahCost: 3);
            yield return null;
            Assert.That(layers.All(r => r == null || !r.enabled), Is.True, "Preparing the next production fixture disposes the old native effect renderers.");
            Assert.That(controller.Model.PresentationActions, Is.Empty);
            Assert.That(controller.Model.Hits, Is.Empty);
        }

        SpriteRenderer[] LegacyLayers() => controller.GetComponentsInChildren<SpriteRenderer>()
            .Where(r => r.enabled && r.sprite != null && r.name.StartsWith("Formation original / ", StringComparison.Ordinal)).ToArray();
        static bool HasPaint(Sprite sprite)
        {
            try { return FormationArtAssertions.VisibleLocalBounds(sprite).size.sqrMagnitude > .0001f; }
            catch (InvalidOperationException) { return false; } // A genuine source effect may begin with a blank frame.
        }
    }
}
