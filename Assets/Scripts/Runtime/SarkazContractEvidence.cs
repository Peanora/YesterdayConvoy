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
            public string scope="Saved SarkazTrial; production sandbox presets, badge/card pointer dragging, real HP, attacks and one-time deployment wards. No injected HP, damage, SP, clock or forced result. Automated production input is not human mouse verification.";
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
            yield return null;yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,name+".png"));
            yield return null;yield return null;
        }

        int SarkazEvidenceSlot(string cardId)=>shop.Bench.ToList().FindIndex(p=>p.Id==cardId&&IsLineupPiece(p));
        string SarkazEvidenceInventory()=>shop.Gold+"/"+string.Join(",",shopLineup)+"/"+
            string.Join(",",shop.Bench.Select(p=>p.Id+":"+p.Instance+":"+p.Star))+"/"+string.Join(",",shop.Offers)+"/"+
            string.Join(",",shop.Roles.OrderBy(r=>r.Id).Select(r=>r.Id+":"+shop.Available(r.Id)));
        bool SarkazEvidenceDrag(string cardId)
        {
            int slot=SarkazEvidenceSlot(cardId);if(slot<0||Camera.main==null)return false;
            int before=Battle.Units.Count;var gate=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(Battle.Entry));
            ProcessShopPointer(EventType.MouseDown,ShopPieceRect(slot).center);
            ProcessShopPointer(EventType.MouseDrag,gate);ProcessShopPointer(EventType.MouseUp,gate);
            return Battle.Units.Count==before+1&&Battle.Units.Last().CardId==cardId;
        }
        void SarkazEvidenceBadgeDrag(Vector2 target)
        {
            ProcessShopPointer(EventType.MouseDown,SarkazContractBadgeRect.center);
            ProcessShopPointer(EventType.MouseDrag,target);ProcessShopPointer(EventType.MouseUp,target);
        }

        internal IEnumerator VerifySarkazContract(string directory)
        {
            Directory.CreateDirectory(directory);var result=new SarkazEvidenceResult{scene=SceneManager.GetActiveScene().name};
            var checks=new List<string>();bool valid=true,completed=false;float previousScale=Time.timeScale;
            Action<string,bool> check=(name,ok)=>{checks.Add(name+"="+ok);valid&=ok;Debug.Log("SARKAZ_PLAYER "+name+"="+ok);};
            try
            {
                diagnostic=true;Application.runInBackground=true;Time.timeScale=0;
                yield return SarkazEvidenceReady();campaignConstructOpen=false;inspectedSynergy=null;
                check("saved-sarkaz-trial",activeSynergyTrial?.trialId=="sarkaz"&&SynergySandbox&&FindAnyObjectByType<AuthoredBattlefield>()!=null);
                var allMembers=SynergySummary.From(shop.Roles.Select(r=>r.Id)).Single(s=>s.Name=="萨卡兹");
                check("eleven-members-five-thresholds",allMembers.Count==11&&allMembers.Thresholds.SequenceEqual(new[]{2,4,6,8,9}));

                // The existing mixed contract preset contains a real non-Sarkaz M18.
                // It uses the ordinary finite stock and art preparation paths.
                PrepareSandboxRoster(6);yield return SarkazEvidenceReady();
                string inventory=SarkazEvidenceInventory();int slot=SarkazEvidenceSlot("CARD_M37");
                check("mixed-preset-has-real-member-and-outsider",slot>=0&&SarkazEvidenceSlot("CARD_M18")>=0&&Battle.SarkazTier==6);
                var target=ShopPieceRect(slot).center;
                ProcessShopPointer(EventType.MouseDown,SarkazContractBadgeRect.center);ProcessShopPointer(EventType.MouseDrag,target);
                check("badge-uses-production-pointer-drag",SarkazContractBadgeDragging);
                yield return SarkazEvidenceCapture(directory,"01-badge-drag-to-sarkaz");
                ProcessShopPointer(EventType.MouseUp,target);
                check("badge-selects-sarkaz-without-moving-card",PreparedContractCard=="CARD_M37"&&!SarkazContractBadgeDragging&&SarkazEvidenceInventory()==inventory);
                yield return SarkazEvidenceCapture(directory,"02-sarkaz-marked-contract-owner");
                string owner=PreparedContractCard;target=ShopPieceRect(SarkazEvidenceSlot("CARD_M18")).center;
                SarkazEvidenceBadgeDrag(target);
                check("outsider-drop-reports-exact-message",ShopFeedback=="无法使羁绊外的角色成为承契者");
                check("outsider-drop-preserves-owner-stock-and-gold",PreparedContractCard==owner&&!SarkazContractBadgeDragging&&SarkazEvidenceInventory()==inventory);
                yield return SarkazEvidenceCapture(directory,"03-outsider-rejected-with-message");
                ProcessShopPointer(EventType.MouseUp,target);
                check("repeat-release-has-no-side-effects",PreparedContractCard==owner&&SarkazEvidenceInventory()==inventory);
                ProcessShopPointer(EventType.MouseDown,SarkazContractBadgeRect.center);ProcessShopPointer(EventType.MouseDrag,new Vector2(800,600));
                CancelShopDrag();ProcessShopPointer(EventType.MouseUp,new Vector2(800,600));
                check("cancel-badge-preserves-owner-and-inventory",PreparedContractCard==owner&&!SarkazContractBadgeDragging&&SarkazEvidenceInventory()==inventory);

                int[] tiers={2,4,6,8,9};
                float[] life={1.1f,1.2f,1.3f,1.4f,1.6f},attack={.35f,.60f,.90f,1.20f,1.50f},shield={.20f,.30f,.40f,.50f,.60f};
                for(int i=0;i<tiers.Length;i++)
                {
                    int tier=tiers[i];string tag="tier-"+tier+"-";
                    if(i>0){ReplaySandbox();yield return SarkazEvidenceReady();}
                    if(!SetSarkazTierPreset(tier))throw new InvalidOperationException("Cannot request Sarkaz tier "+tier+".");
                    yield return SarkazEvidenceReady();
                    check(tag+"real-roster",Battle.SarkazTier==tier&&shopLineup.Count==tier&&shopLineup.Distinct().Count()==tier);
                    SarkazEvidenceBadgeDrag(ShopPieceRect(SarkazEvidenceSlot("CARD_M37")).center);
                    check(tag+"badge-selects-core",PreparedContractCard=="CARD_M37"&&!SarkazContractBadgeDragging);
                    if(tier==9)
                    {inspectedSynergy="萨卡兹";yield return SarkazEvidenceCapture(directory,"04-nine-tier-permanent-rules");inspectedSynergy=null;}
                    if(!StartLoopNextBattle())throw new InvalidOperationException("Cannot begin Sarkaz tier "+tier+".");
                    yield return SarkazEvidenceReady();Battle.Begin();Battle.SetPaused(true);
                    owner=PreparedContractCard;
                    SarkazEvidenceBadgeDrag(ShopPieceRect(SarkazEvidenceSlot("CARD_M38")).center);
                    check(tag+"battle-locks-contract-owner",PreparedContractCard==owner&&!SarkazContractBadgeDragging&&Battle.Units.Count==0);
                    int dp=Battle.DP;
                    check(tag+"paused-normal-deployment",SarkazEvidenceDrag("CARD_M37"));
                    var core=Battle.Units.Last(u=>u.CardId=="CARD_M37"&&!u.SynergySupport);
                    float baseHealth=core.Profile?.Unit.Health??Battle.Settings.Health;
                    check(tag+"actual-health-and-cost",Math.Abs(core.MaxHealth-baseHealth*life[i])<.02f&&Math.Abs(core.Health-core.MaxHealth)<.02f&&Battle.DP==dp-Battle.FindCard(core.CardId).Settings.DeploymentCost);
                    check(tag+"deployment-immediately-grants-permanent-attack-and-ward",Math.Abs(Battle.ContractAttackBonus(core)-attack[i])<.001f&&Math.Abs(Battle.ContractShieldRemaining(core)-core.MaxHealth*shield[i])<.02f&&double.IsPositiveInfinity(Battle.ContractRemaining));
                    string partner=tier>=6?"CARD_M26":"CARD_M38";
                    check(tag+"second-normal-deployment",SarkazEvidenceDrag(partner));
                    var other=Battle.Units.Last(u=>u.CardId==partner&&!u.SynergySupport);float share=tier<6?0:tier==6?.5f:1;
                    check(tag+"ally-share-at-deployment",Math.Abs(Battle.ContractAttackBonus(other)-attack[i]*share)<.001f&&Math.Abs(Battle.ContractShieldRemaining(other)-other.MaxHealth*shield[i]*share)<.02f);
                    float ward=Battle.ContractShieldRemaining(core);double clock=Battle.Clock;int resource=Battle.DP;float sp=core.SkillSP;
                    Tick(3);
                    check(tag+"pause-does-not-spend-or-refill",Battle.Clock==clock&&Battle.ContractShieldRemaining(core)==ward&&Battle.DP==resource&&core.SkillSP==sp);
                    Battle.SetPaused(false);
                    check(tag+"manual-entry-retired",!Battle.CanActivateContract&&!Battle.TryActivateContract(Battle.RunId,++requestSequence)&&Battle.ContractUses==0&&Battle.ContractCooldownRemaining==0&&Battle.ContractNextShieldRemaining==0);
                    if(tier==2||tier==6||tier==9)
                    {Battle.SetPaused(true);yield return SarkazEvidenceCapture(directory,"0"+(i+5)+"-tier-"+tier+"-deployment");Battle.SetPaused(false);}
                    if(tier==2||tier==9)
                    {
                        CombatShot shot=null;
                        for(int step=0;step<200&&Battle.Phase==BattlePhase.Running;step++)
                        {
                            Tick(.05f);shot=Battle.CombatShots.FirstOrDefault(s=>!s.FromDefender&&s.SourceId==core.EntityId&&s.Resolved&&s.Applied&&s.DamageDealt>0);
                            if(shot!=null)break;if(step%4==0)yield return null;
                        }
                        float expected=Battle.UnitCombat(core).UnitAttack*(1+attack[i]);
                        check(tag+"ordinary-shot-deals-real-boosted-damage",shot!=null&&Math.Abs(shot.Attack-expected)<.05f&&shot.DamageDealt>0&&Battle.Defenders.Any(d=>d.Health<d.MaxHealth));
                        if(shot!=null)
                        {if(tier==2){result.twoTierAttack=shot.Attack;result.twoTierDamage=shot.DamageDealt;}else{result.nineTierAttack=shot.Attack;result.nineTierDamage=shot.DamageDealt;}}
                        Battle.SetPaused(true);yield return SarkazEvidenceCapture(directory,"0"+(i+6)+"-tier-"+tier+"-actual-hit");Battle.SetPaused(false);
                    }
                    if(tier==9)
                    {
                        result.nineTierHealth=core.MaxHealth;result.nineTierShield=core.MaxHealth*.60f;
                        bool sawAbsorption=false,increased=false,afterTwenty=false;float previousWard=Battle.ContractShieldRemaining(core);
                        for(int step=0;step<650&&Battle.Phase==BattlePhase.Running&&core.Active;step++)
                        {
                            Tick(.05f);float currentWard=Battle.ContractShieldRemaining(core);
                            increased|=currentWard>previousWard+.02f;previousWard=currentWard;
                            sawAbsorption|=Battle.CombatFeedback.Any(e=>e.TargetType==CombatFeedbackTarget.Monster&&e.TargetId==core.EntityId&&e.Kind==CombatFeedbackKind.ShieldAbsorbed);
                            if(Battle.Clock>=21){afterTwenty=true;break;}if(step%4==0)yield return null;
                        }
                        check(tag+"natural-hits-consume-one-time-ward",sawAbsorption);
                        check(tag+"no-twenty-second-refill-and-permanent-attack",afterTwenty&&core.Active&&!increased&&Battle.ContractShieldRemaining(core)<core.MaxHealth*.60f-.01f&&Math.Abs(Battle.ContractAttackBonus(core)-1.5f)<.001f);
                        Battle.SetPaused(true);yield return SarkazEvidenceCapture(directory,"11-nine-tier-one-time-ward-after-twenty-seconds");
                    }
                }
                Battle.SetPaused(false);ReplaySandbox();yield return SarkazEvidenceReady();
                check("replay-clears-old-entities-and-one-time-wards",Battle.Clock==0&&Battle.Units.Count==0&&Battle.ContractUses==0);
                completed=true;
            }
            finally
            {
                Time.timeScale=previousScale;inspectedSynergy=null;CancelShopDrag();result.passed=valid&&completed;result.checks=checks.ToArray();
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
