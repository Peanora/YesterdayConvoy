using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        public bool MephistoVerificationPassed{get;private set;}
        public bool MephistoVerificationComplete{get;private set;}
        // Explicit diagnostic conditions: near-stationary multi-lane units and wounded recipients.
        // These conditions are never used by normal maps or the shop.
        ManualBattle MephistoFixture(bool left=false,bool two=false)
        {
            var profiles=new[]{"M25","M21","M22","M23"}.Select(id=>CampaignBalance.Build(Resources.Load<ManualBattleDefinition>("MonsterTests/"+id),1,100,10)).Select(p=>new BasicRosterProfile(new ManualBattleSettings(p.Unit.CardId,p.Unit.UnitId,100,10,3,1,.1f,p.Unit.Health,.001f,1,1,1,1),p.Combat,true)).ToArray();
            var starts=new[]{new Vector3(-1.5f,.35f,0),new Vector3(1,.35f,1.3f),new Vector3(1.5f,.35f,-.6f),new Vector3(-.2f,.35f,-2.2f)};
            if(left)for(int i=0;i<starts.Length;i++)starts[i].x=-starts[i].x;
            var direction=Vector3.right*(left?-1:1);var lanes=starts.Select((p,i)=>new DeploymentLane("VFX"+i,"END"+i,new[]{p,p+direction*100})).ToArray();
            var far=new Vector3(100,.35f,100);var guard=new DefenderSettings("mephi-fixture-guard","测试守军","D01",far,100000,1,0,.01f,1,0);
            var b=new ManualBattle(profiles[0].Unit,lanes[0].Points,far,profiles[0].Combat,roster:new[]{guard},deploymentLanes:lanes);b.ConfigureCardLoadout(profiles.Skip(1));
            ClearViews();Battle=b;terminalPresentationElapsed=0;requestSequence=0;EnableCombatView();b.Begin();b.TryDeploy(b.RunId,1,starts[0]);
            for(int i=1;i<4;i++)b.TryDeployRole(b.RunId,i+1,starts[i],profiles[i].Unit.CardId);
            if(two){b.Advance(.11f);b.TryDeploy(b.RunId,5,starts[0]);}
            foreach(var u in b.Units.Where(u=>u.CardId!="CARD_M25"))b.TryDamageUnit(u.EntityId,u.MaxHealth*.7f,CombatDamageType.Arts);
            SyncViews();return b;
        }
        static int MephistoPixels(MephistoEffectView view,string effect)
        {
            var camera=Camera.main;var prior=camera.targetTexture;var rect=camera.rect;var active=RenderTexture.active;
            var rt=new RenderTexture(960,540,24);rt.Create();var image=new Texture2D(960,540,TextureFormat.RGB24,false);
            var renderers=view.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled&&r.name=="Mephisto / "+effect).ToArray();
            try{
                camera.targetTexture=rt;camera.rect=new Rect(0,0,1,1);camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,960,540),0,0);image.Apply();var on=image.GetPixels32();
                foreach(var r in renderers)r.enabled=false;camera.Render();image.ReadPixels(new Rect(0,0,960,540),0,0);image.Apply();var off=image.GetPixels32();int changed=0;
                for(int i=0;i<on.Length;i++)if(Math.Abs(on[i].r-off[i].r)+Math.Abs(on[i].g-off[i].g)+Math.Abs(on[i].b-off[i].b)>12)changed++;return changed;
            }
            finally{foreach(var r in renderers)r.enabled=true;camera.targetTexture=prior;camera.rect=rect;RenderTexture.active=active;rt.Release();Destroy(rt);Destroy(image);}
        }
        public IEnumerator VerifyMephisto(bool quit=true)
        {
            if(!Debug.isDebugBuild){MephistoVerificationComplete=true;yield break;}
            diagnostic=true;Application.runInBackground=true;Time.timeScale=0;MephistoVerificationComplete=MephistoVerificationPassed=false;
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");string dir=at>=0&&at+1<args.Length?args[at+1]:null;if(dir!=null)Directory.CreateDirectory(dir);
            bool ok=true;Action<string,bool> check=(name,value)=>{ok&=value;Debug.Log("MEPHI_EVENT "+name+"="+value);};
            check("fiveCostStock",SynergySandbox&&shop.Roles.Any(r=>r.Id=="CARD_M25"&&r.Cost==5)&&shop.Available("CARD_M25")==9);
            SetSandboxRoster(4);while(ArtLoading)yield return null;
            string[] fillers={"CARD_M21","CARD_M22","CARD_M23","CARD_M24","CARD_M08","CARD_M10","CARD_M05","CARD_M07"};
            shop.Preset(new[]{new ShopTrialModel.Piece{Id="CARD_M25",Star=1},new ShopTrialModel.Piece{Id="CARD_M25",Star=1}}.Concat(fillers.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1})),new[]{"CARD_M25"},8,0,40);
            shopLineup.Clear();shopLineup.AddRange(new[]{"CARD_M21","CARD_M22","CARD_M23","CARD_M25"});SyncShopDeck(false);SetShopVisible(true);yield return null;
            yield return MandragoraFrame(dir,"01-mephisto-five-cost-shop");
            var purchase=PurchaseShopCard(0);check("fullBenchMerge",purchase.Success&&purchase.FinalStar==2&&shop.Bench.Count==9);yield return new WaitForSecondsRealtime(.85f);
            check("benchOriginalModel",ShopBenchModel(purchase.Instance)!=null&&ShopBenchModel(purchase.Instance).sprite.name.StartsWith("M25/"));yield return MandragoraFrame(dir,"02-mephisto-two-star-bench");
            check("nextBattle",StartLoopNextBattle());Battle.Begin();Battle.SetPaused(true);yield return null;yield return new WaitForSecondsRealtime(.3f);
            int slot=shop.Bench.ToList().FindIndex(p=>p.Id=="CARD_M25");var gate=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(Battle.Entry));
            ProcessShopPointer(EventType.MouseDown,ShopPieceRect(slot).center);ProcessShopPointer(EventType.MouseDrag,gate);ProcessShopPointer(EventType.MouseUp,gate);
            var bought=Battle.Units.FirstOrDefault(u=>u.CardId=="CARD_M25");check("pausedTwoStarDrag",bought!=null&&bought.MaxHealth==1140*SandboxMonsterHealth&&Battle.Clock==0&&Battle.UnitCombat(bought).Effects.Healer&&!Battle.UnitCombat(bought).CanAttack);
            yield return MandragoraFrame(dir,"03-mephisto-real-deployment");
            var b=MephistoFixture();Tick(.05f);yield return null;Tick(.65f);yield return null;
            var view=FindObjectsByType<MephistoEffectView>(FindObjectsSortMode.None).First(v=>v.gameObject.activeInHierarchy);
            check("noEarlyImpact",!view.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.enabled&&r.name=="Mephisto / attack_01_hit"));
            Tick(.7f);yield return null;yield return null;var cast=b.HealingCasts[0];int impact=MephistoPixels(view,"attack_01_hit"),hand=MephistoPixels(view,"attack_01_start"),aura=MephistoPixels(view,"buff_01_start");
            check("threeRealTargets",cast.Resolved&&cast.AppliedTargetIds.Count==3&&cast.HealthRestored>0);check("nativeImpactPixels",impact>20);check("nativeHandPixels",hand>2);check("nativeAuraPixels",aura>2);
            check("noDamageOrAmmo",b.Units[0].OrdinaryAttacksFired==0&&b.Units[0].SkillAmmo==0&&b.CombatShots.All(s=>s.FromDefender));yield return MandragoraFrame(dir,"04-native-three-target-treatment");
            b.SetPaused(true);var clock=b.Clock;float healed=b.Units[0].HealingDone;var sprites=view.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled).Select(r=>r.sprite).ToArray();Tick(10);yield return null;
            check("pause",clock==b.Clock&&healed==b.Units[0].HealingDone&&sprites.SequenceEqual(view.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled).Select(r=>r.sprite)));b.SetPaused(false);
            Tick(3.4f);yield return null;check("repeatOnce",b.HealingCasts.Count>=2&&Math.Abs(b.Units[0].HealingDone-b.HealingCasts.Sum(c=>c.HealthRestored))<.01);yield return MandragoraFrame(dir,"05-repeated-treatment");
            b.TryDamageUnit(b.Units[0].EntityId,100000,CombatDamageType.Arts);Tick(.1f);yield return null;check("boostRemoved",b.RegenerationMultiplier==1);Tick(2.1f);yield return null;check("auraTailCleared",!view.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.enabled&&r.name.Contains("buff_01")));
            b=MephistoFixture(left:true);Tick(1.45f);yield return null;yield return null;var actor=FindObjectsByType<DemoSpriteVisual>(FindObjectsSortMode.None).First(v=>v.EntityId==b.Units[0].EntityId&&v.gameObject.activeInHierarchy);
            check("leftFacing",actor.Renderer.flipX&&MephistoPixels(view,"attack_01_start")>2);yield return MandragoraFrame(dir,"06-native-left-facing-treatment");
            b=MephistoFixture(two:true);Tick(1.45f);yield return null;yield return null;
            check("independentCasters",b.HealingCasts.Select(c=>c.SourceId).Distinct().Count()==2&&b.RegenerationMultiplier==2&&view.GetComponentsInChildren<SpriteRenderer>().Count(r=>r.enabled&&r.name=="Mephisto / attack_01_start")==2);
            b.Restart();Tick(0);yield return null;check("restartClears",view.VisibleEffects==0&&b.HealingCasts.Count==0);
            b=MephistoFixture();Tick(.1f);b.TryDamageUnit(b.Units[0].EntityId,100000,CombatDamageType.Arts);Tick(1.4f);yield return null;
            check("deadCasterCancels",b.HealingCasts.All(c=>c.HealthRestored==0&&c.AppliedTargetIds.Count==0)&&!view.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.enabled&&r.name=="Mephisto / attack_01_hit"));
            LeaveShopTrial();TryEnterMonsterTest("CARD_M25");while(ArtLoading)yield return null;SelectCard(true);var receipt=TryDeployAt(Battle.Entry);var natural=Battle.FindUnit(receipt.EntityId);
            for(int i=0;i<800&&natural!=null&&natural.HealingDone<=0;i++){Tick(.05f);if(i%10==0)yield return null;}
            Tick(.3f);yield return null;view=FindObjectsByType<MephistoEffectView>(FindObjectsSortMode.None).First(v=>v.gameObject.activeInHierarchy);int normal=MephistoPixels(view,"attack_01_hit");
            check("normalEntryNaturalHealing",receipt.Success&&natural!=null&&natural.HealingDone>0&&natural.OrdinaryAttacksFired==0&&normal>2);yield return MandragoraFrame(dir,"07-normal-entry-natural-healing");
            ReturnToMonsterTests();yield return null;check("returnClears",view.VisibleEffects==0);
            MephistoVerificationComplete=true;MephistoVerificationPassed=ok;Debug.Log($"MEPHISTO_VFX_CHECK: passed={ok}; impactPixels={impact}; handPixels={hand}; auraPixels={aura}; normalPixels={normal}; shopMerge={purchase.Success&&purchase.FinalStar==2}; developmentMultiLaneFixture=True; normalEntryNoInjectedHealth=True");
            if(quit)Application.Quit(ok?0:1);
        }
    }
}
