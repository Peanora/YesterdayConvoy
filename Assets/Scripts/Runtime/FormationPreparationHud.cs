using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        static readonly Color EconomyPanel=new Color(.095f,.145f,.18f,.98f);
        static readonly Color EconomyLine=new Color(.30f,.39f,.43f,1);
        static readonly Color EconomyInk=new Color(.91f,.94f,.95f,1);
        static readonly Color EconomyMuted=new Color(.60f,.68f,.72f,1);
        static readonly Color EconomyGold=new Color(.89f,.76f,.46f,1);

        void DrawFormationEconomyHud(bool preparing)
        {
            var r=FormationExperienceRect;
            Fill(r,EconomyPanel);ApprovedUi.Border(r,EconomyLine);
            EconomyLabel(new Rect(r.x+12,r.y+8,r.width-24,18),"团队经验",11,EconomyMuted);
            EconomyLabel(new Rect(r.x+12,r.y+29,75,32),Shop.Level+"级",27,EconomyInk,true);
            EconomyLabel(new Rect(r.x+78,r.y+35,r.width-90,24),Shop.Level==10?"最高等级":$"{Shop.XP} / {Shop.NextLevelXP}",12,EconomyMuted,false,TextAnchor.MiddleRight);
            var track=new Rect(r.x+12,r.y+67,r.width-24,4);
            Fill(track,new Color(.20f,.28f,.32f));
            float progress=Shop.Level==10?1:Mathf.Clamp01((float)Shop.XP/Shop.NextLevelXP);
            Fill(new Rect(track.x,track.y,track.width*progress,track.height),new Color(.59f,.73f,.78f));
            bool old=GUI.enabled;GUI.enabled=old&&preparing&&CanTrade&&Shop.Level<10&&Shop.CanAfford(4);
            if(approvedShop.LoopButton(new Rect(r.x+12,r.y+86,r.width-24,27),$"经验 +{Shop.PurchasedXP}   4金币"))BuyExperience();
            GUI.enabled=old;

            var coin=FormationCoinRect;
            Fill(coin,EconomyPanel);ApprovedUi.Border(coin,marketVisible?EconomyGold:EconomyLine,marketVisible?2:1);
            EconomyLabel(new Rect(coin.x+12,coin.y+7,coin.width-24,18),"招募商店",11,EconomyMuted);
            if(formationCoin!=null)GUI.DrawTexture(new Rect(coin.x+13,coin.y+30,18,18),formationCoin,ScaleMode.ScaleToFit,true);
            EconomyLabel(new Rect(coin.x+38,coin.y+23,coin.width-51,33),Shop.UnlimitedGold?"∞":Shop.Gold.ToString(),29,EconomyGold,true,TextAnchor.MiddleRight);
            EconomyLabel(new Rect(coin.x+8,coin.yMax-19,coin.width-16,14),marketVisible?"点击收起":"点击展开",9,EconomyMuted,false,TextAnchor.MiddleCenter);
            EconomyLabel(FormationInterestRect,$"预计利息 +{FormationDisplayedInterest}",10,EconomyGold,false,TextAnchor.MiddleCenter);
            GUI.enabled=old&&preparing;
            bool coinClicked=GUI.Button(coin,new GUIContent(string.Empty,"招募商店 · 点击或Tab展开"),GUIStyle.none);
            bool interestClicked=GUI.Button(FormationInterestRect,new GUIContent(string.Empty,"按当前金币计算的本场利息；点击展开商店"),GUIStyle.none);
            if(coinClicked||interestClicked)ToggleFormationMarket();
            GUI.enabled=old;
        }

        void EconomyLabel(Rect rect,string value,int size,Color color,bool bold=false,TextAnchor alignment=TextAnchor.MiddleLeft)
            =>GUI.Label(rect,value,approvedShop.LoopText(size,alignment,color,bold?650:400));
    }
}
