using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace YesterdayConvoy.Editor
{
    public static class FormationCampaignTools
    {
        [MenuItem("Yesterday Convoy/Formation Battle/Author Complete v0.7 Challenge")]
        public static void Author()
        {
            FormationDeploymentTools.RebuildExpandedBoard();
            GameSceneCatalog.UpdateBuildSettings();
            Validate();
            Debug.Log("FORMATION_FULL_AUTHOR_PASS: approved v0.7 rules, complete card references and restored 26x8 board");
        }
        [MenuItem("Yesterday Convoy/Formation Battle/Validate Complete v0.7 Challenge")]
        public static void Validate()
        {
            FormationDeploymentTools.Validate();
            if(FormationCampaignRules.AllUnits.Length!=46||FormationCampaignRules.Nodes.Length!=23||FormationCampaignRules.AllPreplans.Length!=16||FormationCampaignRules.AllAugments.Length!=36)
                throw new InvalidOperationException("Full v0.7 content tables are incomplete.");
            if(FormationCampaignRules.Nodes.Count(n=>n.Kind!="补给")!=20)
                throw new InvalidOperationException("Full challenge must contain 20 battles and 3 supply nodes.");
            var c=Object.FindAnyObjectByType<FormationBattleController>();
            foreach(var unit in FormationCampaignRules.AllUnits)
            {
                var catalog=FormationUnitCatalog.Find(unit.CardId);
                if(catalog==null||!c.formationCards.Any(card=>card.CardId==unit.CardId))
                    throw new InvalidOperationException("Missing full-challenge unit: "+unit.Name);
            }
            if(FormationBattleModel.Width!=26||FormationBattleModel.Height!=8||FormationBattleModel.DeploymentColumns!=12)
                throw new InvalidOperationException("Both fixed views need comparable usable board area.");
            if(FormationCampaignRules.ExperienceToNext[8]!=80||FormationCampaignRules.ExperienceToNext[9]!=100)
                throw new InvalidOperationException("Approved high-level experience curve has regressed.");
            Debug.Log("FORMATION_FULL_VALIDATE_PASS: 46 units, 16 preplans, 36 augments, 21 advanced recipes, 23 nodes and restored authored board");
        }
    }
}
