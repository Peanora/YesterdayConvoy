using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class DefenderInspectorLifecycleTests
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        ManualBattleController controller;
        float previousTimeScale;

        [SetUp]public void Setup()=>previousTimeScale=Time.timeScale;
        [TearDown]public void Cleanup()
        {
            if(controller!=null&&controller.ShopTrialActive)controller.LeaveShopTrial();
            Time.timeScale=previousTimeScale;
        }

        // These are controlled UI lifecycle fixtures. Only runtime state changes; saved assets and combat rules stay untouched.
        static void State(DefenderState defender,string property,object value)
            =>typeof(DefenderState).GetProperty(property).GetSetMethod(true).Invoke(defender,new[]{value});
        string RawSelection=>(string)typeof(ManualBattleController).GetField("inspectedDefender",Hidden).GetValue(controller);
        static Vector2 HitPoint(DefenderState defender)
        {
            var camera=Camera.main;
            Vector2 foot=TacticalViewport.Current.FromScreen(camera.WorldToScreenPoint(defender.Position));
            Vector2 head=TacticalViewport.Current.FromScreen(camera.WorldToScreenPoint(defender.Position+camera.transform.up*1.5f));
            return (foot+head)*.5f;
        }
        void Click(Vector2 point)
        {
            Assert.That(controller.ShopMapPointAvailable(point),Is.True,"The fixture click must reach the actual battlefield input.");
            controller.ProcessShopPointer(EventType.MouseDown,point);
            controller.ProcessShopPointer(EventType.MouseUp,point);
        }
        IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync(BlackForestLayout.SceneName);yield return null;
            double deadline=Time.realtimeSinceStartupAsDouble+180;
            do
            {
                controller=Object.FindAnyObjectByType<ManualBattleController>();
                if(controller!=null&&controller.BlackForestChallengeActive&&!controller.ArtLoading&&!controller.ShopPanelTransitioning)break;
                yield return null;
            }while(Time.realtimeSinceStartupAsDouble<deadline);
            Assert.That(controller,Is.Not.Null);Assert.That(controller.BlackForestChallengeActive,Is.True);
            Assert.That(controller.ArtLoading||controller.ShopPanelTransitioning,Is.False);
            Time.timeScale=0;
            typeof(ManualBattleController).GetField("inspectedSynergy",Hidden).SetValue(controller,null);
            Assert.That(controller.StartLoopNextBattle(),Is.True,"Use the production start button before testing battlefield pointer input.");
            deadline=Time.realtimeSinceStartupAsDouble+180;
            while((controller.ArtLoading||controller.ShopPanelTransitioning)&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            Assert.That(controller.ArtLoading||controller.ShopPanelTransitioning,Is.False);
            typeof(ManualBattleController).GetField("inspectedSynergy",Hidden).SetValue(controller,null);
            Assert.That(controller.Loop.CanDeploy,Is.True);
            // Isolate screen hit boxes so the overlap tests do not depend on the authored neighbours.
            int index=0;
            foreach(var defender in controller.Battle.Defenders)State(defender,"Position",new Vector3(100+index++*3,0,100));
            controller.Tick(0);
        }

        [UnityTest]public IEnumerator DeathClearsTheOpenPanelAndDeadFootprintsDoNotHideLivingOverlaps()
        {
            yield return Load();
            var dead=controller.Battle.FindDefender("D01");var living=controller.Battle.FindDefender("D08");
            State(dead,"Position",new Vector3(-4,.04f,.5f));controller.Tick(0);
            Vector2 oldPoint=HitPoint(dead);Click(oldPoint);
            Assert.That(controller.DefenderInspectorVisible,Is.True);Assert.That(controller.InspectedDefenderId,Is.EqualTo(dead.EntityId));
            State(dead,"Health",0f);Assert.That(dead.Alive,Is.False);controller.Tick(0);
            Assert.That(RawSelection,Is.Null,"The same controller tick must clear selection before the next GUI/property read.");
            Assert.That(controller.DefenderInspectorVisible,Is.False);Click(oldPoint);
            Assert.That(controller.InspectedDefenderId,Is.Null,"The retained dead model must not make its former position selectable.");
            State(living,"Position",dead.Position);controller.Tick(0);Click(oldPoint);
            Assert.That(controller.DefenderInspectorVisible,Is.True);Assert.That(controller.InspectedDefenderId,Is.EqualTo(living.EntityId),"A live unit at exactly the dead unit's position remains selectable.");
        }

        [UnityTest]public IEnumerator DebtLifeRemainsInspectableButDepartedAndReserveUnitsDoNot()
        {
            yield return Load();var guard=controller.Battle.FindDefender("D25");
            State(guard,"Position",new Vector3(-4,.04f,.5f));State(guard,"Health",-10f);State(guard,"DebtLife",true);
            controller.Tick(0);Vector2 point=HitPoint(guard);Assert.That(guard.Alive,Is.True);Click(point);
            Assert.That(controller.InspectedDefenderId,Is.EqualTo(guard.EntityId),"Hoshiguma's active debt-life phase is alive even below zero HP.");
            State(guard,"Health",guard.MaxHealth);State(guard,"DebtLife",false);State(guard,"Departed",true);controller.Tick(0);
            Assert.That(RawSelection,Is.Null);Click(point);Assert.That(controller.DefenderInspectorVisible,Is.False,"A departed unit stays absent even if it has positive HP.");
            State(guard,"Departed",false);State(guard,"Reserve",true);controller.Tick(0);Click(point);
            Assert.That(controller.InspectedDefenderId,Is.Null,"A not-yet-deployed reserve must not appear in battlefield inspection.");
            State(guard,"Reserve",false);controller.Tick(0);Click(point);
            Assert.That(controller.InspectedDefenderId,Is.EqualTo(guard.EntityId),"Once the reserve is actually present, it becomes selectable.");
            State(guard,"Reserve",true);controller.Tick(0);Assert.That(RawSelection,Is.Null,"Leaving the field closes an already open panel.");
        }

        [UnityTest]public IEnumerator ARelocatedMon3trUsesItsCurrentPositionAndRestartCannotInheritItsSelection()
        {
            yield return Load();var guard=controller.Battle.FindDefender("D23");
            State(guard,"Position",new Vector3(-5,.04f,3.5f));controller.Tick(0);
            Vector2 oldPoint=HitPoint(guard);Click(oldPoint);Assert.That(controller.InspectedDefenderId,Is.EqualTo(guard.EntityId));
            State(guard,"Position",new Vector3(-2,.04f,-1.5f));controller.Tick(0);
            Assert.That(controller.InspectedDefenderId,Is.EqualTo(guard.EntityId),"Relocation does not mean departure.");
            controller.CloseDefenderInspector();Click(oldPoint);Assert.That(controller.InspectedDefenderId,Is.Null,"The old location no longer owns a hit box.");
            Click(HitPoint(guard));Assert.That(controller.InspectedDefenderId,Is.EqualTo(guard.EntityId));
            string id=guard.EntityId;controller.Battle.Restart();controller.Tick(0);
            Assert.That(controller.Battle.FindDefender(id),Is.Not.SameAs(guard));Assert.That(controller.Battle.FindDefender(id).Alive,Is.True);
            Assert.That(RawSelection,Is.Null,"A fresh run's identically named defender must not inherit the old inspector.");
            Assert.That(controller.DefenderInspectorVisible,Is.False);
        }
    }
}
