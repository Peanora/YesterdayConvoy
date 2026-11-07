using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        bool dedicatedSynergyMenuOpen;
        void DrawDedicatedSynergyMenu()
        {
            Fill(new Rect(0,0,1280,720),new Color(.90f,.92f,.93f));
            GUI.Label(new Rect(48,28,900,38),"九羁绊专项试验",ApprovedUi.Text(scene.InterfaceFont,28,TextAnchor.MiddleLeft,ApprovedUi.Ink,700));
            GUI.Label(new Rect(48,74,1115,40),"每张地图在编辑器内独立保存；无限金币、九张库存。初始构筑可改，战斗结束可返回整备。",ApprovedUi.Text(scene.InterfaceFont,15,TextAnchor.MiddleLeft,ApprovedUi.Muted));
            if(LoopButton(new Rect(1065,28,166,38),"返回角色测试")){dedicatedSynergyMenuOpen=false;return;}
            for(int i=0;i<SynergyTrialCatalog.Entries.Count;i++)
            {
                var entry=SynergyTrialCatalog.Entries[i];var rect=new Rect(48+i%3*398,137+i/3*174,382,154);
                Fill(rect,new Color(.96f,.97f,.98f));ApprovedUi.Border(rect,ApprovedUi.Rule);
                GUI.Label(new Rect(rect.x+16,rect.y+12,340,30),entry.Title,ApprovedUi.Text(scene.InterfaceFont,21,TextAnchor.MiddleLeft,ApprovedUi.Ink,700));
                string note=entry.Id=="guerrilla"?"初始6档 · 构筑面板可转职冲8/9档":entry.Id=="mudrock"?"I级0经验起步 · 连战培养 · 免费转职达6档":entry.Id=="marine"?"4人 · 接敌落种 · 溟痕扩散与神经爆发":entry.Id=="sarkaz"?"初始6档 · 构筑面板可试2/4/6/8/9档":entry.Members.Count+"人 · 初始达成最高"+entry.MaximumTier+"档";
                GUI.Label(new Rect(rect.x+16,rect.y+52,350,28),note,ApprovedUi.Text(scene.InterfaceFont,12,TextAnchor.MiddleLeft,ApprovedUi.Muted));
                if(LoopButton(new Rect(rect.x+16,rect.y+100,350,36),"进入 "+entry.Title+" 试验",true))SceneManager.LoadScene(entry.SceneName);
            }
            GUI.Label(new Rect(48,678,1184,25),"标签保留原本交集；未部署/死亡不改变战前档位。综合无限金币入口仍然保留。",ApprovedUi.Text(scene.InterfaceFont,12,TextAnchor.MiddleLeft,ApprovedUi.Muted));
        }
    }
}
