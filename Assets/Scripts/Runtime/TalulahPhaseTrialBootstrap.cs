using System;
using System.Collections;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed class TalulahPhaseTrialBootstrap:MonoBehaviour
    {
        public const string SceneName="TalulahPhaseTrial";
        public DefenderTrialDefinition encounter;
        public int initialDP=20,initialDefense=1;
        public float firstHealth=4250,firstAttack=336,firstArmor=12;
        internal DefenderTrialDefinition RuntimeEncounter {get;private set;}
        IEnumerator Start()
        {
            // ManualBattleController has execution order -100, so its one Start is already complete.
            // This scene has no CampaignBootstrap/ShopMapBootstrap and no second combat starter.
            if(encounter==null)throw new InvalidOperationException("Talulah phase encounter is missing.");
            _=encounter.Snapshot();RuntimeEncounter=Instantiate(encounter);
            yield return FindAnyObjectByType<ManualBattleController>().PrepareTalulahPhaseTrial(this);
        }
        internal ManualBattle CreateBattle(ManualBattleDefinition talulah)
        {
            if(RuntimeEncounter==null)throw new InvalidOperationException("Talulah phase runtime encounter is not ready.");
            var profile=CampaignBalance.Build(talulah,3,initialDP,initialDefense);var source=profile.Unit;
            var unit=new ManualBattleSettings(source.CardId,source.UnitId,initialDP,initialDefense,1,source.DeploymentCost,source.Cooldown,
                firstHealth,source.Speed,source.GateDamage,source.TowerRange,source.TowerDamage,source.ShotInterval);
            // Requires the approved Talulah domain patch. The flag is runtime-only; no shared Engagement asset changes.
            var effects=CampaignBalance.CopyEffects(profile.Combat.Effects,talulahSecondPhase:true);
            var combat=CampaignBalance.CopyCombat(profile.Combat,firstAttack,firstArmor,effects);
            var result=new ManualBattle(unit,RuntimeEncounter.route,RuntimeEncounter.defenders[0].position,combat,null,RuntimeEncounter.Snapshot());
            foreach(var defender in result.Defenders)result.ConfigureDefenderHighGround(defender.EntityId,true);
            return result;
        }
        void OnDestroy(){if(RuntimeEncounter!=null)Destroy(RuntimeEncounter);}
    }
    public sealed partial class ManualBattleController
    {
        TalulahPhaseTrialBootstrap talulahPhaseTrial;
        bool TalulahPhaseTrialActive=>talulahPhaseTrial!=null&&definition==talulahDefinition;
        internal IEnumerator PrepareTalulahPhaseTrial(TalulahPhaseTrialBootstrap trial)
        {
            if(trial==null||trial.RuntimeEncounter==null)throw new InvalidOperationException("Talulah phase scene is incomplete.");
            return Prepare(Plan(trial.RuntimeEncounter.defenders,new[]{talulahDefinition.Card},false,talulahSecondPhase:true),()=>{
                talulahPhaseTrial=trial;defenderTrial=trial.RuntimeEncounter;
                SwitchTestDefinition(talulahDefinition,true);
                Feedback="三星塔露拉 · 拖到红门部署 · 正常承伤进入双阶段；满技力可手动火雨";
            },"准备三星塔露拉完整双阶段");
        }
    }
}
