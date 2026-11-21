using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleModel
    {
        static readonly Vector2Int[] WangCrossOffsets =
        {
            Vector2Int.zero, Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up
        };
        const double WangPlacementInterval = .25;
        const double WangDetonationInterval = .25;
        const double WangAfterglow = .55;
        const double MlynarInterval = 1.2;
        const double MlynarSkillLength = 26;
        const double MlynarSlashTime = 32d / 60;

        internal FormationGuardEffect[] GuardChessPieces(FormationDefender guard) =>
            guardKits.TryGetValue(guard.Id, out var kit) && kit.Guard == guard && guard.Alive
                ? kit.ChessPieces ?? Array.Empty<FormationGuardEffect>() : Array.Empty<FormationGuardEffect>();

        bool WangCrossFits(Vector2Int center)
        {
            foreach (var offset in WangCrossOffsets)
            {
                Vector2 cell = center + offset;
                if (cell.x < DeploymentColumns + 2 || cell.x >= Width - 1 || cell.y < 0 || cell.y >= Height ||
                    (Terrain != null && (!Terrain.CanPlaceDefender(cell) || !Terrain.IsWalkable(cell) || Terrain.HeightAt(cell) != 0)) ||
                    Defenders.Any(other => other.Alive && (other.Position - cell).sqrMagnitude < .01f))
                    return false;
            }
            return true;
        }

        internal Vector2Int? FindWangCrossCenter(FormationDefender guard)
        {
            if (!guard.SkillCell.HasValue) return null;
            Vector2Int anchor = guard.SkillCell.Value;
            return (from x in Enumerable.Range(DeploymentColumns + 3, Width - DeploymentColumns - 5)
                    from y in Enumerable.Range(1, Height - 2)
                    let cell = new Vector2Int(x, y)
                    where (cell - anchor).sqrMagnitude <= 16 && WangCrossFits(cell)
                    let otherHalf = (anchor.y < Height / 2) != (y < Height / 2)
                    orderby otherHalf, (cell - anchor).sqrMagnitude, cell.y, cell.x
                    select (Vector2Int?)cell).FirstOrDefault();
        }

        void TickGuardChess(FormationDefender guard, GuardKit kit)
        {
            if (GuardEffect(guard) != null || Clock < kit.NextPlacement) return;
            if (!kit.WangCenter.HasValue || !WangCrossFits(kit.WangCenter.Value))
                kit.WangCenter = FindWangCrossCenter(guard);
            if (kit.WangCenter is not { } center)
            {
                kit.NextPlacement = Clock + WangPlacementInterval;
                return;
            }
            if (GuardTargets().Any(unit => WangCrossOffsets.Any(offset => GuardInRange(center + offset, unit, .8f))))
                PlaceGuardChess(guard, kit, center);
        }

        void PlaceGuardChess(FormationDefender guard, GuardKit kit, Vector2Int center)
        {
            StartGuardCast(guard, kit, null, 5d / 60);
            double firstDetonation = Clock + WangCrossOffsets.Length * WangPlacementInterval;
            double end = firstDetonation + (WangCrossOffsets.Length - 1) * WangDetonationInterval + WangAfterglow;
            kit.ActiveUntil = end;
            kit.NextPlacement = end + 24;
            kit.Effect = new FormationGuardEffect
            {
                Kind = "chess_board", Origin = center, PlacedAt = Clock, StartedAt = Clock, EndsAt = end
            };
            kit.ChessPieces = new FormationGuardEffect[WangCrossOffsets.Length];
            for (int index = 0; index < WangCrossOffsets.Length; index++)
            {
                double placedAt = Clock + index * WangPlacementInterval;
                var stone = new FormationGuardEffect
                {
                    Kind = "chess", Origin = center + WangCrossOffsets[index], Radius = 1.4f,
                    PlacedAt = placedAt, StartedAt = placedAt, EndsAt = end
                };
                kit.ChessPieces[index] = stone;
                ScheduleOwned(guard, firstDetonation + index * WangDetonationInterval, () => DetonateWangStone(guard, stone));
            }
            ScheduleOwned(guard, end, () => BeginGuardAction(guard, true, 8d / 60, motion: "skill_end"));
        }

        void DetonateWangStone(FormationDefender guard, FormationGuardEffect stone)
        {
            stone.Kind = "chess_hit";
            stone.StartedAt = stone.ImpactAt = Clock;
            stone.EndsAt = Clock + WangAfterglow;
            foreach (var unit in GuardVictims(stone.Origin, stone.Radius, 3))
            {
                CombatDamage(guard, unit, guard.Attack * 1.8f, 1, true, false);
                if (unit.Alive) GiveBuff(unit, "guard_chess_slow", .25f, 2);
            }
        }

        FormationFighter[] MlynarTargets(FormationDefender guard, Vector2 facing) => GuardTargets()
            .Where(unit =>
            {
                var offset = unit.Position - guard.Position;
                int row = Mathf.Abs(Mathf.RoundToInt(Vector2.Dot(offset, new Vector2(-facing.y, facing.x))));
                int col = Mathf.RoundToInt(Vector2.Dot(offset, facing));
                return row <= 2 && col >= 0 && col <= 3 - row;
            })
            .OrderBy(unit => (unit.Position - guard.Position).sqrMagnitude).ThenBy(unit => unit.Id).Take(5).ToArray();

        void TickGuardMlynar(FormationDefender guard, GuardKit kit)
        {
            if (Buff(guard, "stun") > 0 || Buff(guard, "freeze") > 0 || Clock + 1e-7 < guard.ActionUntil) return;
            if (GuardSkillActive(guard))
            {
                Vector2 facing = kit.Effect.Direction;
                if (Clock + 1e-7 < guard.NextAttackAt || Clock + MlynarSlashTime / 2 >= kit.ActiveUntil) return;
                var targets = MlynarTargets(guard, facing);
                if (targets.Length == 0) return;
                guard.TargetId = targets[0].Id;
                var swing = BeginGuardAction(guard, true, MlynarInterval / 2, targets[0], motion: "skill_attack");
                swing.To = swing.From + facing;
                swing.LaunchedAt = swing.ContactAt = swing.At + MlynarSlashTime / swing.Rate;
                guard.NextAttackAt = Clock + MlynarInterval / Mathf.Max(.1f,
                    1 - (Buff(guard, "cold") > 0 ? .2f : 0) - Buff(guard, "nerve_slow"));
                GuardContact(guard, swing, () =>
                {
                    foreach (var victim in MlynarTargets(guard, facing))
                        CombatDamage(guard, victim, guard.Attack * 1.5f, 0, true, false);
                });
                return;
            }

            var threat = MlynarTargets(guard, guard.Facing).FirstOrDefault();
            if (threat == null || guard.SkillCharge + 1e-5 < guard.SkillRequired) return;
            StartGuardCast(guard, kit, threat, 10d / 60);
            kit.ActiveUntil = Clock + MlynarSkillLength;
            guard.NextAttackAt = Clock;
            kit.Effect = new FormationGuardEffect
            {
                Kind = "mlynar", Origin = guard.Position, Direction = guard.Facing,
                StartedAt = Clock, EndsAt = kit.ActiveUntil
            };
            ScheduleOwned(guard, kit.ActiveUntil, () => BeginGuardAction(guard, true, 10d / 60, motion: "skill_end"));
        }
    }
}
