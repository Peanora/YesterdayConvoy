using System;

namespace YesterdayConvoy
{
    public sealed partial class ManualBattle
    {
        internal float V2DefenderSkill(DefenderState defender,string key,float legacy)
        {
            if(!CampaignV2Enabled)return legacy;
            if(CampaignV2Stage<1||CampaignV2Stage>CampaignJourneyDefinition.StageCount)throw new InvalidOperationException("V2 defender skills require their authored stage.");
            var stage=CampaignJourneyCatalog.Stage(CampaignV2Stage);
            float value=stage.GuardValue(defender.Settings.Id,key,float.NaN);
            if(float.IsNaN(value)||float.IsInfinity(value))throw new InvalidOperationException("Missing authored v2 defender parameter: "+CampaignV2Stage+"/"+defender.Settings.Id+"/"+key);
            return value;
        }
    }
}
