using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        [Serializable] sealed class DamageRankingResult
        {
            public bool passed;
            public string[] checks;
            public float totalDamage;
            public string[] ranking,displayRanking;
            public string phase;
        }
        // Explicit development fixture: actual pointer deployment and normal battle simulation.
        // No fixture numbers are inserted into the damage ledger.
        public IEnumerator VerifyDamageRanking()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");
            string folder=at>=0&&at+1<args.Length?args[at+1]:Path.GetFullPath("artifacts/damage-ranking-player");Directory.CreateDirectory(folder);
            var checks=new List<string>();bool passed=true;float oldScale=Time.timeScale;diagnostic=true;Application.runInBackground=true;
            Action<string,bool> check=(name,ok)=>{checks.Add(name+"="+ok);passed&=ok;Debug.Log("DAMAGE_RANKING_EVENT "+name+"="+ok);};
            try
            {
                yield return PrepareCampaign(true,73191);Time.timeScale=0;
                for(int i=0;i<1800&&(ArtLoading||ShopPanelTransitioning);i++)yield return null;
                check("readyWithoutDamage",DamageRankingVisible&&Battle.TotalPlayerDamage==0&&Battle.DamageRanking.Count==0);
                check("panelBlocksWorld",!ShopMapPointAvailable(DamageRankingPanelRect.center));
                int gold=shop.Gold,count=shop.Bench.Count;string[] offers=shop.Offers.ToArray();
                ProcessShopPointer(EventType.MouseDown,DamageRankingPanelRect.center);ProcessShopPointer(EventType.MouseUp,DamageRankingPanelRect.center);
                check("rankingDoesNotPurchase",gold==shop.Gold&&count==shop.Bench.Count&&offers.SequenceEqual(shop.Offers));
                yield return ApprovedUiFrame(folder,"01-preparation-zero-damage");
                damageRankingScroll=new Vector2(0,10000);
                yield return ApprovedUiFrame(folder,"01b-preparation-scroll-bottom");
                check("overflowRowsCanScroll",damageRankingRows.Count>5&&damageRankingScroll.y>0);
                damageRankingScroll=Vector2.zero;
                check("startBattle",StartLoopNextBattle());
                for(int i=0;i<1800&&(ArtLoading||ShopPanelTransitioning);i++)yield return null;
                Battle.Begin();Battle.SetPaused(true);
                foreach(string id in new[]{"CARD_M07","CARD_M08","CARD_M11","CARD_M12","CARD_M13","CARD_M06","CARD_M14"})
                {
                    int index=shop.Bench.ToList().FindIndex(p=>p.Id==id);int before=Battle.Units.Count(u=>!u.SynergySupport);
                    Vector2 from=ShopPieceRect(index).center,to=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(Battle.Entry));
                    ProcessShopPointer(EventType.MouseDown,from);ProcessShopPointer(EventType.MouseDrag,to);ProcessShopPointer(EventType.MouseUp,to);
                    check("pointerDeploy-"+id,Battle.Units.Count(u=>!u.SynergySupport)==before+1);
                }
                check("pauseDeploymentHasNoDamage",Battle.TotalPlayerDamage==0&&Battle.Clock==0);
                Battle.SetPaused(false);
                for(int i=0;i<2400&&Battle.Phase==BattlePhase.Running&&Battle.DamageRanking.Count(r=>r.Damage>0)<3;i++)
                {Tick(.05f);if(i%8==0)yield return null;}
                check("multipleNaturalDamageSources",Battle.DamageRanking.Count(r=>r.Damage>0)>=2);
                check("descendingDamage",Battle.DamageRanking.Select(r=>r.Damage).SequenceEqual(Battle.DamageRanking.Select(r=>r.Damage).OrderByDescending(n=>n)));
                check("rowsSumToTotal",Math.Abs(Battle.DamageRanking.Sum(r=>r.Damage)-Battle.TotalPlayerDamage)<.1f);
                check("displayRowsPartitionDamage",Math.Abs(Battle.DisplayDamageRanking.Sum(r=>r.Damage)-Battle.TotalPlayerDamage)<.1f&&
                    Battle.DisplayDamageRanking.Select(r=>r.Damage).SequenceEqual(Battle.DisplayDamageRanking.Select(r=>r.Damage).OrderByDescending(n=>n))&&
                    Battle.DisplayDamageRanking.Count(r=>r.CardId==ManualBattle.GuerrillaDamageCardId)<=1);
                yield return ApprovedUiFrame(folder,"02-natural-combat-ranking");
                Battle.SetPaused(true);double clock=Battle.Clock;float total=Battle.TotalPlayerDamage;Tick(5);
                check("pauseKeepsDamageAndClock",Battle.Clock==clock&&Battle.TotalPlayerDamage==total);
                check("foldedShopStillBlocksRanking",!ShopVisible&&!ShopMapPointAvailable(DamageRankingPanelRect.center));
                yield return ApprovedUiFrame(folder,"03-paused-ranking");
                Battle.SetPaused(false);
                for(int i=0;i<14400&&Battle.Phase==BattlePhase.Running;i++)
                {
                    if(i%20==0)
                    {
                        // Spend remaining legal uses so the fixture can reach the ordinary
                        // finite-resource ending, instead of imposing a countdown defeat.
                        foreach(string id in shopLineup.ToArray())
                        {
                            if(Battle.CheckCardDeployment(id,Battle.Entry)!=DeployFailure.None)continue;
                            int index=shop.Bench.ToList().FindIndex(p=>p.Id==id);
                            Vector2 from=ShopPieceRect(index).center,to=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(Battle.Entry));
                            ProcessShopPointer(EventType.MouseDown,from);ProcessShopPointer(EventType.MouseDrag,to);ProcessShopPointer(EventType.MouseUp,to);
                        }
                        foreach(var unit in Battle.Units.Where(u=>u.Active&&!u.SynergySupport).ToArray())
                        {SelectSkillUnit(unit.EntityId);ActivateSelectedSkill();}
                    }
                    Tick(.05f);if(i%12==0)yield return null;
                }
                check("terminalResultRetainsDamage",Battle.Phase!=BattlePhase.Running&&Battle.TotalPlayerDamage>=total);
                yield return ApprovedUiFrame(folder,"04-final-ranking");
                File.WriteAllText(Path.Combine(folder,"result.json"),JsonUtility.ToJson(new DamageRankingResult
                {passed=passed,checks=checks.ToArray(),totalDamage=Battle.TotalPlayerDamage,ranking=Battle.DamageRanking.Select(r=>r.CardId+":"+r.Damage.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture)).ToArray(),
                    displayRanking=Battle.DisplayDamageRanking.Select(r=>r.CardId+":"+r.Damage.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture)).ToArray(),phase=Battle.Phase.ToString()},true));
                Debug.Log("DAMAGE_RANKING_CHECK passed="+passed);
            }
            finally {Time.timeScale=oldScale;diagnostic=false;}
            yield return null;Application.Quit(passed?0:1);
        }
    }
    public sealed class DamageRankingSmokeDriver : MonoBehaviour
    {
        static bool started;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Begin()
        {
            if(started||!Debug.isDebugBuild||Array.IndexOf(Environment.GetCommandLineArgs(),"-damageRankingSmokeTest")<0)return;
            started=true;var go=new GameObject("Damage ranking verification");DontDestroyOnLoad(go);go.AddComponent<DamageRankingSmokeDriver>();
        }
        IEnumerator Start()
        {
            ManualBattleController controller=null;
            for(int i=0;i<1800;i++){controller=FindAnyObjectByType<ManualBattleController>();if(controller!=null&&!controller.ArtLoading)break;yield return null;}
            if(controller==null){Debug.LogError("DAMAGE_RANKING_CHECK controller missing");Application.Quit(1);yield break;}
            var routines=new Stack<IEnumerator>();routines.Push(controller.VerifyDamageRanking());
            while(routines.Count>0)
            {
                var routine=routines.Peek();bool moved=false;object current=null;Exception failure=null;
                try {moved=routine.MoveNext();if(moved)current=routine.Current;}
                catch(Exception exception){failure=exception;}
                if(failure!=null)
                {
                    Debug.LogException(failure);
                    foreach(var pending in routines)(pending as IDisposable)?.Dispose();
                    Application.Quit(1);yield break;
                }
                if(!moved){(routines.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator nested){routines.Push(nested);continue;}
                yield return current;
            }
        }
    }
}
