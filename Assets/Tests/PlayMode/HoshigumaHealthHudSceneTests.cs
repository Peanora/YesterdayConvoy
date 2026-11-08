using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class HoshigumaHealthHudSceneTests
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        ManualBattleController controller;
        float previousScale;
        [SetUp]public void Setup()=>previousScale=Time.timeScale;
        [TearDown]public void Cleanup()
        {
            if(controller!=null&&controller.ShopTrialActive)controller.LeaveShopTrial();
            Time.timeScale=previousScale;
        }
        static void State(DefenderState defender,string property,object value)
            =>typeof(DefenderState).GetProperty(property).GetSetMethod(true).Invoke(defender,new[]{value});
        IEnumerator Ready(bool forest=false)
        {
            double deadline=Time.realtimeSinceStartupAsDouble+180;
            int stable=0;
            while(stable<2&&Time.realtimeSinceStartupAsDouble<deadline)
            {
                controller=Object.FindAnyObjectByType<ManualBattleController>();
                bool ready=controller!=null&&!controller.ArtLoading&&!controller.ShopPanelTransitioning&&(!forest||controller.BlackForestChallengeActive);
                stable=ready?stable+1:0;yield return null;
            }
            Assert.That(controller,Is.Not.Null);Assert.That(controller.ArtLoading||controller.ShopPanelTransitioning,Is.False);
            Assert.That(stable,Is.EqualTo(2),"The actual scene bootstrap must finish before writing the display fixture.");
            Time.timeScale=0;
            typeof(ManualBattleController).GetField("inspectedSynergy",Hidden).SetValue(controller,null);
            yield return null;
        }
        static Rect Bar(ManualBattleController owner,DefenderState defender)
        {
            float height=owner.ShopTrialActive?1.6f:owner.TacticalMapActive?1.6f:1.32f;
            Vector2 point=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(defender.Position+Camera.main.transform.up*height));
            float width=owner.ShopTrialActive?48:owner.TacticalMapActive?62:96;
            return new Rect(point.x-width/2,point.y,width,owner.ShopTrialActive?6:7);
        }
        IEnumerator CaptureRedHud(DefenderState defender,string name)
        {
            controller.Tick(0);yield return null;yield return null;
            var screen=ScreenCapture.CaptureScreenshotAsTexture();Assert.That(screen,Is.Not.Null);
            try
            {
                Rect bounds=Bar(controller,defender);int red=0;var viewport=TacticalViewport.Current;
                Vector2 topLeft=viewport.ToScreen(new Vector2(bounds.xMin,bounds.yMin));
                Vector2 bottomRight=viewport.ToScreen(new Vector2(bounds.xMax,bounds.yMax));
                int minX=Mathf.Clamp(Mathf.FloorToInt(topLeft.x),0,screen.width-1),maxX=Mathf.Clamp(Mathf.CeilToInt(bottomRight.x),0,screen.width-1);
                int minY=Mathf.Clamp(Mathf.FloorToInt(bottomRight.y),0,screen.height-1),maxY=Mathf.Clamp(Mathf.CeilToInt(topLeft.y),0,screen.height-1);
                for(int y=minY;y<=maxY;y++)for(int x=minX;x<=maxX;x++)
                {
                    Color pixel=screen.GetPixel(x,y);
                    if(pixel.r>.7f&&pixel.r>pixel.g*1.7f&&pixel.r>pixel.b*1.7f)red++;
                }
                string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"..","artifacts","defender-feedback"));Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory,name+".png"),screen.EncodeToPNG());
                Assert.That(red,Is.GreaterThan(2),"The actual OnGUI health-bar rectangle must contain red pixels, not just a model flag: "+name+" "+bounds);
                Debug.Log("HOSHIGUMA_RED_HEALTH_HUD: "+name+"; redPixels="+red+"; controlledRuntimeState=True; noCombatRuleChange=True");
            }
            finally{Object.Destroy(screen);}
        }

        [UnityTest]public IEnumerator ShopAndStandaloneHudRenderActualRedDebtIncludingZeroDebtEntry()
        {
            yield return SceneManager.LoadSceneAsync(BlackForestLayout.SceneName);yield return null;
            controller=Object.FindAnyObjectByType<ManualBattleController>();yield return Ready(true);
            Assert.That(controller.BlackForestChallengeActive,Is.True);controller.SetShopVisible(false);yield return new WaitForSecondsRealtime(.3f);
            var guard=controller.Battle.FindDefender("D25");Assert.That(guard,Is.Not.Null);
            // Controlled display fixture, not a claim that a natural battle reached this state.
            State(guard,"Health",0f);State(guard,"DebtLife",true);State(guard,"Debt",guard.MaxHealth*.5f);
            var state=DefenderHealthPresentation.Read(guard);Assert.That(state.Visible&&state.IsDebt,Is.True);Assert.That(state.Fraction,Is.EqualTo(.25f));
            yield return CaptureRedHud(guard,"hoshiguma-debt-bar");
            State(guard,"Debt",0f);yield return CaptureRedHud(guard,"hoshiguma-zero-debt-red-phase");
            State(guard,"Departed",true);Assert.That(DefenderHealthPresentation.Read(guard).Visible,Is.False);

            yield return SceneManager.LoadSceneAsync("MonsterTrial");yield return null;
            controller=Object.FindAnyObjectByType<ManualBattleController>();yield return Ready();
            Assert.That(controller.TryEnterDefenderSkillTrial("D25"),Is.True);yield return null;yield return Ready();
            Assert.That(controller.ShopTrialActive,Is.False);guard=controller.Battle.FindDefender("D25");Assert.That(guard,Is.Not.Null);
            State(guard,"Health",0f);State(guard,"DebtLife",true);State(guard,"Debt",guard.MaxHealth*.5f);
            yield return CaptureRedHud(guard,"hoshiguma-standalone-debt-bar");
        }
    }
}
