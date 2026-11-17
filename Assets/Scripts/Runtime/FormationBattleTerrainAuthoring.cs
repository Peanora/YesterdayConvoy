using System;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Saved alongside the scene geometry; combat receives a separate immutable snapshot.</summary>
    [DisallowMultipleComponent]
    public sealed class FormationBattleTerrainAuthoring : MonoBehaviour
    {
        public FormationBattleTerrainRegion[] regions = Array.Empty<FormationBattleTerrainRegion>();
        public FormationBattleTerrainLayout BuildLayout() => new FormationBattleTerrainLayout(regions);
    }
}
