using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed class BattleCardState
    {
        public ManualBattleSettings Settings {get;}
        public int RemainingUses {get;internal set;}
        internal double NextDeploymentTime;
        internal BattleCardState(ManualBattleSettings settings){Settings=settings;RemainingUses=settings.DeploymentLimit;}
    }
    public sealed partial class ManualBattle
    {
        readonly List<BattleCardState> cards=new List<BattleCardState>();
        public IReadOnlyList<BattleCardState> Cards=>cards.AsReadOnly();
        public bool HasCardLoadout=>cards.Count>0;
        public BattleCardState FindCard(string id)=>cards.Find(c=>c.Settings.CardId==id);
        public double CardCooldown(string id)=>Math.Max(0,(FindCard(id)?.NextDeploymentTime??nextDeploymentTime)-Clock);
        public void ConfigureCardLoadout(IEnumerable<BasicRosterProfile> profiles)
        {
            var source=profiles?.ToArray()??throw new ArgumentNullException(nameof(profiles));
            if(source.Any(p=>p==null||p.Unit.CardId==Settings.CardId))throw new ArgumentException("Invalid trial deck.");
            ConfigureRosterProfiles(source);cards.Clear();cards.Add(new BattleCardState(Settings));
            foreach(var p in source)cards.Add(new BattleCardState(p.Unit));
        }
        void ResetCards(){foreach(var c in cards){c.RemainingUses=c.Settings.DeploymentLimit;c.NextDeploymentTime=0;}}
        bool HasAffordableCard()=>HasCardLoadout ? cards.Exists(c=>c.RemainingUses>0 && DP>=c.Settings.DeploymentCost) : RemainingUses>0 && DP>=Settings.DeploymentCost;
        public DeployFailure CheckCardDeployment(string id,Vector3 point,bool allowWhilePaused=false)
        {
            if(string.IsNullOrEmpty(id))return DeployFailure.MissingCard;
            if(!HasCardLoadout)return id==Settings.CardId || rosterProfiles.ContainsKey(id) ? CheckDeploymentState(point,allowWhilePaused,null) : DeployFailure.MissingCard;
            var card=FindCard(id);return card==null?DeployFailure.MissingCard:CheckDeploymentState(point,allowWhilePaused,card);
        }
    }
}
