using System.Collections;
using System.Linq;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        public IEnumerator VerifyMudrock()
        {
            diagnostic=true;Application.runInBackground=true;Time.timeScale=0;
            var args=System.Environment.GetCommandLineArgs();int at=System.Array.IndexOf(args,"-demoEvidenceDir");string dir=at>=0&&at+1<args.Length?args[at+1]:null;
            bool ok=Battle.Gramophones.Count==3;StartBattle();Battle.SetPaused(true);
            ProcessPointerDown(ScreenPosition(CardSlot(0).center));var receipt=ProcessPointerUp(Camera.main.WorldToScreenPoint(Battle.Entry));
            ok&=receipt.Success&&Battle.IsPaused;Battle.SetPaused(false);Tick(35.1f);yield return InteractionFrame(dir,"mudrock-ready");
            var unit=Battle.Units.FirstOrDefault();ok&=unit!=null&&unit.Active&&Battle.IsSkillReady(unit.EntityId);
            if(unit!=null&&unit.Active){SelectSkillUnit(unit.EntityId);ok&=ActivateSelectedSkill()==SkillFailure.None;var pos=unit.Motion.Position;Tick(.9f);yield return InteractionFrame(dir,"mudrock-capture");ok&=Battle.Gramophones.Any(g=>g.Owner==GramophoneOwner.Monsters)&&unit.Motion.Position==pos;
                Battle.SetPaused(true);double clock=Battle.Clock;Tick(3);yield return InteractionFrame(dir,"mudrock-pause");ok&=Battle.Clock==clock;Battle.SetPaused(false);Tick(.7f);ok&=!Battle.IsSkillActionPlaying(unit);Tick(5);yield return InteractionFrame(dir,"mudrock-device");ok&=Battle.GramophoneActions.Any(a=>a.Applied);}
            RestartBattle();yield return null;ok&=Battle.MudrockCaptures.Count==0&&Battle.Units.Count==0&&Battle.Gramophones.All(g=>g.Captures==0);
            Debug.Log("MUDROCK_PLAYER: drag/pause/charge/capture/actionLock/device/restart="+ok);Application.Quit(ok?0:1);
        }
    }
}
