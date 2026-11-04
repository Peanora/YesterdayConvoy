using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        static void PatriotRequire(bool value,string message){if(!value)throw new InvalidOperationException("PATRIOT_VFX_CHECK: "+message);}
        static SpriteRenderer[] PatriotRenderers()=>FindObjectsByType<SpriteRenderer>().Where(r=>r.enabled&&r.name.StartsWith("Patriot effect / ")).ToArray();
        static Color32[] PatriotCapture(string path)
        {
            var cam=Camera.main;var rt=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGBA32,false);
            var old=cam.targetTexture;var rect=cam.rect;var active=RenderTexture.active;
            try
            {
                cam.targetTexture=rt;cam.rect=new Rect(0,0,1,1);cam.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
                if(path!=null)File.WriteAllBytes(path,image.EncodeToPNG());return image.GetPixels32();
            }
            finally{cam.targetTexture=old;cam.rect=rect;RenderTexture.active=active;rt.Release();Destroy(rt);Destroy(image);}
        }
        static double PatriotGpuCheck(string folder,string name)
        {
            var renderers=PatriotRenderers();PatriotRequire(renderers.Length>0,name+" has no effect renderer");
            PatriotRequire(renderers.All(r=>r.sharedMaterial.shader.isSupported),"Unsupported runtime shader");
            var shown=PatriotCapture(Path.Combine(folder,name+".png"));foreach(var r in renderers)r.enabled=false;
            var hidden=PatriotCapture(null);foreach(var r in renderers)r.enabled=true;
            double delta=shown.Zip(hidden,(a,b)=>(double)(Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b))).Average();
            PatriotRequire(delta>.005,name+" did not change actual GPU pixels");return delta;
        }
        // Explicit developer fixture: phase transitions use real damage calls; authored levels are untouched.
        // Invoked by the GPU test and the opt-in Windows verification flag, never ordinary gameplay.
        IEnumerator VerifyPatriotPresentation(string folder,bool quit)
        {
            Directory.CreateDirectory(folder);float oldTime=Time.timeScale;Time.timeScale=0;
            try
            {
                PatriotRequire(TryEnterMonsterTest("CARD_M20"),"Existing M20 entry missing");SelectCard(true);while(ArtLoading)yield return null;
                PatriotRequire(TryDeployAt(Battle.Entry).Success,"Normal level deployment failed");
                for(int i=0;i<500&&!Battle.CombatShots.Any(s=>!s.FromDefender&&s.Applied);i++){Tick(.025f);yield return null;}
                PatriotRequire(Battle.CombatShots.Any(s=>!s.FromDefender&&s.Applied),"Normal authored M20 never hit");
                double normal=PatriotGpuCheck(folder,"patriot-normal-level");
                ClearViews();terminalPresentationElapsed=0;requestSequence=0;
                var s=new ManualBattleSettings("CARD_M20","M20_PATRIOT",30,10,12,1,.1f,1000,.001f,1,1,1,1);
                var c=new EngagementSettings(unitAttack:60,unitArmor:5,unitRange:.75f,unitInterval:4,defenderHealth:10000,defenderAttack:1,defenderRange:.001f,blockCount:1,
                    initialSP:0,requiredSP:0,spPerSecond:0,skillAmmo:0,skillAttack:0,skillInterval:0,skillRange:0,hasSkill:false,melee:true,
                    projectileTime:.02f,attackDuration:1.5f,attackWindup:.75f,effects:definition.Engagement.Snapshot().Effects);
                var entry=new Vector3(-1.8f,.35f,0);Battle=new ManualBattle(s,new[]{entry,entry+Vector3.forward*100},entry+Vector3.right*.4f,c,new[]{new Vector3(1.4f,.8f,0)});
                Battle.ConfigureDefenderHighGround(Battle.Defenders[1].EntityId,true);EnableCombatView();SelectCard(true);
                PatriotRequire(TryDeployAt(Battle.Entry).Success,"Fixture deployment failed");var u=Battle.Units[0];Tick(.9f);yield return null;
                double march=PatriotGpuCheck(folder,"patriot-march");var volley=Battle.CombatShots.Where(x=>!x.FromDefender).Take(4).ToArray();
                PatriotRequire(volley.Length==4&&volley.Count(x=>x.Applied)>=3,"Four-hit volley missing");
                var renderer=PatriotRenderers().First();var props=new MaterialPropertyBlock();renderer.GetPropertyBlock(props);float frame=props.GetFloat("_Frame");var position=renderer.transform.position;
                Battle.SetPaused(true);double clock=Battle.Clock;Tick(10);yield return null;renderer.GetPropertyBlock(props);
                PatriotRequire(Battle.Clock==clock&&props.GetFloat("_Frame")==frame&&position==renderer.transform.position,"Pause moved effect");Battle.SetPaused(false);
                PatriotRequire(Battle.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts),"First fatal damage rejected");Tick(.65f);yield return null;
                double rebirth=PatriotGpuCheck(folder,"patriot-rebirth-begin");double began=u.PatriotPhaseStartedAt;
                Tick((float)(began+57.4-Battle.Clock));yield return null;
                double rise=PatriotGpuCheck(folder,"patriot-rebirth-rise");
                PatriotRequire(u.PatriotPhase==PatriotPhase.Reborn,"60 second waiting period shortened");
                Tick((float)(began+60.1-Battle.Clock));yield return null;
                PatriotRequire(u.PatriotPhase==PatriotPhase.Destruction&&Battle.UnitInvulnerable(u),"Second phase/protection missing");
                for(int i=0;i<100&&!Battle.CombatShots.Any(x=>!x.FromDefender&&x.Applied&&x.StartedAt>began+60);i++){Tick(.025f);yield return null;}
                double attack=PatriotGpuCheck(folder,"patriot-destruction-attack");
                for(int i=0;i<1000&&!Battle.PatriotCasting(u);i++){Tick(.025f);yield return null;}
                PatriotRequire(u.Spear!=null,"Spear not scheduled");var spear=u.Spear;
                PatriotRequire(spear.TargetId==Battle.Defenders[1].EntityId,"Spear ignored authored high ground");
                Tick((float)(spear.StartedAt+.7-Battle.Clock));yield return null;
                double charge=PatriotGpuCheck(folder,"patriot-spear-charge");
                Tick((float)(spear.Shot.ImpactAt+.1-Battle.Clock));yield return null;
                double impact=PatriotGpuCheck(folder,"patriot-spear-impact");
                PatriotRequire(spear.Shot.Applied&&spear.Shot.DamageDealt>0,"Spear had no real damage");
                PatriotRequire(u.ImmolationTicks>0&&u.ImmolationDamage>0,"Aura had no real damage");
                PatriotRequire(!Battle.UnitInvulnerable(u),"Protection outlived 15 seconds");
                PatriotRequire(Battle.TryDamageUnit(u.EntityId,100000,CombatDamageType.Arts),"Final death rejected");Tick(.7f);yield return null;
                double death=PatriotGpuCheck(folder,"patriot-final-death");
                RestartBattle();yield return null;PatriotRequire(PatriotRenderers().Length==0,"Restart left native effects");
                ReturnToMonsterTests();yield return null;PatriotRequire(PatriotRenderers().Length==0,"Switch left native effects");
                string result="{\"passed\":true,\"normalLevel\":true,\"developerPhaseFixture\":true,\"actualDamage\":true,\"pause\":true,\"reset\":true,\"gpu\":\""+SystemInfo.graphicsDeviceType+"\",\"gpuDeltas\":["+string.Join(",",new[]{normal,march,rebirth,rise,attack,charge,impact,death}.Select(x=>x.ToString("0.000000",System.Globalization.CultureInfo.InvariantCulture)))+"]}";
                File.WriteAllText(Path.Combine(folder,"patriot-runtime-check.json"),result);Debug.Log("PATRIOT_VFX_CHECK_PASS "+result);
            }
            finally{Time.timeScale=oldTime;}
            if(quit)Application.Quit(0);
        }
    }
}
