using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleModel
    {
        const int WangPiecesPerCast = 5;
        const double MlynarInterval = 1.2;
        const double MlynarSkillLength = 26;
        const double MlynarSlashTime = 32d / 60;

        internal FormationGuardEffect[] GuardChessPieces(FormationDefender guard) =>
            guardKits.TryGetValue(guard.Id, out var kit) && kit.Guard == guard && guard.Alive
                ? kit.ChessPieces ?? Array.Empty<FormationGuardEffect>() : Array.Empty<FormationGuardEffect>();

        void PlaceGuardChess(FormationDefender guard, GuardKit kit)
        {
            Vector2Int anchor = guard.SkillCell.Value;
            var cells = (from x in Enumerable.Range(DeploymentColumns + 2, Width - DeploymentColumns - 3)
                         from y in Enumerable.Range(0, Height)
                         let cell = new Vector2Int(x, y)
                         where (cell - anchor).sqrMagnitude <= 9 &&
                             (Terrain == null || (Terrain.CanPlaceDefender(cell) && Terrain.IsWalkable(cell) && Terrain.HeightAt(cell) == 0)) &&
                             !Defenders.Any(other => other.Alive && (other.Position - (Vector2)cell).sqrMagnitude < .01f)
                         orderby (cell - anchor).sqrMagnitude, cell.y, cell.x
                         select cell).Take(WangPiecesPerCast).ToArray();
            if (cells.Length != WangPiecesPerCast) return;

            StartGuardCast(guard, kit, null, 5d / 60);
            kit.ActiveUntil = double.PositiveInfinity;
            kit.Effect = new FormationGuardEffect
            {
                Kind = "chess_board", Origin = anchor, PlacedAt = Clock, StartedAt = Clock,
                EndsAt = double.PositiveInfinity
            };
            kit.ChessPieces = cells.Select((cell, index) => new FormationGuardEffect
            {
                Kind = "chess", Origin = cell, Radius = 1.4f, PlacedAt = Clock, StartedAt = Clock,
                ImpactAt = Clock + (index == 0 ? .1 : .25), EndsAt = double.PositiveInfinity
            }).ToArray();
        }

        void TickGuardChess(FormationDefender guard, GuardKit kit)
        {
            if (GuardEffect(guard) == null)
            {
                if (Clock >= kit.NextPlacement && GuardSkillCellIsFree(guard)) PlaceGuardChess(guard, kit);
                return;
            }

            bool resolved = false;
            foreach (var stone in kit.ChessPieces)
            {
                if (stone.Kind != "chess" || Clock < stone.ImpactAt ||
                    !GuardTargets().Any(unit => GuardInRange(stone.Origin, unit, .8f))) continue;
                stone.Kind = "chess_hit";
                stone.StartedAt = stone.ImpactAt = Clock;
                stone.EndsAt = Clock + .55;
                resolved = true;
                foreach (var unit in GuardVictims(stone.Origin, stone.Radius, 3))
                {
                    CombatDamage(guard, unit, guard.Attack * 1.8f, 1, true, false);
                    if (unit.Alive) GiveBuff(unit, "guard_chess_slow", .25f, 2);
                }
            }
            if (!resolved || kit.ChessPieces.Any(stone => stone.Kind == "chess")) return;
            kit.ActiveUntil = kit.Effect.EndsAt = Clock + .55;
            kit.NextPlacement = kit.ActiveUntil + 24;
            ScheduleOwned(guard, kit.ActiveUntil, () => BeginGuardAction(guard, true, 8d / 60, motion: "skill_end"));
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
