using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        public bool MandragoraVerificationPassed{get;private set;}
        public bool MandragoraVerificationComplete{get;private set;}
        // Explicit development fixture for late boss phases; never used by normal gameplay.
        ManualBattle MandragoraFixture(float guardHealth=100000,bool left=false,int blocks=0)
        {
            var data=Resources.Load<ManualBattleDefinition>("MonsterTests/M32");var c=data.Engagement.Snapshot();
            var settings=new ManualBattleSettings("CARD_M32",data.Card.Unit.UnitId,100,10,3,1,.1f,1000,.001f,1,1,1,1);
            var combat=new EngagementSettings(unitAttack:c.UnitAttack,unitArmor:c.UnitArmor,unitRange:1.5f,unitInterval:1.8f,defenderHealth:guardHealth,defenderAttack:1,defenderRange:.01f,blockCount:blocks,blockRadius:1.25f,
                initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,attackDuration:1.5f,attackWindup:.8333334f,effects:c.Effects);
            var entry=new Vector3(-.7f,.35f,0);var target=entry+Vector3.right*(left?-1.1f:1.1f);
            var guard=new DefenderSettings("mandra-target","测试守军","D01",target,guardHealth,1,0,.01f,1,blocks,blockRadius:1.25f);
            var b=new ManualBattle(settings,new[]{entry,entry+Vector3.right*100},target,combat,null,new[]{guard});
            b.ConfigureFacilities(new[]{new FacilitySettings(FacilityKind.Pillar,entry+Vector3.back,power:10),new FacilitySettings(FacilityKind.Pillar,new Vector3(1.4f,.35f,0),power:10)});
            ClearViews();Battle=b;terminalPresentationElapsed=0;requestSequence=0;EnableCombatView();b.Begin();b.TryDeploy(b.RunId,1,b.Entry);SyncViews();return b;
        }
        static int MandragoraPixelDifference(MandragoraEffectView view,string effect=null)
        {
            var camera=Camera.main;var oldTarget=camera.targetTexture;var oldRect=camera.rect;var oldActive=RenderTexture.active;
            var rt=new RenderTexture(960,540,24);rt.Create();var image=new Texture2D(960,540,TextureFormat.RGB24,false);
            var renderers=view.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled&&(effect==null||r.name=="Mandragora / "+effect)).ToArray();
            try
            {
                camera.targetTexture=rt;camera.rect=new Rect(0,0,1,1);camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,960,540),0,0);image.Apply();var on=image.GetPixels32();
                foreach(var r in renderers)r.enabled=false;camera.Render();image.ReadPixels(new Rect(0,0,960,540),0,0);image.Apply();var off=image.GetPixels32();int changed=0;
                for(int i=0;i<on.Length;i++)if(Math.Abs(on[i].r-off[i].r)+Math.Abs(on[i].g-off[i].g)+Math.Abs(on[i].b-off[i].b)>12)changed++;return changed;
            }
            finally{foreach(var r in renderers)r.enabled=true;camera.targetTexture=oldTarget;camera.rect=oldRect;RenderTexture.active=oldActive;rt.Release();Destroy(rt);Destroy(image);}
        }
        IEnumerator MandragoraFrame(string directory,string name)
        {
            yield return null;
            if(directory==null)yield break;
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,name+".png"));
            if(Application.isEditor)
            {
                var cam=Camera.main;var prior=cam.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1280,720,24);rt.Create();var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
                try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(Path.Combine(directory,name+"-camera.png"),image.EncodeToPNG());}
                finally{cam.targetTexture=prior;RenderTexture.active=active;rt.Release();Destroy(rt);Destroy(image);}
            }
            yield return null;yield return null;
        }
        public IEnumerator VerifyMandragora(bool quit=true)
        {
            if(!Debug.isDebugBuild){MandragoraVerificationComplete=true;yield break;}
            diagnostic=true;Application.runInBackground=true;Time.timeScale=0;MandragoraVerificationComplete=MandragoraVerificationPassed=false;
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");string dir=at>=0&&at+1<args.Length?args[at+1]:null;
            if(dir!=null)Directory.CreateDirectory(dir);
            bool ok=SynergySandbox&&shop.Roles.Any(r=>r.Id=="CARD_M32"&&r.Cost==5);
            Action<string,bool> check=(name,value)=>{ok&=value;Debug.Log("MANDRA_EVENT "+name+"="+value);};
            SetSandboxRoster(5);while(ArtLoading)yield return null;
            string[] others={"CARD_M29","CARD_M30","CARD_M31","CARD_M07","CARD_M08","CARD_M10","CARD_M11","CARD_M05"};
            shop.Preset(new[]{new ShopTrialModel.Piece{Id="CARD_M32",Star=1},new ShopTrialModel.Piece{Id="CARD_M32",Star=1}}.Concat(others.Select(id=>new ShopTrialModel.Piece{Id=id,Star=1})),new[]{"CARD_M32"},8,0,40);
            shopLineup.Clear();shopLineup.AddRange(new[]{"CARD_M29","CARD_M30","CARD_M31","CARD_M32"});SyncShopDeck(false);SetShopVisible(true);yield return null;
            yield return MandragoraFrame(dir,"01-mandragora-five-cost-shop");
            var purchase=PurchaseShopCard(0);check("fullBenchMerge",purchase.Success&&purchase.FinalStar==2&&shop.Bench.Count==9);yield return new WaitForSecondsRealtime(.85f);
            check("benchModel",ShopBenchModel(purchase.Instance)!=null&&ShopBenchModel(purchase.Instance).sprite.name.StartsWith("M32/"));yield return MandragoraFrame(dir,"02-mandragora-merged-bench");
            check("nextBattle",StartLoopNextBattle());Battle.Begin();Battle.SetPaused(true);yield return null;yield return new WaitForSecondsRealtime(.3f);
            int slot=shop.Bench.ToList().FindIndex(p=>p.Id=="CARD_M32");var gate=TacticalViewport.Current.FromScreen(Camera.main.WorldToScreenPoint(Battle.Entry));
            ProcessShopPointer(EventType.MouseDown,ShopPieceRect(slot).center);ProcessShopPointer(EventType.MouseDrag,gate);ProcessShopPointer(EventType.MouseUp,gate);
            var purchased=Battle.Units.FirstOrDefault(u=>u.CardId=="CARD_M32");check("pausedTwoStarDrag",purchased!=null&&purchased.MaxHealth==1900&&Battle.Clock==0);
            yield return MandragoraFrame(dir,"03-mandragora-paused-deployment");
            var b=MandragoraFixture();var unit=b.Units[0];Tick(6.1f);yield return null;yield return null;
            var view=FindObjectsByType<MandragoraEffectView>(FindObjectsSortMode.None).First(v=>v.VisibleEffects>0);
            int gaze=MandragoraPixelDifference(view,"c1_skill_01_beam");check("gaze",gaze>30&&unit.Gaze!=null&&unit.Gaze.Ticks==1);yield return MandragoraFrame(dir,"04-native-ground-gaze");
            b.SetPaused(true);double clock=b.Clock;var frames=view.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled).Select(r=>r.sprite).ToArray();Tick(3);yield return null;
            check("pauseFrames",b.Clock==clock&&frames.SequenceEqual(view.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled).Select(r=>r.sprite)));b.SetPaused(false);
            check("pillarBreak",b.TryTopplePillar(0,b.Entry+Vector3.back*2)&&!unit.StoneShield&&unit.Gaze.Interrupted);Tick(.15f);yield return null;int broken=MandragoraPixelDifference(view);check("breakPixels",broken>30);
            b.TryDamageUnit(unit.EntityId,650,CombatDamageType.Arts);Tick(.1f);yield return null;check("groundRestore",unit.StoneShield);yield return MandragoraFrame(dir,"05-original-shield-restore");
            b.TryDamageUnit(unit.EntityId,100000,CombatDamageType.Arts);Tick(2);yield return null;int reborn=MandragoraPixelDifference(view);ok&=unit.MandraPhase==MandragoraPhase.Reborn&&reborn>30;yield return MandragoraFrame(dir,"06-original-rebirth");
            Tick(3.1f);yield return null;ok&=b.UnitAirborne(unit);yield return MandragoraFrame(dir,"07-second-phase-shield");
            for(int i=0;i<220&&(unit.Gaze==null||unit.Gaze.Ended||b.Clock-unit.Gaze.StartedAt<1.1);i++){Tick(.1f);if(i%15==0)yield return null;}
            yield return null;int gaze2=MandragoraPixelDifference(view,"c1_skill_01_beam");ok&=unit.Gaze!=null&&!unit.Gaze.Ended&&gaze2>30;yield return MandragoraFrame(dir,"08-second-phase-gaze");
            for(int i=0;i<250&&!b.PillarCommands.Any();i++){Tick(.1f);if(i%15==0)yield return null;}
            Tick(.8f);yield return null;var actor=FindObjectsByType<DemoSpriteVisual>(FindObjectsSortMode.None).First(v=>v.EntityId==unit.EntityId);
            bool cast=actor.Renderer.sprite.name.Contains("extra_c2_skill_3");ok&=cast;yield return MandragoraFrame(dir,"09-original-pillar-warning");
            for(int i=0;i<60&&!b.PillarCommands.Any(p=>p.Applied);i++)Tick(.1f);Tick(.15f);yield return null;
            int pillar=MandragoraPixelDifference(view,"stonepillar_collapse_01_left");ok&=b.PillarCommands.Any(p=>p.Applied)&&pillar>30;yield return MandragoraFrame(dir,"10-original-collapse");
            Tick(25.1f);yield return null;ok&=unit.StoneShield;bool restored=actor.Renderer.sprite.name.Contains("extra_c2_skill_2");ok&=restored;yield return MandragoraFrame(dir,"11-second-phase-restore");
            b.TryDamageUnit(unit.EntityId,100000,CombatDamageType.Arts);Tick(.2f);yield return null;int death=MandragoraPixelDifference(view);ok&=unit.Resolution==UnitResolution.Dead&&death>30;yield return MandragoraFrame(dir,"12-original-final-death");
            b=MandragoraFixture(50);Tick(6.1f);yield return null;check("sealedTile",b.SealedTileCount==1&&!view.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.enabled&&r.name=="Mandragora / c1_skill_01_beam"));yield return MandragoraFrame(dir,"13-original-sealed-stones");
            b=MandragoraFixture(left:true);Tick(6.1f);yield return null;var leftActor=FindObjectsByType<DemoSpriteVisual>(FindObjectsSortMode.None).First(v=>v.EntityId==b.Units[0].EntityId&&v.gameObject.activeInHierarchy);check("leftFacing",leftActor.Renderer.flipX);yield return MandragoraFrame(dir,"14-left-facing-gaze");
            b=MandragoraFixture();Tick(.4f);b.TryDeploy(b.RunId,2,b.Entry);Tick(5.9f);yield return null;int beams=view.GetComponentsInChildren<SpriteRenderer>().Count(r=>r.enabled&&r.name=="Mandragora / c1_skill_01_beam");ok&=beams==2;
            b.Restart();Tick(0);yield return null;check("restart",view.VisibleEffects==0&&b.Gazes.Count==0&&b.PillarCommands.Count==0);
            b=MandragoraFixture(blocks:1);Tick(1.2f);yield return null;
            check("groundAttackSingleHit",b.Units[0].OrdinaryAttacksFired==1&&b.CombatShots.Count(s=>!s.FromDefender&&s.Applied)==1&&MandragoraPixelDifference(view)>30);yield return MandragoraFrame(dir,"15-ground-attack-contact");
            b=MandragoraFixture();b.TryDamageUnit(b.Units[0].EntityId,100000,CombatDamageType.Arts);Tick(6.2f);yield return null;
            check("secondAttackSingleHit",b.Units[0].MandraPhase==MandragoraPhase.Second&&b.CombatShots.Count(s=>!s.FromDefender&&s.Applied)==1&&MandragoraPixelDifference(view)>30);yield return MandragoraFrame(dir,"16-second-attack-contact");
            MandragoraVerificationComplete=true;MandragoraVerificationPassed=ok;
            Debug.Log($"MANDRAGORA_VFX_CHECK: passed={ok}; shopFullBenchMerge={purchase.Success&&purchase.FinalStar==2}; pausedDrag={purchased!=null}; gazePixels={gaze}; brokenPixels={broken}; rebirthPixels={reborn}; gaze2Pixels={gaze2}; originalPillarAction={cast}; collapsePixels={pillar}; originalRestoreAction={restored}; deathPixels={death}; independentBeams={beams}; developmentLatePhases=True");
            if(quit)Application.Quit(ok?0:1);
        }
    }
}
