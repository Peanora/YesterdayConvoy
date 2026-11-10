using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        bool dedicatedSynergyMenuOpen;
        bool dedicatedStyleTab;
        void DrawDedicatedSynergyMenu()
        {
            Fill(new Rect(0,0,1280,720),new Color(.90f,.92f,.93f));
            GUI.Label(new Rect(48,22,900,38),dedicatedStyleTab?"六打法羁绊试玩":"九阵营羁绊专项",ApprovedUi.Text(scene.InterfaceFont,28,TextAnchor.MiddleLeft,ApprovedUi.Ink,700));
            GUI.Label(new Rect(48,62,1115,25),"独立保存地图；无限金币、真实有限库存，8级/100DP起步。可换编队、升星，战后返回整备。",ApprovedUi.Text(scene.InterfaceFont,14,TextAnchor.MiddleLeft,ApprovedUi.Muted));
            if(LoopButton(new Rect(48,95,190,30),"阵营羁绊 · 九条",!dedicatedStyleTab))dedicatedStyleTab=false;
            if(LoopButton(new Rect(250,95,190,30),"打法羁绊 · 六条",dedicatedStyleTab))dedicatedStyleTab=true;
            if(LoopButton(new Rect(1065,28,166,38),"返回角色测试")){dedicatedSynergyMenuOpen=false;return;}
            var entries=dedicatedStyleTab?SynergyTrialCatalog.StyleEntries:SynergyTrialCatalog.Entries;
            for(int i=0;i<entries.Count;i++)
            {
                var entry=entries[i];var rect=new Rect(48+i%3*398,137+i/3*174,382,154);
                Fill(rect,new Color(.96f,.97f,.98f));ApprovedUi.Border(rect,ApprovedUi.Rule);
                GUI.Label(new Rect(rect.x+16,rect.y+12,340,30),entry.Title,ApprovedUi.Text(scene.InterfaceFont,21,TextAnchor.MiddleLeft,ApprovedUi.Ink,700));
                string note=dedicatedStyleTab?entry.MaximumTier+"档＋混搭支援 · 阵营同时生效":entry.Id=="guerrilla"?"初始6档 · 构筑面板可转职冲8/9档":entry.Id=="mudrock"?"I级0经验起步 · 连战培养 · 免费转职达6档":entry.Id=="marine"?"4人 · 接敌落种 · 溟痕扩散与神经爆发":entry.Id=="sarkaz"?"初始6档 · 构筑面板可试2/4/6/8/9档":entry.Members.Count+"人 · 初始达成最高"+entry.MaximumTier+"档";
                GUI.Label(new Rect(rect.x+16,rect.y+52,350,28),note,ApprovedUi.Text(scene.InterfaceFont,12,TextAnchor.MiddleLeft,ApprovedUi.Muted));
                if(LoopButton(new Rect(rect.x+16,rect.y+100,350,36),"进入 "+entry.Title+" 试验",true))SceneManager.LoadScene(entry.SceneName);
            }
            GUI.Label(new Rect(48,678,1184,25),dedicatedStyleTab?"左侧点击打法查看全部成员/档位。强攻先接阻挡，射手双来源，秘术三次普攻，壁垒先承伤，狂袭半血，协同接续SP。":"九阵营专项保持原有独立验证；六打法专项和综合试玩中，两层羁绊同时生效。",ApprovedUi.Text(scene.InterfaceFont,12,TextAnchor.MiddleLeft,ApprovedUi.Muted));
        }
    }
}
