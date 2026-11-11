using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YesterdayConvoy.Editor
{
    public static class BossRosterIntegrationTools
    {
        public static void VerifyRepresentative()
        {
            string dir=Path.GetFullPath("Library/BossRosterEvidence");Directory.CreateDirectory(dir);
            var art=BossRosterArt.For("CARD_B22");
            if(art==null||!art.HasAttack||art["attack"].OnAttackTimes.Count!=2)throw new Exception("B22 source attack/event data missing");
            EditorSceneManager.OpenScene("Assets/Scenes/MonsterTrial.unity");
            var camera=Camera.main??UnityEngine.Object.FindFirstObjectByType<Camera>();
            if(camera==null)throw new Exception("Actual scene camera missing");
            var c=new EngagementSettings(unitAttack:40,unitArmor:0,unitRange:4,unitInterval:3,defenderHealth:10000,defenderAttack:1,
                defenderRange:4,hasSkill:false,initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,
                attackDuration:art.AttackDuration,attackWindup:art.AttackContact);
            var s=new ManualBattleSettings("CARD_B22","B22_enemy_1571_mirbst",30,1,2,2,1,1000,.65f,1,1,1,1);
            var battle=new ManualBattle(s,new[]{Vector3.zero,Vector3.right*100},Vector3.right,c);
            battle.Begin();var result=battle.TryDeploy(battle.RunId,1,battle.Entry);
            if(!result.Success)throw new Exception("Representative deployment failed: "+result.Failure);
            var unit=battle.Units[0];
            var card=ScriptableObject.CreateInstance<CardDefinition>();var so=new SerializedObject(card);so.FindProperty("cardId").stringValue="CARD_B22";so.ApplyModifiedPropertiesWithoutUndo();
            var anchor=new GameObject("B22 probe logical position");
            var go=new GameObject("B22 approved source / integration probe",typeof(SpriteRenderer),typeof(DemoSpriteVisual));go.transform.SetParent(anchor.transform,false);
            var view=go.GetComponent<DemoSpriteVisual>();view.Bind(battle,unit,card);
            var renderer=go.GetComponent<SpriteRenderer>();
            var fxObject=new GameObject("B22 original HDR FX probe",typeof(BossRosterEffectView));
            var fx=fxObject.GetComponent<BossRosterEffectView>();fx.Bind(()=>battle,()=>battle.Clock);fx.UseCombatShotFallback=false;
            var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;
            Color32[] Capture(string file,bool visible)
            {
                renderer.enabled=visible;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
                var image=new Texture2D(1280,720,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
                var pixels=image.GetPixels32();File.WriteAllBytes(Path.Combine(dir,file),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=previous;return pixels;
            }
            battle.Advance(1.35f);anchor.transform.position=unit.Motion.Position;view.SendMessage("LateUpdate");
            foreach(var e in battle.BossRosterEvents)fx.Receive(e.Id,e.ActivationId,e.RunId,e.EntityId,e.CardId,e.Kind,e.SkillKey,e.NativeSkillKey,e.Animation,e.Phase,e.HitIndex,e.At,e.EndAt,e.Position,e.TargetPosition,e.TargetId,e.Applied,e.Cancelled,e.Region);
            fx.SendMessage("LateUpdate");
            var before=Capture("representative-off.png",false);var after=Capture("representative-attack.png",true);
            int changed=before.Where((p,i)=>Math.Abs(p.r-after[i].r)+Math.Abs(p.g-after[i].g)+Math.Abs(p.b-after[i].b)>20).Count();
            if(changed<50||renderer.sprite==null||renderer.sprite.texture.width<=0)throw new Exception("B22 actual camera source not visible: "+changed);
            if(battle.Defenders[0].Health>=10000)throw new Exception("Representative real target received no damage");
            if(view.FrameState!="BossRoster/Attack"&&view.FrameState!="BossRoster/attack"||view.FrameIndex<=0)throw new Exception("Representative attack is not animating");
            int visibleFx=fx.VisibleEffects;if(visibleFx<=0)throw new Exception("B22 original HDR effect is not visible");
            var combined=Capture("representative-with-native-fx.png",true);fxObject.SetActive(false);
            var bodyOnly=Capture("representative-body-only.png",true);
            int fxPixels=combined.Where((p,i)=>Math.Abs(p.r-bodyOnly[i].r)+Math.Abs(p.g-bodyOnly[i].g)+Math.Abs(p.b-bodyOnly[i].b)>10).Count();
            if(fxPixels<10)throw new Exception("B22 native FX actual camera difference too small: "+fxPixels);
            int frame=view.FrameIndex;double clock=battle.Clock;battle.SetPaused(true);battle.Advance(10);view.SendMessage("LateUpdate");
            if(battle.Clock!=clock||view.FrameIndex!=frame)throw new Exception("B22 pause did not freeze source motion");
            File.WriteAllText(Path.Combine(dir,"representative.json"),"{\"task\":\"B22\",\"sourceAttackEvents\":2,\"actualCameraChangedPixels\":"+changed+",\"nativeFxChangedPixels\":"+fxPixels+",\"damageObserved\":true,\"pauseVerified\":true,\"formalPlayerVerified\":false}");
            camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(anchor);UnityEngine.Object.DestroyImmediate(card);
            UnityEngine.Object.DestroyImmediate(fxObject);Debug.Log("BOSS_ROSTER_REPRESENTATIVE_PASS pixels="+changed+" fx="+fxPixels);
        }
    }
}
