using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        [Serializable] sealed class SarkazEvidenceResult
        {
            public bool passed;
            public string scene;
            public string scope="Saved SarkazTrial, explicit one-star sandbox tier presets and existing sandbox stat multipliers. Production pointer deployment, real HP, attack, shields and simulation timers. No injected HP, damage, SP, clock or forced result. Automated actions are not human mouse verification.";
            public string[] checks;
            public float twoTierAttack,twoTierDamage,nineTierAttack,nineTierDamage,nineTierHealth,nineTierShield;
        }

        IEnumerator SarkazEvidenceReady()
        {
            for(int i=0;i<1800&&(ArtLoading||ShopPanelTransitioning);i++)yield return null;
            if(ArtLoading||ShopPanelTransitioning)throw new InvalidOperationException("Sarkaz preparation did not become ready.");
            yield return null;
        }

        IEnumerator SarkazEvidenceCapture(string directory,string name)
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,name+".png"));
            yield return null;yield return null;
        }

        bool SarkazEvidenceDrag(string cardId)
        {
            int slot=shop.Bench.ToList().FindIndex(p=>p.Id==cardId);
            if(slot<0||Camera.main==null)return false;
            int before=Battle.Units.Count;
            var gate=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(Battle.Entry));
            ProcessShopPointer(EventType.MouseDown,ShopPieceRect(slot).center);
            ProcessShopPointer(EventType.MouseDrag,gate);
            ProcessShopPointer(EventType.MouseUp,gate);
            return Battle.Units.Count==before+1&&Battle.Units.Last().CardId==cardId;
        }

        internal IEnumerator VerifySarkazContract(string directory)
        {
            Directory.CreateDirectory(directory);
            var result=new SarkazEvidenceResult{scene=SceneManager.GetActiveScene().name};
            var checks=new List<string>();bool valid=true,completed=false;
            float previousScale=Time.timeScale;
            Action<string,bool> check=(name,ok)=>{checks.Add(name+"="+ok);valid&=ok;Debug.Log("SARKAZ_PLAYER "+name+"="+ok);};
            try
            {
                diagnostic=true;Application.runInBackground=true;Time.timeScale=0;
                yield return SarkazEvidenceReady();
                check("saved-sarkaz-trial",activeSynergyTrial?.trialId=="sarkaz"&&SynergySandbox&&FindAnyObjectByType<AuthoredBattlefield>()!=null);
                var allMembers=SynergySummary.From(shop.Roles.Select(r=>r.Id)).Single(s=>s.Name=="萨卡兹");
                check("eleven-members-five-thresholds",allMembers.Count==11&&allMembers.Thresholds.SequenceEqual(new[]{2,4,6,8,9}));
                int[] tiers={2,4,6,8,9};
                float[] life={1.1f,1.2f,1.3f,1.4f,1.6f},attack={.35f,.60f,.90f,1.20f,1.50f},shield={.20f,.30f,.40f,.50f,.60f};
                for(int i=0;i<tiers.Length;i++)
                {
                    int tier=tiers[i];string prefix="tier-"+tier+"-";
                    if(i>0){ReplaySandbox();yield return SarkazEvidenceReady();}
                    if(!SetSarkazTierPreset(tier))throw new InvalidOperationException("Cannot request Sarkaz tier "+tier+".");
                    yield return SarkazEvidenceReady();
                    check(prefix+"real-roster",Battle.SarkazTier==tier&&shopLineup.Count==tier&&shopLineup.Distinct().Count()==tier);
                    check(prefix+"select-core",SetRemainingChoices("CARD_M37",MarineAdaptation.Barrier,MarineAdaptation.Hunt));
                    if(tier==9)
                    {
                        inspectedSynergy="萨卡兹";
                        yield return SarkazEvidenceCapture(directory,"01-nine-tier-preparation");
                        inspectedSynergy=null;
                    }
                    if(!StartLoopNextBattle())throw new InvalidOperationException("Cannot begin Sarkaz tier "+tier+".");
                    yield return SarkazEvidenceReady();
                    Battle.Begin();Battle.SetPaused(true);
                    int dp=Battle.DP;
                    check(prefix+"paused-normal-drag",SarkazEvidenceDrag("CARD_M37"));
                    var core=Battle.Units.Last(u=>u.CardId=="CARD_M37"&&!u.SynergySupport);
                    float baseHealth=core.Profile?.Unit.Health??Battle.Settings.Health;
                    check(prefix+"actual-health-and-cost",Math.Abs(core.MaxHealth-baseHealth*life[i])<.02f&&Math.Abs(core.Health-core.MaxHealth)<.02f&&Battle.DP==dp-Battle.FindCard(core.CardId).Settings.DeploymentCost);
                    // The caster's original twenty-second shackle would suppress the
                    // guard throughout the first automatic refill. Use the ordinary
                    // thrower in the larger presets so natural hits can consume ward.
                    string partner=tier>=6&&shopLineup.Contains("CARD_M26")?"CARD_M26":
                        shopLineup.First(id=>id!="CARD_M37"&&id!="CARD_M28"&&id!="CARD_M42"&&id!="CARD_M43");
                    check(prefix+"second-normal-drag",SarkazEvidenceDrag(partner));
                    var other=Battle.Units.Last(u=>u.CardId==partner&&!u.SynergySupport);
                    check(prefix+"paused-cannot-cast",!UseContract()&&Battle.ContractUses==0);
                    double pausedAt=Battle.Clock;
                    Tick(2);
                    check(prefix+"paused-clock",Battle.Clock==pausedAt);
                    Battle.SetPaused(false);
                    int uses=Battle.ContractUses,resource=Battle.DP;float sp=core.SkillSP;
                    if(tier<9)
                    {
                        check(prefix+"ready-without-injury",Battle.CanActivateContract&&UseContract()&&Battle.ContractUses==uses+1&&Battle.DP==resource&&core.SkillSP==sp);
                        long command=requestSequence;
                        float ward=Battle.ContractShieldRemaining(core);double cooldown=Battle.ContractCooldownRemaining;
                        check(prefix+"request-retry-is-idempotent",Battle.TryActivateContract(Battle.RunId,command)&&Battle.ContractUses==uses+1&&Math.Abs(Battle.ContractShieldRemaining(core)-ward)<.01f&&Battle.ContractCooldownRemaining==cooldown);
                        check(prefix+"twenty-second-cooldown",Math.Abs(Battle.ContractCooldownRemaining-20)<.001&&!UseContract());
                    }
                    else
                    {
                        check(prefix+"automatic-with-two-deployed",Battle.Units.Count==2&&Battle.ContractUses==0&&!Battle.CanActivateContract&&!UseContract());
                        result.nineTierHealth=core.MaxHealth;result.nineTierShield=Battle.ContractShieldRemaining(core);
                    }
                    check(prefix+"core-attack-and-ward",Math.Abs(Battle.ContractAttackBonus(core)-attack[i])<.001f&&Math.Abs(Battle.ContractShieldRemaining(core)-core.MaxHealth*shield[i])<.02f);
                    float share=tier<6?0:tier==6?.5f:1;
                    check(prefix+"ally-share",Math.Abs(Battle.ContractAttackBonus(other)-attack[i]*share)<.001f&&Math.Abs(Battle.ContractShieldRemaining(other)-other.MaxHealth*shield[i]*share)<.02f);
                    Battle.SetPaused(true);
                    double remaining=Battle.ContractRemaining,cd=Battle.ContractCooldownRemaining,refresh=Battle.ContractShieldRefreshRemaining(core),clock=Battle.Clock;
                    float shieldBefore=Battle.ContractShieldRemaining(core);
                    Tick(3);
                    check(prefix+"pause-freezes-effect-and-shield",Battle.Clock==clock&&Battle.ContractRemaining==remaining&&Battle.ContractCooldownRemaining==cd&&Battle.ContractShieldRefreshRemaining(core)==refresh&&Battle.ContractShieldRemaining(core)==shieldBefore);
                    if(tier==2||tier==6||tier==9)yield return SarkazEvidenceCapture(directory,"0"+(i+2)+"-tier-"+tier+"-active");
                    Battle.SetPaused(false);
                    if(tier==2||tier==9)
                    {
                        CombatShot shot=null;
                        for(int step=0;step<200&&Battle.Phase==BattlePhase.Running;step++)
                        {
                            Tick(.05f);
                            shot=Battle.CombatShots.FirstOrDefault(s=>!s.FromDefender&&s.SourceId==core.EntityId&&s.Resolved&&s.Applied&&s.DamageDealt>0);
                            if(shot!=null)break;
                            if(step%4==0)yield return null;
                        }
                        float expected=Battle.UnitCombat(core).UnitAttack*(1+attack[i]);
                        check(prefix+"ordinary-shot-deals-real-boosted-damage",shot!=null&&Math.Abs(shot.Attack-expected)<.05f&&shot.DamageDealt>0&&Battle.Defenders.Any(d=>d.Health<d.MaxHealth));
                        if(shot!=null)
                        {
                            if(tier==2){result.twoTierAttack=shot.Attack;result.twoTierDamage=shot.DamageDealt;}
                            else {result.nineTierAttack=shot.Attack;result.nineTierDamage=shot.DamageDealt;}
                        }
                        Battle.SetPaused(true);yield return SarkazEvidenceCapture(directory,"0"+(i+3)+"-tier-"+tier+"-actual-hit");Battle.SetPaused(false);
                    }
                    if(tier==9)
                    {
                        bool refreshed=false,sawAbsorption=false;
                        for(int step=0;step<600&&Battle.Phase==BattlePhase.Running&&core.Active;step++)
                        {
                            Tick(.05f);
                            sawAbsorption|=Battle.CombatFeedback.Any(e=>e.TargetType==CombatFeedbackTarget.Monster&&e.TargetId==core.EntityId&&e.Kind==CombatFeedbackKind.ShieldAbsorbed);
                            if(Battle.Clock>=20&&Battle.ContractShieldRefreshRemaining(core)>19.8)
                            {refreshed=true;break;}
                            if(step%4==0)yield return null;
                        }
                        check(prefix+"natural-hits-consume-ward",sawAbsorption);
                        check(prefix+"automatic-twenty-second-refill",refreshed&&Math.Abs(Battle.ContractShieldRemaining(core)-core.MaxHealth*.60f)<.02f&&Math.Abs(Battle.ContractAttackBonus(core)-1.5f)<.001f&&Battle.ContractUses==0);
                        Battle.SetPaused(true);yield return SarkazEvidenceCapture(directory,"08-nine-tier-auto-refill");
                    }
                }
                Battle.SetPaused(false);ReplaySandbox();yield return SarkazEvidenceReady();
                check("replay-clears-unit-ward-and-cooldown",Battle.Clock==0&&Battle.Units.Count==0&&Battle.ContractUses==0&&Battle.ContractCooldownRemaining==0);
                completed=true;
            }
            finally
            {
                Time.timeScale=previousScale;inspectedSynergy=null;
                result.passed=valid&&completed;result.checks=checks.ToArray();
                File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(result,true));
                Debug.Log("SARKAZ_CONTRACT_PLAYER_PASS="+result.passed+"; checks="+checks.Count+"; attack2="+result.twoTierAttack+"; attack9="+result.nineTierAttack);
            }
        }
    }

    // Opt-in only: the shortcut leaves the saved scene under ordinary player control.
    public sealed class SarkazContractSmokeDriver:MonoBehaviour
    {
        static bool installed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Install()
        {
            var args=Environment.GetCommandLineArgs();
            bool smoke=Debug.isDebugBuild&&args.Contains("-sarkazContractTest");
            if(installed||(!smoke&&!args.Contains("-sarkazTrial")))return;
            installed=true;var go=new GameObject("Sarkaz explicit verification");DontDestroyOnLoad(go);go.AddComponent<SarkazContractSmokeDriver>();
        }
        IEnumerator Verify(string directory,bool smoke)
        {
            if(smoke)Application.runInBackground=true;
            yield return null;
            if(SceneManager.GetActiveScene().name!="SarkazTrial")yield return SceneManager.LoadSceneAsync("SarkazTrial");
            if(!smoke)yield break;
            double deadline=Time.realtimeSinceStartupAsDouble+600;ManualBattleController controller=null;
            while(Time.realtimeSinceStartupAsDouble<deadline)
            {
                controller=FindAnyObjectByType<ManualBattleController>();
                if(controller!=null&&controller.SynergySandbox&&controller.Loop.CanShop&&!controller.ArtLoading&&!controller.ShopPanelTransitioning)break;
                yield return null;
            }
            if(controller==null||!controller.SynergySandbox||!controller.Loop.CanShop||controller.ArtLoading||controller.ShopPanelTransitioning)throw new InvalidOperationException("SarkazTrial startup timed out.");
            yield return controller.VerifySarkazContract(directory);
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();bool smoke=Debug.isDebugBuild&&args.Contains("-sarkazContractTest");
            int at=Array.IndexOf(args,"-demoEvidenceDir");
            string directory=at>=0&&at+1<args.Length?Path.GetFullPath(args[at+1]):Path.GetFullPath("artifacts/sarkaz-player");
            if(smoke)Directory.CreateDirectory(directory);
            var stack=new Stack<IEnumerator>();stack.Push(Verify(directory,smoke));
            while(stack.Count>0)
            {
                object current=null;Exception failure=null;bool moved=false;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception error){failure=error;}
                if(failure!=null)
                {
                    while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Debug.LogException(failure);
                    if(smoke){File.WriteAllText(Path.Combine(directory,"exception.txt"),failure.ToString());Application.Quit(1);}
                    yield break;
                }
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator child){stack.Push(child);continue;}yield return current;
            }
            if(smoke)
            {
                bool passed=File.Exists(Path.Combine(directory,"result.json"))&&File.ReadAllText(Path.Combine(directory,"result.json")).Contains("\"passed\": true");
                Application.Quit(passed?0:1);
            }
            else Destroy(gameObject);
        }
    }
}
