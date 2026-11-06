using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattle
    {
        // Only the explicit Player fixture supplies a seed, so it can cover all three outcomes in one cast.
        internal void SeedWolfEvidence(int seed)=>wolfRolls=new System.Random(seed);
    }
    public sealed class SilverWolfTrial : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void BackgroundForEvidence()
        {
            if(Debug.isDebugBuild && Environment.GetCommandLineArgs().Contains("-silverWolfSmokeTest"))Application.runInBackground=true;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();
            if(!args.Contains("-silverWolfTrial")&&!(Debug.isDebugBuild&&args.Contains("-silverWolfSmokeTest")))return;
            var go=new GameObject("Silver Wolf trial launcher");DontDestroyOnLoad(go);go.AddComponent<SilverWolfTrial>();
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();bool verify=args.Contains("-silverWolfSmokeTest")&&Debug.isDebugBuild;
            if(SceneManager.GetActiveScene().name!="MonsterTrial")yield return SceneManager.LoadSceneAsync("MonsterTrial");
            ManualBattleController c=null;
            for(int i=0;i<1200;i++){c=FindAnyObjectByType<ManualBattleController>();if(c!=null && c.Battle!=null&&!c.ArtLoading)break;yield return null;}
            if(c==null||c.Battle==null){Debug.LogError("Silver Wolf trial unavailable");if(verify)Application.Quit(1);yield break;}
            var option=c.MonsterTests.FirstOrDefault(o=>o.Id==SilverWolfCombat.CardId&&o.Playable);
            if(option==null){Debug.LogError("Silver Wolf not present in playable catalog");if(verify)Application.Quit(1);yield break;}
            if(verify) Debug.Log("WOLF_PLAYER: preparing authored trial");
            c.LoadWolfTrial(option);while(c.ArtLoading)yield return null;
            if(verify)
            {
                Application.runInBackground=true;int i=Array.IndexOf(args,"-demoEvidenceDir");
                yield return c.VerifyWolf(i>=0&&i+1<args.Length?args[i+1]:Application.persistentDataPath);
            }
            Destroy(gameObject);
        }
    }
    public sealed partial class ManualBattleController
    {
        internal void LoadWolfTrial(MonsterTestOption option)=>PrepareMonsterEntry(option);
        internal IEnumerator VerifyWolf(string folder)
        {
            Directory.CreateDirectory(folder);diagnostic=true;Time.timeScale=0;
            yield return new WaitForSecondsRealtime(1.5f);
            var checks=new List<string>();bool valid=true;
            Action<string,bool> check=(name,value)=>{checks.Add(name+"="+value);valid&=value;Debug.Log("WOLF_CHECK "+name+"="+value);};
            check("normal-menu-entry",definition.Card.CardId==SilverWolfCombat.CardId&&!TestSelectionOpen);
            for(int pass=0;pass<2;pass++)
            {
                // Second pass deliberately approaches the same slug along a left-facing route.
                // Both passes use the authored card, costs, SP, targeting and real world-space effects.
                if(pass==1)
                {
                    var target=definition.Engagement.DefenderPosition;
                    ClearViews();CancelInteraction();Battle=new ManualBattle(definition.Snapshot(),
                        new[]{target+Vector3.right*6,target-Vector3.right*7},target,definition.Engagement.Snapshot());
                    int seed=Enumerable.Range(0,100).First(n=>{var rng=new System.Random(n);return new[]{rng.Next(3),rng.Next(3),rng.Next(3)}.Distinct().Count()==3;});
                    Battle.SeedWolfEvidence(seed);EnableCombatView();
                }
                SelectCard(true);var receipt=TryDeployAt(Battle.Entry);check("deploy-"+pass,receipt.Success);
                if(!receipt.Success)break;
                var u=Battle.FindUnit(receipt.EntityId);Tick(.35f);yield return null;
                var actor=FindObjectsByType<DemoSpriteVisual>(FindObjectsSortMode.None).Single(v=>v.EntityId==u.EntityId && v.gameObject.activeInHierarchy);
                bool facing=Vector3.Dot(u.Motion.Direction,Camera.main.transform.right)<0;
                check("walk-"+pass,actor.FrameState=="Walk"&&u.IsMoving&&actor.Renderer.flipX==facing);
                if(pass==1)check("actual-left-route",facing);
                yield return ViviEvidenceFrame(folder,pass==0?"01-normal-walk":"03-walk-left");
                for(int i=0;i<900 && double.IsNegativeInfinity(u.LastAttackTime);i++)Tick(.05f);
                Tick(.88f);yield return null;
                check("actual-uppercut-"+pass,actor.FrameState=="Attack"&&Battle.CombatShots.Any(s=>s.SourceId==u.EntityId&&s.Applied&&s.WolfBox==WolfBoxKind.None));
                check("actual-slug-"+pass,combatView.DefenderVisual.sprite!=null&&combatView.DefenderVisual.sprite.name.StartsWith("OriginiumSlug/"));
                yield return ViviEvidenceFrame(folder,pass==0?"02-normal-punch":"04-left-punch");
                if(pass==0)continue;
                for(int n=0;n<400&&!Battle.IsSkillReady(u.EntityId);n++)Tick(.05f);
                SelectSkillUnit(u.EntityId);check("manual-transform",ActivateSelectedSkill()==SkillFailure.None);
                for(int n=0;n<80 && Battle.Clock<u.WolfTransformAt+.90;n++)Tick(.05f);
                yield return null;
                check("visor-transformation",actor.FrameState=="WolfTransform");yield return ViviEvidenceFrame(folder,"05-visor-left");
                var seen=new HashSet<WolfBoxKind>();int captures=0;
                for(int i=0;i<8400 && seen.Count<3 && u.Active && Battle.Phase==BattlePhase.Running;i++)
                {
                    if(u.SkillAmmo==0&&Battle.IsSkillReady(u.EntityId)){SelectSkillUnit(u.EntityId);ActivateSelectedSkill();}
                    Tick(.05f);
                    if(i%8==0)yield return null;
                    var shot=Battle.CombatShots.LastOrDefault(s=>s.SourceId==u.EntityId&&s.WolfBox!=WolfBoxKind.None);
                    if(shot==null||!shot.Applied||seen.Contains(shot.WolfBox)||Battle.Clock<shot.ImpactAt+.1||Battle.Clock>shot.ImpactAt+.35)continue;
                    yield return null;
                    seen.Add(shot.WolfBox);
                    check("visible-left-"+shot.WolfBox,actor.FrameState=="Wolf"+shot.WolfBox&&actor.Renderer.flipX&&combatView.WolfEffectCount>0&&shot.DamageDealt>0);
                    yield return ViviEvidenceFrame(folder,"06-left-"+shot.WolfBox);
                    if(captures++==0){double clock=Battle.Clock;int index=actor.FrameIndex;Battle.SetPaused(true);Tick(2);yield return null;check("pause",Battle.Clock==clock&&actor.FrameIndex==index);Battle.SetPaused(false);}
                }
                check("three-loot-outcomes",seen.Count==3);
            }
            RestartBattle();yield return null;check("reset",Battle.Units.Count==0&&Battle.CombatShots.Count==0&&combatView.WolfEffectCount==0);
            File.WriteAllLines(Path.Combine(folder,"wolf-checks.txt"),checks);Debug.Log("WOLF_PLAYER_PASS="+valid);
            Application.Quit(valid?0:1);
        }
    }
}
