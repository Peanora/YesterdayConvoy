using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationCampaignRun
    {
        public static readonly string[] CrownKeys = { "CROWN", "AEGIS", "CLOAK" };
        public int CrownPopulation => items.Count(item => CrownKeys.Contains(item.Key));
        public bool TripleCrownUnlocked => modulus.GetValueOrDefault("v08-three-crowns") == 1;
        public int IberiaFirst { get; private set; }
        public int IberiaSecond { get; private set; } = 1;
        public int SteamMainDriver { get; set; }
        public Vector2Int? SuiCentre { get; private set; }
        bool wisadelSelected, wisadelSwitched, v08BattleLocked;
        public bool V08BattleLocked => v08BattleLocked;
        public bool MilitaryTradeAvailable => !v08BattleLocked && TraitCount("行商") > 0 &&
            modulus.GetValueOrDefault("v08-trade-" + CurrentNode.Plane) == 0;

        public bool SelectIberia(int first, int second)
        {
            if(v08BattleLocked || first < 0 || first > 2 || second < 0 || second > 2 || first == second) return false;
            IberiaFirst=first;IberiaSecond=second;return true;
        }
        public bool HasIberiaOption(int option) => TraitCount("伊比利亚") >= 2 &&
            (IberiaFirst==option || TraitCount("伊比利亚")>=4 && IberiaSecond==option);
        public bool CanSelectWisadelMode(int mode) => !v08BattleLocked && mode>=1 && mode<=3 && (!wisadelSelected || !wisadelSwitched || mode==WisadelSkill);
        public bool SelectWisadelMode(int mode)
        {
            if(v08BattleLocked || mode<1 || mode>3 || wisadelSelected && wisadelSwitched && mode!=WisadelSkill) return false;
            if(mode==WisadelSkill && wisadelSelected)return true;
            if(wisadelSelected)wisadelSwitched=true;
            wisadelSelected=true;WisadelSkill=mode;return true;
        }
        public bool SelectSuiCentre(Vector2Int cell)
        {
            if(v08BattleLocked || cell.x<0 || cell.x>=FormationBattleModel.DeploymentColumns ||
                cell.y<0 || cell.y>=FormationBattleModel.Height)return false;
            SuiCentre=cell;return true;
        }
        public ShopTrialModel.Result MilitaryTrade(int option)
        {
            if(!MilitaryTradeAvailable || HasBlockingChoice || option<0 || option>2)return Fail("军需交易当前不可用");
            if(!shop.UnlimitedGold && shop.Gold<8)return Fail("需要8金币");
            shop.AddGold(-8);modulus["v08-trade-"+CurrentNode.Plane]=1;
            if(option==0)RequestComponents("行商军需 · 基础件自选",6);
            else if(option==1)shop.AddExperience(8);
            else shop.FreeRefreshes+=4;
            return Ok("军需交易完成，本位面剩余0次");
        }
        internal void BeginV08Preparation(){v08BattleLocked=false;wisadelSwitched=false;}
        void RefreshV08Economy()
        {
            shop.ExtraPopulation=(HasAugment("CP1")?1:0)+CrownPopulation;
            bool complete=items.Where(item=>item.Owner!=0).GroupBy(item=>item.Owner)
                .Any(group=>CrownKeys.All(key=>group.Any(item=>item.Key==key)));
            if(complete && !TripleCrownUnlocked)
            {modulus["v08-three-crowns"]=1;modulus["v08-crown-node-"+CurrentNode.Id]=1;shop.AddGold(300);}
        }
        void EnterV08Node()
        {
            BeginV08Preparation();
            if(TripleCrownUnlocked && modulus.GetValueOrDefault("v08-crown-node-"+CurrentNode.Id)==0)
            {modulus["v08-crown-node-"+CurrentNode.Id]=1;shop.AddGold(100);}
            if(modulus.GetValueOrDefault("cannott-3star")!=0 && modulus.GetValueOrDefault("v08-can-node-"+CurrentNode.Id)==0)
            {modulus["v08-can-node-"+CurrentNode.Id]=1;shop.AddGold(100);}
        }
    }
}
