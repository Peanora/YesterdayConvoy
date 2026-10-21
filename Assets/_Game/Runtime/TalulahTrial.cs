using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        [SerializeField] private ManualBattleDefinition talulahDefinition;
        private ManualBattleDefinition wisadelDefinition;
        public bool TalulahActive => definition==talulahDefinition && talulahDefinition!=null;
        public string SkillName => TalulahActive ? "焚天火雨" : definition==wisadelDefinition ? "爆裂黎明" : "主动技能";

        public void SwitchTrial(bool talulah)
        {
            var next=talulah ? talulahDefinition : wisadelDefinition;
            if(next==null) throw new InvalidOperationException("Trial configuration is missing.");
            SwitchTestDefinition(next);
        }
        private void SwitchTestDefinition(ManualBattleDefinition next)
        {
            ClearViews(); CancelInteraction(); notice=null; requestSequence=0; selectedEntityId=null;
            definition=next; SimulationSpeed=1; TestSelectionOpen=false; settlementPointerDown=selectionReturnPointerDown=false;
            Battle=CreateBattle(); ValidateConfiguration();
            cardView.SetCard(definition.Card); preview.sprite=OriginalTalulahArt.BattleSprite(definition.Card);
            EnableCombatView(); UpdatePreview();
            Feedback="拖动底部角色卡至红门部署";
        }

        private void DrawTrialSelector()
        {
            if(GUI.Button(TestReturnRect,"返回怪物选择",buttonStyle)) ReturnToMonsterTests();
            if(SlugActive)
            {
                GUI.Label(new Rect(24,631,650,28),"源石虫 · 近战普通攻击 · 无主动技能",smallStyle);
                GUI.Label(new Rect(24,659,650,26),"可部署3次 · 单次1 DP · 空格暂停 / 支持暂停部署",smallStyle);
            }
            else if(TalulahActive)
            {
                GUI.Label(new Rect(24,631,650,28),"焚天火雨 · 满技力后点击角色 → 技能图标",smallStyle);
                GUI.Label(new Rect(24,659,650,26),"6秒 / 6波全图打击 · 施法原地不动 · 空格暂停",smallStyle);
            }
        }

        private IEnumerator TalulahFrame(string directory,string name,bool zoom=false)
        {
            yield return null; yield return null;
            Directory.CreateDirectory(directory);
            Camera camera=Camera.main; var oldTarget=camera.targetTexture; var oldActive=RenderTexture.active;
            Vector3 oldPosition=camera.transform.position; float oldAspect=camera.aspect;
            int width=zoom ? 512 : 1280,height=zoom ? 512 : 720;
            var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);
            var image=new Texture2D(width,height,TextureFormat.RGBA32,false);
            try
            {
                if(zoom)
                {
                    camera.transform.position=Battle.Units[0].Motion.Position+camera.transform.up*.91f-camera.transform.forward*5;
                    camera.aspect=1;
                }
                camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,width,height),0,0); image.Apply();
                File.WriteAllBytes(Path.Combine(directory,name+"-camera.png"),image.EncodeToPNG());
            }
            finally
            { camera.targetTexture=oldTarget; camera.transform.position=oldPosition; camera.aspect=oldAspect;
              RenderTexture.active=oldActive; target.Release(); Destroy(target); Destroy(image); }
        }

        private IEnumerator VerifyTalulahMotion(string directory)
        {
            if(directory==null) throw new InvalidOperationException("Motion evidence needs an output directory.");
            Time.timeScale=0; SwitchTrial(true); SelectCard(true); TryDeployAt(Battle.Entry);
            for(int i=0;i<36;i++) { Tick(1f/30); yield return TalulahFrame(directory,"walk-"+i.ToString("00"),true); }
            var unit=Battle.Units[0];
            for(int i=0;i<300 && double.IsInfinity(unit.LastAttackTime);i++) { Tick(.05f); yield return null; }
            for(int i=0;i<33;i++) { Tick(1f/30); yield return TalulahFrame(directory,"attack-"+i.ToString("00"),true); }
            for(int i=0;i<200 && !Battle.IsSkillReady(unit.EntityId);i++) { Tick(.05f); yield return null; }
            SelectSkillUnit(unit.EntityId); bool cast=ActivateSelectedSkill()==SkillFailure.None;
            for(int i=0;i<180;i++) { Tick(1f/30); yield return TalulahFrame(directory,"rain-"+i.ToString("000")); }
            Debug.Log("TALULAH_MOTION_EVIDENCE: walk36; attack33; rain180; cast="+cast+"; camera-only, zoom for character, normal map camera for rain.");
            Time.timeScale=1; Application.Quit(cast ? 0 : 1);
        }

        private IEnumerator VerifyTalulah(string directory)
        {
            Time.timeScale=0; SwitchTrial(true); Battle.Begin(); Battle.SetPaused(true);
            Vector2 card=ScreenPosition(RosterSlot.center), entry=Camera.main.WorldToScreenPoint(Battle.Entry);
            ProcessPointerDown(card); ProcessPointerMove(entry);
            bool placed=ProcessPointerUp(entry).Success && Battle.IsPaused;
            Tick(10); bool pauseDeploy=Battle.Clock==0 && Battle.Units.Count==1;
            Battle.SetPaused(false); Tick(4.05f);
            var unit=Battle.Units[0]; string id=unit.EntityId;
            bool ready=Battle.IsSkillReady(id);
            OpenSkillMenu(id); double clock=Battle.Clock; Tick(1);
            bool slow=SkillMenuOpen && Math.Abs(Battle.Clock-clock-.03)<.001;
            if(directory!=null) yield return TalulahFrame(directory,"talulah-ready");
            bool cast=CastFocusedSkill()==SkillFailure.None;
            double start=unit.Motion.DistanceTravelled; Tick(1.25f); yield return null;
            bool global=true; foreach(var d in Battle.Defenders) global &= d.Health<Battle.CombatSettings.DefenderHealth;
            Battle.SetPaused(true); clock=Battle.Clock; int shots=Battle.CombatShots.Count;
            Tick(10); bool freeze=Battle.Clock==clock && shots==Battle.CombatShots.Count;
            if(directory!=null) yield return TalulahFrame(directory,"talulah-fire-rain");
            Battle.SetPaused(false); Tick(4.5f);
            bool locked=unit.Motion.DistanceTravelled==start && Battle.IsSkillActionPlaying(unit);
            bool allDead=true; foreach(var d in Battle.Defenders) allDead &= !d.Alive;
            Tick(.35f); bool resume=unit.Motion.DistanceTravelled>start;
            for(int i=0;i<1500 && Battle.Phase==BattlePhase.Running;i++) Tick(.05f);
            bool won=Battle.Phase==BattlePhase.Won;
            if(directory!=null) yield return TalulahFrame(directory,"talulah-victory");
            RestartBattle();
            bool reset=Battle.Units.Count==0 && Battle.CombatShots.Count==0 && Battle.SkillCasts==0;
            foreach(var d in Battle.Defenders) reset &= d.Alive && d.Health==Battle.CombatSettings.DefenderHealth;
            SwitchTrial(false); bool original=Definition.Card==scene.Card && Battle.Defenders.Count==1;
            Debug.Log($"TALULAH_CHECK: drag={placed}; pauseDeploy={pauseDeploy}; ready={ready}; slow={slow}; cast={cast}; global={global}; freeze={freeze}; locked={locked}; allDead={allDead}; resume={resume}; won={won}; reset={reset}; original={original}.");
            Time.timeScale=1; Application.Quit(placed && pauseDeploy && ready && slow && cast && global && freeze && locked && allDead && resume && won && reset && original ? 0 : 1);
        }
    }
}
