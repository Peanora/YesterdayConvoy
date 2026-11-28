using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy
{
    /// <summary>Explicit opt-in rendered UI checks. Never supplies items to a normal game.</summary>
    public sealed class FormationConsumableEvidence : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public string scope="Actual Windows Player and authored 2-7 scene, EXPLICIT preset of seven one-star units, level9/100gold and consumables/gear. Real equipment-rail pointer drags, one-card duplication, fusion, all-item removal and empty-target retention. First10 simulated combat seconds check battle use is blocked. Not a natural campaign or win-rate test. Guaranteed reward drops checked separately.";
            public bool passed,copyOne,copyMerge,removedAll,emptyRetained,combatBlocked,originalPactIcons;
            public int checks;public List<string> exceptions=new List<string>();
        }
        readonly Report report=new Report();FormationBattleController controller;string directory;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Install()
        {
            if(!Environment.GetCommandLineArgs().Contains("-formationConsumableSmoke"))return;
            var go=new GameObject("Consumable UI verification");DontDestroyOnLoad(go);go.AddComponent<FormationConsumableEvidence>();
        }
        void OnEnable()=>Application.logMessageReceived+=Log;
        void OnDisable()=>Application.logMessageReceived-=Log;
        void Log(string message,string trace,LogType type){if(type==LogType.Exception)report.exceptions.Add(message);}
        void Check(bool value,string text){report.checks++;if(!value)throw new InvalidOperationException(text);}
        void Save()=>File.WriteAllText(Path.Combine(directory,"consumable-smoke.json"),JsonUtility.ToJson(report,true));
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-formationEvidenceDir");
            directory=Path.GetFullPath(at>=0?args[at+1]:"artifacts/consumables-v09/player");Directory.CreateDirectory(directory);
            var stack=new Stack<IEnumerator>();stack.Push(Work());
            while(stack.Count>0)
            {
                bool next;object value=null;
                try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}
                catch(Exception error){report.exceptions.Add(error.ToString());Save();Application.Quit(1);yield break;}
                if(!next){stack.Pop();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;
            }
        }
        IEnumerator Shot(string name,float delay=.2f)
        {yield return new WaitForSecondsRealtime(delay);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(directory,name+".png"));yield return null;}
        Vector2 Target(int instance)
        {
            var rect=controller.FormationRecruitmentTargetRect(instance);
            Check(rect.width>0&&rect.height>0,"Actual board or reserve target exists");return rect.center;
        }
        void Drag(int item,int target)
        {
            Check(controller.TryEquipmentTilePoint(item,out var from),"Tool visible in equipment rail");Vector2 to=Target(target);
            Check(controller.ProcessFormationEquipmentPointer(EventType.MouseDown,from),"Real tool pointer down");
            Check(controller.ProcessFormationEquipmentPointer(EventType.MouseDrag,to),"Real tool pointer drag");
            Check(controller.ProcessFormationEquipmentPointer(EventType.MouseUp,to),"Real tool pointer release");
        }
        IEnumerator Work()
        {
            Application.runInBackground=true;FormationBalanceV09.Select(FormationBalanceV09.Variant.B);
            if(SceneManager.GetActiveScene().name!=FormationBattleController.SceneName)yield return SceneManager.LoadSceneAsync(FormationBattleController.SceneName);
            double deadline=Time.realtimeSinceStartupAsDouble+180;
            while(((controller=FindAnyObjectByType<FormationBattleController>())==null||!controller.Ready)&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            Check(controller!=null&&controller.Ready,"Actual game scene ready");yield return controller.OpenEconomyFixture();
            controller.SetMarketVisible(false);yield return new WaitForSecondsRealtime(1.2f);
            var run=controller.Campaign;var shop=controller.Shop;var target=shop.Bench.Single(p=>FormationCampaignRules.Find(p.Id).Id=="M17");
            var gear=new[]{run.AddItem("AA"),run.AddItem("EE"),run.AddItem("徽章:海嗣")};gear[1].Stacks=7;
            foreach(var item in gear)Check(run.Equip(item.Instance,target.Instance).Success,"Initial explicit gear fixture");
            var first=run.AddItem(FormationCampaignRun.CopierKey);var second=run.AddItem(FormationCampaignRun.CopierKey);
            var remover=run.AddItem(FormationCampaignRun.RemoverKey);var spare=run.AddItem(FormationCampaignRun.RemoverKey);
            controller.RefreshEconomicEvidencePresentation();
            report.originalPactIcons=SynergyBadgePresentation.IconFor("远见")?.name=="foresight"&&SynergyBadgePresentation.IconFor("奇迹")?.name=="miracle";
            Check(report.originalPactIcons,"Original Garrison Pact textures packaged and loaded");
            Check(FormationBattleController.SupplyItemArt(FormationCampaignRun.CopierKey)!=null&&FormationBattleController.SupplyItemArt(FormationCampaignRun.RemoverKey)!=null,"Distinct consumable icons");
            yield return Shot("01-original-pacts-and-consumables");int gold=shop.Gold,stock=shop.Available(target.Id);
            Drag(first.Instance,target.Instance);report.copyOne=shop.Bench.Where(p=>p.Id==target.Id).Sum(p=>ShopTrialModel.Copies(p.Star))==2;
            Check(report.copyOne&&shop.Available(target.Id)==stock-1&&shop.Gold==gold,"Copier creates one finite-stock card without payment");
            yield return Shot("02-copy-arrival");yield return new WaitForSecondsRealtime(1.2f);
            Drag(second.Instance,target.Instance);yield return new WaitForSecondsRealtime(1.3f);
            target=shop.Bench.Single(p=>p.Id==target.Id);report.copyMerge=target.Star==2&&controller.Model.Units.Any(u=>u.Instance==target.Instance&&u.Star==2);
            Check(report.copyMerge,"Real copied third card fuses and updates visible model");
            Check(run.Equipment(target.Instance).Count==3&&run.AllEquipment.Count==5,"Fusion retains three original gear instances; no duplicated gear");
            yield return Shot("03-copy-merge-two-star");
            Drag(remover.Instance,target.Instance);report.removedAll=run.Equipment(target.Instance).Count==0&&gear.All(i=>run.Inventory.Contains(i));
            Check(report.removedAll&&gear[1].Stacks==7&&!run.HasTrait(target,"海嗣"),"All gear returns intact; badge effect removed");
            Check(!run.AllEquipment.Contains(remover)&&run.AllEquipment.Contains(spare),"Exactly one remover consumed");
            yield return Shot("04-removal-return-flight",.18f);yield return new WaitForSecondsRealtime(.9f);
            Drag(spare.Instance,target.Instance);report.emptyRetained=run.Inventory.Contains(spare);Check(report.emptyRetained,"Empty target retains remover");
            yield return Shot("05-empty-target-retains-tool");
            foreach(var item in gear)Check(controller.UseFormationItem(item.Instance,target.Instance,Target(target.Instance)).Success,"Reequip actual returned item");
            controller.StartBattle();while(controller.FormationBattleOpeningActive)yield return null;
            Check(controller.Model.Phase==FormationPhase.Fighting,"Actual battle starts");
            report.combatBlocked=!controller.ProcessFormationEquipmentPointer(EventType.MouseDown,Vector2.zero)&&!controller.Session.UseItem(spare.Instance,target.Instance).Success;
            Check(report.combatBlocked&&run.Inventory.Contains(spare),"Battle cannot consume tools");
            while(controller.Model.Clock<10&&controller.Model.Phase==FormationPhase.Fighting)
            {for(int i=0;i<10&&controller.Model.Phase==FormationPhase.Fighting;i++){controller.RefreshBalanceTiming();controller.Model.Advance(.025f);}yield return null;}
            yield return Shot("06-actual-battle-use-blocked");Check(report.exceptions.Count==0,"No runtime script exceptions");
            report.passed=true;Save();Debug.Log("CONSUMABLE_SMOKE_PASS: "+report.checks);Application.Quit(0);
        }
    }
}
