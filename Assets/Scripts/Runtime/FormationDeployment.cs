using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed class FormationDeploymentPreview
    {
        public bool Valid { get; }
        public string Failure { get; }
        public RectInt Cells { get; }
        public int SwapInstance { get; }
        public RectInt SwapCells { get; }
        internal int SwapIndex { get; }

        internal FormationDeploymentPreview(bool valid, string failure, RectInt cells,
            int swapInstance = 0, RectInt swapCells = default, int swapIndex = -1)
        { Valid = valid; Failure = failure; Cells = cells; SwapInstance = swapInstance; SwapCells = swapCells; SwapIndex = swapIndex; }

        public static FormationDeploymentPreview Invalid(string failure, RectInt cells = default) =>
            new FormationDeploymentPreview(false, failure, cells);
    }

    /// <summary>Preparation footprints only. Combat movement and attack ranges remain centre based.</summary>
    public static class FormationDeployment
    {
        public static Vector2 Center(Vector2 anchor, Vector2Int footprint) =>
            anchor + new Vector2((footprint.x - 1) * .5f, (footprint.y - 1) * .5f);

        public static RectInt Cells(Vector2 anchor, Vector2Int footprint) =>
            new RectInt(Mathf.RoundToInt(anchor.x), Mathf.RoundToInt(anchor.y), footprint.x, footprint.y);

        public static bool TryCells(Vector2 anchor, Vector2Int footprint, out RectInt cells)
        {
            cells = default;
            if (float.IsNaN(anchor.x) || float.IsNaN(anchor.y) || float.IsInfinity(anchor.x) ||
                float.IsInfinity(anchor.y) || anchor.x < 0 || anchor.y < 0 ||
                anchor.x >= FormationBattleModel.DeploymentColumns || anchor.y >= FormationBattleModel.Height ||
                Mathf.Abs(anchor.x - Mathf.RoundToInt(anchor.x)) > .0001f ||
                Mathf.Abs(anchor.y - Mathf.RoundToInt(anchor.y)) > .0001f) return false;
            cells = Cells(anchor, footprint); return Fits(cells);
        }

        public static bool Fits(RectInt cells) => cells.width > 0 && cells.height > 0 &&
            cells.xMin >= 0 && cells.yMin >= 0 && cells.xMax <= FormationBattleModel.DeploymentColumns &&
            cells.yMax <= FormationBattleModel.Height;

        public static bool IsFree(RectInt cells, IReadOnlyList<RectInt> occupied, int ignore = -1, int ignoreOther = -1)
        {
            if (!Fits(cells)) return false;
            for (int i = 0; i < occupied.Count; i++)
                if (i != ignore && i != ignoreOther && cells.Overlaps(occupied[i])) return false;
            return true;
        }

        /// <summary>Builds the complete move/swap before either side is changed.</summary>
        public static FormationDeploymentPreview Preview(IReadOnlyList<RectInt> occupied, int movingIndex,
            Vector2Int anchor, IReadOnlyList<int> instances = null)
        {
            if (occupied == null || movingIndex < 0 || movingIndex >= occupied.Count)
                return FormationDeploymentPreview.Invalid("角色不在当前编队");
            var original = occupied[movingIndex];
            var target = new RectInt(anchor.x, anchor.y, original.width, original.height);
            if (!Fits(target)) return FormationDeploymentPreview.Invalid("完整占地必须位于左侧部署区", target);
            int swap = -1;
            for (int i = 0; i < occupied.Count; i++)
            {
                if (i == movingIndex || !target.Overlaps(occupied[i])) continue;
                if (swap >= 0) return FormationDeploymentPreview.Invalid("落点覆盖多个角色，请选择空位", target);
                swap = i;
            }
            if (swap < 0) return new FormationDeploymentPreview(true, null, target);
            var displaced = new RectInt(original.x, original.y, occupied[swap].width, occupied[swap].height);
            if (!IsFree(displaced, occupied, movingIndex, swap) || displaced.Overlaps(target))
                return FormationDeploymentPreview.Invalid("交换后占地越界或重叠，已保留原位置", target);
            int instance = instances != null && swap < instances.Count ? instances[swap] : 0;
            return new FormationDeploymentPreview(true, null, target, instance, displaced, swap);
        }
    }
}
