using System;
using UnityEngine;
namespace YesterdayConvoy
{
    [CreateAssetMenu(menuName="Yesterday Convoy/Boss Arena")]
    public sealed class BossArenaDefinition:ScriptableObject
    {
        public SynergyTrialDefinition preparation;
        [Range(60,120)]public float redeploymentSeconds=90;
        [Range(1.1f,4)]public float enragedAttackSpeed=2;
        [Range(1,5)]public float bossVisualScale=2.6f;
        public string skinName="超新星";
        public void Validate()
        {
            if(preparation==null)throw new InvalidOperationException("Missing arena preparation.");
            preparation.Validate();_=new BossArenaRules(redeploymentSeconds,enragedAttackSpeed);
            var guards=preparation.encounter.defenders;
            if(guards.Length!=1||guards[0].id!=BossArenaRules.BossId||bossVisualScale<1||bossVisualScale>5)
                throw new InvalidOperationException("The arena requires one authored Wisadel boss.");
        }
    }
}
