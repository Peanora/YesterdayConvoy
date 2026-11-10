using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed class BossArenaSmokeDriver:MonoBehaviour
    {
        static bool installed;
        [Serializable]sealed class Result{public bool passed;public int checks,failures,deployments;public double seconds;public string mode="actual-model-preset-squad",graphics;public List<string> errors=new List<string>();}
        readonly Result result=new Result();
        void Check(string label,bool value){result.checks++;if(!value){result.failures++;result.errors.Add(label);Debug.LogError("ARENA_PLAYER_CHECK_FAILED: "+label);}}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            if(installed||!Debug.isDebugBuild||!Environment.GetCommandLineArgs().Contains("-bossArenaSmokeTest"))return;
            installed=true;Application.runInBackground=true;var go=new GameObject("Boss arena explicit player verification");DontDestroyOnLoad(go);go.AddComponent<BossArenaSmokeDriver>();
        }
        IEnumerator Verify()
        {
            ManualBattleController c=null;
            for(int frame=0;frame<7200;frame++)
            {
                c=FindAnyObjectByType<ManualBattleController>();
                if(c!=null&&c.BossArenaActive&&c.CampaignActive&&!c.ArtLoading&&!c.ShopPanelTransitioning)break;
                yield return null;
            }
            Check("authored-arena-loaded",c!=null&&c.BossArenaActive&&c.CampaignActive&&!c.ArtLoading);
            if(c==null||!c.BossArenaActive||!c.CampaignActive||c.ArtLoading)yield break;
            result.graphics=SystemInfo.graphicsDeviceType.ToString();
            Check("one-skin-boss",c.Battle.Defenders.Count==1&&c.Battle.ArenaBoss.Settings.ArtId=="D31");
            Check("saved-terrain-no-inherited-devices",FindAnyObjectByType<AuthoredBattlefield>()!=null&&FindObjectsByType<GramophoneAnchor>(FindObjectsSortMode.None).Length==0);
            Check("ready-eight-squad",c.Battle.Cards.Count==8&&c.Battle.Units.Count==0&&c.Loop.CanShop);
            Time.timeScale=0;Check("start-via-production-control",c.StartLoopNextBattle());c.Tick(0);yield return null;
            Check("whole-squad-at-once",c.Battle.ArenaDeployments==8&&c.Battle.Cards.All(card=>c.Battle.ArenaCardOnField(card.Settings.CardId)));
            Check("no-DP-or-uses-spent",c.Battle.DP==0&&c.Battle.Cards.All(card=>card.RemainingUses==card.Settings.DeploymentLimit));
            Check("cannot-duplicate-alive-members",c.Battle.DeployReadyArenaSquad()==0);
            var visual=FindAnyObjectByType<DefenderRosterView>().FindVisual(BossArenaRules.BossId);
            Check("real-enlarged-sprite",visual!=null&&visual.sprite!=null&&Mathf.Abs(visual.transform.localScale.x-2.6f)<.001f);
            bool half=false,cooldown=false;long request=900000;
            for(int i=0;i<7200&&c.Battle.Phase==BattlePhase.Running;i++)
            {
                c.Battle.DeployReadyArenaSquad();foreach(var unit in c.Battle.Units.ToArray())if(unit.Active&&c.Battle.IsSkillReady(unit.EntityId))c.Battle.TryActivateSkill(c.Battle.RunId,++request,unit.EntityId);
                c.Tick(.1f);
                if(!cooldown&&c.Battle.Cards.Any(card=>c.Battle.ArenaRedeploymentRemaining(card.Settings.CardId)>0))
                {
                    cooldown=true;double clock=c.Battle.Clock;var remaining=c.Battle.Cards.Select(card=>c.Battle.ArenaRedeploymentRemaining(card.Settings.CardId)).ToArray();
                    float hp=c.Battle.ArenaBoss.Health;c.Battle.SetPaused(true);c.Tick(120);
                    Check("pause-freezes-clock-and-health",c.Battle.Clock==clock&&c.Battle.ArenaBoss.Health==hp);
                    Check("pause-freezes-death-cooldowns",remaining.SequenceEqual(c.Battle.Cards.Select(card=>c.Battle.ArenaRedeploymentRemaining(card.Settings.CardId))));c.Battle.SetPaused(false);
                }
                if(!half&&c.Battle.ArenaEnraged){half=true;Check("half-health-faster-real-skill",c.Battle.ArenaAttackSpeed==2&&c.Battle.ArenaBoss.Health<c.Battle.ArenaBoss.MaxHealth*.5f);}
                if(i%20==0)yield return null;
            }
            Check("natural-half-phase-and-death",half&&cooldown);Check("natural-boss-defeat",c.Battle.Phase==BattlePhase.Won&&c.Battle.ArenaBoss.Health==0);
            Check("real-redeployments-no-breach",c.Battle.ArenaDeployments>8&&c.Battle.Breaches==0&&c.Battle.DP==0);
            result.seconds=c.Battle.Clock;result.deployments=c.Battle.ArenaDeployments;
            var inventory=c.Shop.Bench.Select(p=>p.Id+":"+p.Star).ToArray();c.OpenArenaPreparation();yield return null;
            Check("return-to-preparation-keeps-build",c.Loop.CanShop&&c.ShopVisible&&inventory.SequenceEqual(c.Shop.Bench.Select(p=>p.Id+":"+p.Star)));
            Check("new-fight-resets-boss-and-redeployment",c.Battle.Phase==BattlePhase.Ready&&c.Battle.Units.Count==0&&c.Battle.ArenaBoss.Health==c.Battle.ArenaBoss.MaxHealth&&c.Battle.ArenaDeployments==0);
            Check("can-start-another-full-squad",c.StartLoopNextBattle()&&c.Battle.ArenaDeployments==8);Time.timeScale=1;
        }
        IEnumerator Start()
        {
            string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");
            string directory=at>=0&&at+1<args.Length?Path.GetFullPath(args[at+1]):Path.Combine(Application.persistentDataPath,"BossArenaEvidence");Directory.CreateDirectory(directory);
            var stack=new Stack<IEnumerator>();stack.Push(Verify());
            while(stack.Count>0)
            {
                object current=null;bool moved=false;Exception failure=null;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception e){failure=e;}
                if(failure!=null){Check("exception: "+failure,false);break;}
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator child){stack.Push(child);continue;}yield return current;
            }
            Time.timeScale=1;result.passed=result.failures==0;File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(result,true));
            Debug.Log("BOSS_ARENA_PLAYER_RESULT "+JsonUtility.ToJson(result));Application.Quit(result.passed?0:1);
        }
    }
}
