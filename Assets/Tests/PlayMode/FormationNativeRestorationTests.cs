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
            Step(.025f);
            Assert.That(actor.IsMoving, Is.True);
            Assert.That(Selected(source, actor, "walk").key, Is.EqualTo("walk"));
            Assert.That(Body(actor).sprite, Is.SameAs(source.Frame("walk",controller.Model.Clock*2,true)));
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
            yield return Until(() => controller.Model.Hits.Any(h => h.FromId == actor.Id), 3);
            var hit = controller.Model.Hits.First(h => h.FromId == actor.Id);
            Assert.That(hit.At, Is.GreaterThanOrEqualTo(action.ContactAt - .0001));
            Assert.That(hit.Action, Is.SameAs(action), "The applied hit keeps its own release even when another action starts.");
            Assert.That(hit.Amount, Is.GreaterThan(0));
        }

        [UnityTest] public IEnumerator AllThreeAprilSkillsDrawTheirOwnLayersDuringRealCombat()
        {
            RequireGpu();
            for (int skill=1;skill<=3;skill++)
            {
                yield return Fixture(new[]{"WIS"},wisadelSkill:skill);
                Begin();
                string mark="wisdel_skill_0"+skill;
                bool owner=false,impact=false,linked=false;
                yield return Until(()=>
                {
                    var layers=LegacyLayers().Where(r=>r.sprite.name.Contains(mark)).ToArray();
                    owner|=layers.Any(r=>(r.sprite.name.Contains("start")||r.sprite.name.Contains("buff"))&&HasPaint(r.sprite));
                    impact|=layers.Any(r=>r.sprite.name.Contains("hit")&&HasPaint(r.sprite));
                    linked|=controller.Model.Hits.Any(h=>!h.Heal&&h.SourceArtId=="WISADEL_APRIL"&&
                        (skill==2?h.Action!=null&&!h.Skill&&h.At>h.Action.At:h.Action?.ProjectileOnly==true));
                    return controller.Model.SkillCasts.Count>0&&owner&&impact&&linked;
                },60);
            }
        }

        [UnityTest] public IEnumerator FiveCostRainDrawsAboveEmptyCellsAcrossTheArenaAndFreezesWithCombat()
        {
            RequireGpu();
            yield return Fixture(new[]{"TAL"},talulahCost:5);
            Begin();
            SpriteRenderer[] drops=Array.Empty<SpriteRenderer>();
            yield return Until(()=>
            {
                drops=controller.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled&&r.name=="Falling fire").ToArray();
                return drops.Length>=12&&drops.Max(r=>r.transform.position.x)-drops.Min(r=>r.transform.position.x)>7&&
                    drops.Max(r=>r.transform.position.z)-drops.Min(r=>r.transform.position.z)>5;
            },45);
            Assert.That(controller.Model.SkillCasts.Count,Is.EqualTo(1));
            Assert.That(drops.Any(r=>HasPaint(r.sprite)),Is.True,"The active waves contain actual flame pixels.");
            Vector3 point=drops[0].transform.position;
            var sprite=drops[0].sprite;
            controller.Model.Paused=true;
            Step(.4f);
            Assert.That(drops[0].transform.position,Is.EqualTo(point));
            Assert.That(drops[0].sprite,Is.SameAs(sprite));
            yield return Fixture(new[]{"TAL"},talulahCost:4);
            Assert.That(drops.All(r=>r==null||!r.enabled),Is.True);
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
                Is.EqualTo(6 * .7f * 2).Within(.0001f),
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
            yield return WarmFrame(source, "Skill_Loop_A", .73333335);
            Begin();
            var model=controller.Model;
            var effects=Field<Dictionary<string,SpriteRenderer>>(controller,"formationAuxBodies");
            object ActiveCue() => Invoke(model,"ActiveSuiClawStrike",actor);
            Assert.That(Body(actor).enabled, Is.False, "Combat must not drag the giant deployment sprite along the board.");

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
            Assert.That(drop.transform.position.y-landing.y, Is.InRange(0,.1f));
            CollectionAssert.AreEquivalent(living,model.Hits.Where(hit=>hit.FromId==actor.Id && hit.Skill &&
                Math.Abs(hit.At-resolved)<.0001).Select(hit=>hit.ToId).ToArray(),"A single periodic slam still reaches every living defender once.");
            Assert.That(actor.Position,Is.EqualTo(sourcePosition),"The claw's impact position must not move the real unit to the guard.");
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
