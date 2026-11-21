using System;
using UnityEngine;

namespace YesterdayConvoy
{
    [Serializable]
    public struct FormationGuardPositions
    {
        public Vector2Int[] front, rear;
        public Vector2Int reconstruction, chess;

        internal FormationCampaignRules.GuardFormation Build() =>
            new FormationCampaignRules.GuardFormation((Vector2Int[])front.Clone(), (Vector2Int[])rear.Clone(), reconstruction, chess);
    }

    /// <summary>Saved alongside the scene geometry; combat receives a separate immutable snapshot.</summary>
    [DisallowMultipleComponent]
    public sealed class FormationBattleTerrainAuthoring : MonoBehaviour
    {
        public FormationBattleTerrainRegion[] regions = Array.Empty<FormationBattleTerrainRegion>();
        public FormationGuardPositions[] guardPositions = Array.Empty<FormationGuardPositions>();

        public FormationBattleTerrainLayout BuildLayout() => guardPositions.Length == 0
            ? new FormationBattleTerrainLayout(regions)
            : new FormationBattleTerrainLayout(regions, guardPositions[0].Build(), guardPositions[1].Build());
    }
}
