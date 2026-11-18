using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        void PresentFormationTalulahRain(FormationBattleModel.LegacyZoneSnapshot zone, FormationActor source)
        {
            if (!ViewingDefenders || !UseFormationBattleCamera || Model.Clock < zone.At || Model.Clock >= zone.Until) return;
            var stream = BossRosterSupplementArt.Talulah("original-energy-stream");
            if (!stream.Sequence.Ready) return;

            const float firstColumn = FormationBattleModel.BattleAreaStart + .10f;
            const float lastColumn = FormationBattleModel.Width - 1f;
            const float staggerPerRow = .016f;
            float zoneAge = (float)(Model.Clock - zone.At);
            float scale = LegacyScale(source);
            LegacyTalulah("target-burn-loop", zoneAge * LegacyVisualRate, zone.Center, source, -2, true);
            for (int row = 0; row < FormationBattleModel.Height; row++)
            {
                float rowAge = zoneAge - row * staggerPerRow;
                if (rowAge < 0) continue;
                float frameAge = Mathf.Repeat(rowAge * LegacyVisualRate, stream.Duration);
                var cell = new Vector2(Mathf.Lerp(firstColumn, lastColumn, frameAge / stream.Duration), row);
                // Keep the approved horizontal fire model at the current tile's top
                // surface as it crosses the full board, including empty lanes.
                Vector3 point = ActorWorldPoint(cell) + battleCamera.transform.up * .12f;
                LegacyDraw(stream.At(frameAge), legacyHdrMaterial, point, cell, scale, -5, hdr: stream.HdrRange);
            }
        }
    }
}
