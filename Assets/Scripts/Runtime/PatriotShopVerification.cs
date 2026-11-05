using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        public bool PatriotShopVerificationPassed{get;private set;}
        public bool PatriotShopVerificationComplete{get;private set;}
        // Opt-in developer fixture: retain purchased two-star combat/health, slow movement only.
        // A different primary profile proves that Patriot reads the deployed instance, not the deck head.
        ManualBattle PatriotShopFixture()
        {
            var basic=CampaignBalance.Build(Resources.Load<ManualBattleDefinition>("MonsterTests/M08"),1,100,10);
            var source=CampaignBalance.Build(Resources.Load<ManualBattleDefinition>("MonsterTests/M20"),2,100,10);
            var p=new BasicRosterProfile(new ManualBattleSettings(source.Unit.CardId,source.Unit.UnitId,100,10,3,1,.1f,source.Unit.Health,.001f,1,1,1,1),source.Combat,true);
            var entry=new Vector3(-1.8f,.35f,0);var near=entry+Vector3.right*.4f;
            var guards=new[]{new DefenderSettings("patriot-shop-ground","测试地面守军","D01",near,100000,1,0,.01f,1,1),new DefenderSettings("patriot-shop-high","测试高台守军","D01",entry+Vector3.right*3,100000,1,0,.01f,1)};
            var b=new ManualBattle(basic.Unit,new[]{entry,entry+Vector3.forward*100},near,basic.Combat,roster:guards);
            b.ConfigureCardLoadout(new[]{p});b.ConfigureDefenderHighGround("patriot-shop-high",true);
            ClearViews();Battle=b;terminalPresentationElapsed=0;requestSequence=0;EnableCombatView();b.Begin();b.TryDeployRole(b.RunId,1,entry,"CARD_M20");SyncViews();return b;
        }
        public IEnumerator VerifyPatriotShop(bool quit=true)
        {
            if(!Debug.isDebugBuild){PatriotShopVerificationComplete=true;yield break;}
            float oldTime=Time.timeScale;
            diagnostic=true;Application.runInBackground=true;Time.timeScale=0;PatriotShopVerificationPassed=PatriotShopVerificationComplete=false;
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");string dir=at>=0&&at+1<args.Length?args[at+1]:Path.GetFullPath("artifacts/patriot-shop-gpu");Directory.CreateDirectory(dir);
            bool ok=true;Action<string,bool> check=(name,value)=>{ok&=value;Debug.Log("PATRIOT_SHOP_EVENT "+name+"="+value);};
            try
            {
                check("fiveCostStock",SynergySandbox&&shop.Roles.Any(r=>r.Id=="CARD_M20"&&r.Cost==5)&&shop.Available("CARD_M20")==9);
                SetSandboxRoster(2);while(ArtLoading)yield return null;
                check("guerrillaPreset",shopLineup.Contains("CARD_M20"));
                string[] fillers={"CARD_M03","CARD_M15","CARD_M16","CARD_M17","CARD_M18","CARD_M19","CARD_M08","CARD_M05"};
                shop.Preset(new[]{new ShopTrialModel.Piece{Id="CARD_M20",Star=1},new ShopTrialModel.Piece{Id="CARD_M20",Star=1}}.Concat(fillers.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1})),new[]{"CARD_M20"},8,0,40);
                shopLineup.Clear();shopLineup.AddRange(new[]{"CARD_M03","CARD_M15","CARD_M17","CARD_M20"});SyncShopDeck(false);SetShopVisible(true);yield return null;
                yield return MandragoraFrame(dir,"01-patriot-five-cost-shop");
                var purchase=PurchaseShopCard(0);check("fullBenchMerge",purchase.Success&&purchase.FinalStar==2&&shop.Bench.Count==9&&purchase.Value==5&&shop.Gold==(shop.UnlimitedGold?40:35));yield return new WaitForSecondsRealtime(.85f);
                check("benchOriginalModel",ShopBenchModel(purchase.Instance)!=null&&ShopBenchModel(purchase.Instance).sprite.name.StartsWith("M20/"));yield return MandragoraFrame(dir,"02-patriot-two-star-bench");
                check("nextBattle",StartLoopNextBattle());Battle.Begin();Battle.SetPaused(true);yield return null;yield return new WaitForSecondsRealtime(.3f);
                int slot=shop.Bench.ToList().FindIndex(p=>p.Id=="CARD_M20");var gate=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(Battle.Entry));
                ProcessShopPointer(EventType.MouseDown,ShopPieceRect(slot).center);ProcessShopPointer(EventType.MouseDrag,gate);ProcessShopPointer(EventType.MouseUp,gate);
                var bought=Battle.Units.FirstOrDefault(u=>u.CardId=="CARD_M20");check("pausedTwoStarDrag",bought!=null&&bought.MaxHealth==1140&&Battle.Clock==0&&Battle.IsPatriot(bought)&&!Battle.UnitCombat(bought).HasSkill);
                yield return MandragoraFrame(dir,"03-patriot-real-deployment");
                var b=PatriotShopFixture();var u=b.Units.Single();Tick(.9f);yield return null;
                double march=PatriotGpuCheck(dir,"04-mixed-patriot-march");check("secondaryFourHits",b.CombatShots.Count(s=>s.SourceId==u.EntityId)==4&&b.CombatShots.Count(s=>s.SourceId==u.EntityId&&s.Applied)>=3);
                var renderer=PatriotRenderers().First();var props=new MaterialPropertyBlock();renderer.GetPropertyBlock(props);float frame=props.GetFloat("_Frame");var pos=renderer.transform.position;
                b.SetPaused(true);double clock=b.Clock;Tick(10);yield return null;renderer.GetPropertyBlock(props);check("pause",clock==b.Clock&&props.GetFloat("_Frame")==frame&&pos==renderer.transform.position);b.SetPaused(false);
                b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts);Tick(.65f);yield return null;double reborn=PatriotGpuCheck(dir,"05-mixed-patriot-rebirth");double began=u.PatriotPhaseStartedAt;
                Tick((float)(began+57.4-b.Clock));yield return null;double rise=PatriotGpuCheck(dir,"06-mixed-patriot-rise");check("fullRebirthDelay",u.PatriotPhase==PatriotPhase.Reborn);
                Tick((float)(began+60.1-b.Clock));yield return null;check("twoStarRebirthHealth",u.PatriotPhase==PatriotPhase.Destruction&&u.Health==1140&&b.UnitInvulnerable(u));
                for(int i=0;i<100&&!b.CombatShots.Any(s=>s.SourceId==u.EntityId&&s.Applied&&s.StartedAt>began+60);i++){Tick(.025f);yield return null;}
                double destruction=PatriotGpuCheck(dir,"07-mixed-patriot-destruction");
                for(int i=0;i<1000&&!b.PatriotCasting(u);i++){Tick(.025f);yield return null;}
                check("highGroundSpear",u.Spear!=null&&u.Spear.TargetId=="patriot-shop-high");PatriotRequire(u.Spear!=null,"Mixed shop spear missing");var spear=u.Spear;
                Tick((float)(spear.StartedAt+.7-b.Clock));yield return null;double charge=PatriotGpuCheck(dir,"08-mixed-patriot-spear-charge");
                Tick((float)(spear.Shot.ImpactAt+.1-b.Clock));yield return null;double impact=PatriotGpuCheck(dir,"09-mixed-patriot-spear-impact");check("realSpearDamage",spear.Shot.Applied&&spear.Shot.DamageDealt>0);check("realImmolation",u.ImmolationTicks>0&&u.ImmolationDamage>0);
                b.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts);Tick(.7f);yield return null;double death=PatriotGpuCheck(dir,"10-mixed-patriot-final-death");check("finalDeath",u.Resolution==UnitResolution.Dead);
                b.Restart();Tick(0);yield return null;check("restartClears",PatriotRenderers().Length==0&&!b.PatriotPresent);LeaveShopTrial();yield return null;check("returnClears",PatriotRenderers().Length==0);
                PatriotShopVerificationPassed=ok;PatriotShopVerificationComplete=true;
                string result=$"passed={ok}; gpu={SystemInfo.graphicsDeviceType}; fullBenchTwoStar={purchase.Success&&purchase.FinalStar==2}; nativeDeltas={march},{reborn},{rise},{destruction},{charge},{impact},{death}; latePhasesDeveloperFixture=True";
                File.WriteAllText(Path.Combine(dir,"patriot-shop-check.txt"),result);Debug.Log("PATRIOT_SHOP_CHECK: "+result);
            }
            finally{Time.timeScale=oldTime;}
            if(quit)Application.Quit(ok?0:1);
        }
    }
}
