using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        static void TalulahVisualRequire(bool condition,string text)
        {if(!condition)throw new InvalidOperationException("TALULAH_NATIVE: "+text);}
        static SpriteRenderer[] NativeTalulahAttacks()=>FindObjectsByType<SpriteRenderer>().Where(r=>r.enabled&&r.name.StartsWith("Talulah native / attack-")).ToArray();
        // Development-player evidence, called only by the existing explicit Talulah smoke switch.
        IEnumerator VerifyTalulahNativeLayers(string directory)
        {
            Directory.CreateDirectory(directory);Time.timeScale=0;SwitchTrial(true);
            while(ArtLoading)yield return null;
            SelectCard(true);TalulahVisualRequire(TryDeployAt(Battle.Entry).Success,"Deployment failed");
            for(int i=0;i<200&&!Battle.CombatShots.Any(s=>!s.FromDefender&&!s.FireRain);i++)Tick(.025f);
            var shot=Battle.CombatShots.First(s=>!s.FromDefender&&!s.FireRain);
            Tick((float)(shot.ImpactAt+.08-Battle.Clock));yield return null;
            var effects=NativeTalulahAttacks();
            TalulahVisualRequire(effects.Any(r=>r.name.EndsWith("attack-A-caster"))&&effects.Any(r=>r.name.EndsWith("attack-A-hit")),"Separate caster/target effects missing");
            TalulahVisualRequire(effects.All(r=>r.sharedMaterial.shader.name=="YesterdayConvoy/TalulahNative"&&r.sharedMaterial.shader.isSupported),"Expected supported native shader");
            var visible=PatriotCapture(Path.Combine(directory,"talulah-native-attack.png"));
            foreach(var r in effects)r.enabled=false;var hidden=PatriotCapture(null);foreach(var r in effects)r.enabled=true;
            double delta=visible.Zip(hidden,(a,b)=>(double)(Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b))).Average();
            TalulahVisualRequire(delta>.02,"Native attack changed no camera pixels");
            var caster=effects.First(r=>r.name.EndsWith("attack-A-caster"));var frame=caster.sprite;
            double clock=Battle.Clock;Battle.SetPaused(true);Tick(5);yield return null;
            TalulahVisualRequire(Battle.Clock==clock&&caster.sprite==frame,"Native attack advanced while paused");
            RestartBattle();yield return null;
            TalulahVisualRequire(NativeTalulahAttacks().Length==0,"Restart left attack layers alive");
            SelectCard(true);TalulahVisualRequire(TryDeployAt(Battle.Entry).Success,"Death fixture deployment failed");Tick(.1f);yield return null;
            var unit=Battle.Units[0];var visual=GameObject.Find(unit.EntityId).GetComponentInChildren<DemoSpriteVisual>();
            // Explicit fixture exercises the real defeat/lifecycle path; not a natural-combat claim.
            unit.Health=0;
            typeof(ManualBattle).GetMethod("DefeatUnit",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(Battle,new object[]{unit});
            TalulahVisualRequire(Battle.FinishForTesting(Battle.RunId,false),"Could not enter terminal fixture");
            Tick(0);yield return null;clock=Battle.Clock;var position=unit.Motion.Position;
            TalulahVisualRequire(visual.FrameState=="Death"&&!SettlementVisible,"Death was removed before playing");
            int first=visual.FrameIndex;Tick(1);yield return null;
            TalulahVisualRequire(visual.FrameIndex>first&&Battle.Clock==clock&&unit.Motion.Position==position,"Terminal death did not animate independently of combat");
            PatriotCapture(Path.Combine(directory,"talulah-native-death.png"));
            Tick(1.7f);yield return null;
            TalulahVisualRequire(ViewCount==0&&SettlementVisible,"Full death tail did not finish and clean up");
            RestartBattle();yield return null;
            File.WriteAllText(Path.Combine(directory,"talulah-native-result.json"),"{\"passed\":true,\"nativeAttack\":true,\"deathFixture\":true,\"deathSeconds\":2.6666667,\"gpuDelta\":"+delta.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");
            Debug.Log("TALULAH_NATIVE_PASS: actual caster/hit pixels, pause, terminal death, cleanup; death uses an explicit fixture");
        }
    }
}
