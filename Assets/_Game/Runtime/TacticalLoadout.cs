using System;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        int selectedRosterIndex;
        public int RosterCount=>TacticalMapActive ? 1+(defenderTrial.rosterCards?.Length??0) : 1;
        public int SelectedRosterIndex=>selectedRosterIndex;
        string VictoryUnitName {get {foreach(var unit in Battle.Units)if(unit.Resolution==UnitResolution.Breached)return CardForUnit(unit).DisplayName;return definition.Card.DisplayName;}}
        public Rect CardSlot(int index)=>TacticalMapActive ? new Rect(1126-(RosterCount-1-index)*124,556,112,144) : RosterSlot;
        public ManualBattleDefinition RosterDefinition(int index)=>index==0 ? definition : defenderTrial.rosterCards[index-1];
        private double RosterDeathTail {get {double tail=0;for(int i=0;i<RosterCount;i++)tail=Math.Max(tail,LocalMonsterArt.For(RosterDefinition(i).Card.CardId)?.DeathDuration??0);return tail;}}
        private ManualBattleDefinition DeploymentDefinition=>RosterDefinition(Mathf.Clamp(selectedRosterIndex,0,RosterCount-1));
        public CardDefinition CardForUnit(DeployedUnitState unit)
        {
            if(unit!=null)for(int i=0;i<RosterCount;i++)if(RosterDefinition(i).Card.CardId==unit.CardId)return RosterDefinition(i).Card;
            return definition.Card;
        }
        float UnitVisualHeight(DeployedUnitState unit)
        {
            var card=CardForUnit(unit);return LocalMonsterArt.For(card.CardId)?.Height ?? (card.CardId==OriginalSlugArt.CardId?1:1.8f);
        }
        void DrawTacticalRoster()
        {
            var label=new GUIStyle(smallStyle){alignment=TextAnchor.MiddleCenter,fontSize=15};
            for(int i=0;i<RosterCount;i++)
            {
                var card=RosterDefinition(i).Card;var state=Battle.FindCard(card.CardId);if(state==null)continue;
                Rect slot=CardSlot(i);double cooldown=Battle.CardCooldown(card.CardId);
                Fill(slot,Dragging && selectedRosterIndex==i ? new Color(.48f,.85f,.91f):new Color(.30f,.36f,.41f));
                Fill(new Rect(slot.x+2,slot.y+2,108,140),new Color(.07f,.09f,.12f));
                DrawTestPortrait(new Rect(slot.x+5,slot.y+5,102,106),OriginalTalulahArt.CardPortrait(card));
                Fill(new Rect(slot.x+2,slot.y+116,108,26),new Color(.16f,.22f,.27f));
                GUI.Label(new Rect(slot.x+2,slot.y+117,108,24),card.DisplayName,label);
                Fill(new Rect(slot.x+77,slot.y+2,33,30),new Color(.14f,.18f,.22f));
                GUI.Label(new Rect(slot.x+77,slot.y+2,33,30),state.Settings.DeploymentCost.ToString(),numberStyle);
                GUI.Label(new Rect(slot.x,slot.y-3,55,26),state.RemainingUses+"次",label);
                if(cooldown>0 || Battle.DP<state.Settings.DeploymentCost || state.RemainingUses==0)
                {
                    float height=cooldown>0?112*Mathf.Clamp01((float)(cooldown/state.Settings.Cooldown)):112;
                    Fill(new Rect(slot.x+2,slot.y+2,108,height),new Color(.02f,.03f,.04f,.7f));
                    GUI.Label(new Rect(slot.x+2,slot.y+50,108,28),cooldown>0?$"{cooldown:0.0}s":state.RemainingUses==0?"次数用尽":"费用不足",label);
                }
            }
        }
    }
}
