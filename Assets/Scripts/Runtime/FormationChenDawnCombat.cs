using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleModel
    {
        const float ChenAttackSpeed = 1.13f;
        internal const float ChenAttackInterval = 1.25f / ChenAttackSpeed;
        internal const float ChenSkillSeconds = 20;
        const double ChenAttackDuration = 37d / 30;
        const double ChenAttackContact = 13d / 30;
        const double ChenSkillBegin = 13d / 30;
        const double ChenSkillEnd = 8d / 30;
        const float ChenWaveSpeed = 1.5f;
        const float ChenWaveClearance = .25f;
        const double ChenWaveSample = .03;

        internal readonly struct ChenWavePoint
        {
            internal readonly Vector2 Position, Direction;
            internal readonly double At;
            internal readonly int Turn;

            internal ChenWavePoint(Vector2 position, Vector2 direction, double at, int turn)
            { Position = position; Direction = direction; At = at; Turn = turn; }
        }

        internal sealed class ChenDawnCast
        {
            internal FormationPresentationAction Action;
            internal double Until, NextWaveSample;
            internal int LastWaveTurn;
            internal float WaveAttack;
            internal bool Ended;
            internal readonly List<ChenWavePoint> Path = new List<ChenWavePoint>();
            internal readonly HashSet<int> WaveVictims = new HashSet<int>();

            internal ChenWavePoint WaveAt(double at)
            {
                int low = 0, high = Path.Count;
                while (low + 1 < high)
                {
                    int middle = (low + high) / 2;
                    if (Path[middle].At <= at) low = middle;
                    else high = middle;
                }
                var a = Path[low];
                if (high == Path.Count) return a;
                var b = Path[high];
                return new ChenWavePoint(Vector2.Lerp(a.Position, b.Position, (float)((at - a.At) / (b.At - a.At))),
                    a.Direction, at, a.Turn);
            }
        }

        internal ChenDawnCast GuardChenDawnCast(FormationDefender guard) =>
            guardKits.TryGetValue(guard.Id, out var kit) && kit.Guard == guard ? kit.ChenDawn : null;

        internal static Vector2 ChenCardinal(Vector2 direction) => Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)
            ? new Vector2(direction.x <= 0 ? -1 : 1, 0) : new Vector2(0, direction.y < 0 ? -1 : 1);

        bool ChenCanAttack(FormationDefender guard, FormationFighter target, Vector2 facing, bool skill)
        {
            Vector2 offset = target.Position - guard.Position;
            Vector2 forward = skill ? facing : ChenCardinal(offset);
            float column = Vector2.Dot(offset, forward);
            float row = Mathf.Abs(Vector2.Dot(offset, new Vector2(forward.y, -forward.x)));
            // S3's 3-12 source shape has two outer rows and four squares in the centre row.
            return column >= -.5f && (skill ? row <= 1.5f && column <= 1.5f || row <= .5f && column <= 3.5f
                : row <= .5f && column <= 1.5f);
        }

        void TickChenDawn(FormationDefender guard, GuardKit kit)
        {
            var cast = kit.ChenDawn;
            if (cast != null) TickChenDawnWave(guard, cast);
            if (!guard.Alive || Buff(guard, "stun") > 0 || Buff(guard, "freeze") > 0 || Clock < guard.ActionUntil) return;
            bool active = GuardSkillActive(guard);
            if (cast != null && !active && !cast.Ended)
            {
                cast.Ended = true;
                BeginChenAction(guard, "skill_end", ChenSkillEnd, 0, 1);
                return;
            }

            var targets = GuardTargets().Where(unit => State(unit).RidingCarrier == 0)
                .OrderByDescending(unit => Buff(unit, "taunt") > 0)
                .ThenBy(unit => (unit.Position - guard.Position).sqrMagnitude).ThenBy(unit => unit.Id).ToArray();
            if (!active && guard.SkillCharge + 1e-5 >= guard.SkillRequired)
            {
                var target = targets.FirstOrDefault(unit => ChenCanAttack(guard, unit,
                    ChenCardinal(unit.Position - guard.Position), true));
                if (target != null)
                {
                    guard.Facing = ChenCardinal(target.Position - guard.Position);
                    var action = BeginChenAction(guard, "skill_begin", ChenSkillBegin, ChenSkillBegin, 1, target);
                    guard.SkillCharge = 0;
                    kit.Casts++;
                    kit.LastCast = Clock;
                    kit.ActiveUntil = Clock + ChenSkillSeconds;
                    guard.NextAttackAt = action.EndAt;
                    kit.ChenDawn = new ChenDawnCast { Action = action, Until = kit.ActiveUntil,
                        NextWaveSample = action.LaunchedAt, WaveAttack = guard.Attack * ChenAttackSpeed };
                    BuildChenWave(kit.ChenDawn, guard.Facing);
                    return;
                }
            }

            if (Clock + 1e-8 < guard.NextAttackAt) return;
            var victims = targets.Where(unit => ChenCanAttack(guard, unit, guard.Facing, active)).Take(active ? 3 : 1)
                .Select(unit => (Unit: unit, Life: unit.Deaths)).ToArray();
            if (victims.Length == 0) { guard.TargetId = -1; return; }
            var primary = victims[0].Unit;
            guard.TargetId = primary.Id;
            if (!active) guard.Facing = ChenCardinal(primary.Position - guard.Position);
            float speed = Mathf.Max(.1f, 1 - (Buff(guard, "cold") > 0 ? .2f : 0) - Buff(guard, "nerve_slow"));
            var attack = BeginChenAction(guard, active ? "skill_attack" : "attack", ChenAttackDuration,
                active ? 16d / 30 : ChenAttackContact, ChenAttackSpeed * speed, primary);
            guard.NextAttackAt = Clock + guard.Interval / speed;
            float raw = guard.Attack * ChenAttackSpeed * (active ? 1.8f : 1);
            var context = CaptureActionContext(State(guard));
            context.Presentation = attack;
            for (int hit = 0; hit < (active ? 3 : 1); hit++)
            {
                int beat = hit;
                Schedule(attack.ContactAt + beat * (2d / 30) / attack.Rate, () =>
                {
                    if (!guard.Alive || attack.Cancelled) return;
                    foreach (var victim in victims)
                        if (victim.Unit.Alive && victim.Unit.Enabled && victim.Unit.Deaths == victim.Life)
                            ChenDamage(guard, victim.Unit, raw, active, beat, context);
                });
            }
        }

        FormationPresentationAction BeginChenAction(FormationDefender guard, string motion, double sourceDuration,
            double sourceContact, float attackSpeed, FormationFighter target = null)
        {
            float rate = FormationBattleController.FormationVisualRate * attackSpeed;
            double duration = sourceDuration / rate;
            var action = BeginGuardAction(guard, motion != "attack", duration, target, motion: motion);
            action.Rate = guard.PresentationRate = rate;
            guard.PresentationDuration = duration;
            action.EndAt = action.At + duration;
            action.LaunchedAt = action.ContactAt = action.At + sourceContact / rate;
            return action;
        }

        void ChenDamage(FormationDefender guard, FormationFighter target, float raw, bool skill, int beat,
            AutoActionContext context)
        {
            RunInPresentationContext(guard, context, () =>
            {
                var state = State(guard);
                int actionHits = state.ActionHits;
                state.ActionHits = beat + 1;
                try
                {
                    float physical = DamageAfterDefense(raw, 0, target.Armor, target.MagicResistance);
                    float arts = DamageAfterDefense(raw, 1, target.Armor, target.MagicResistance);
                    CombatDamage(guard, target, raw, physical > arts ? 0 : 1, skill, false);
                }
                finally { state.ActionHits = actionHits; }
            });
        }

        void TickChenDawnWave(FormationDefender guard, ChenDawnCast cast)
        {
            while (guard.Alive && cast.NextWaveSample <= Clock + 1e-8 && cast.NextWaveSample < cast.Until)
            {
                var point = cast.WaveAt(cast.NextWaveSample);
                cast.NextWaveSample += ChenWaveSample;
                if (point.Turn != cast.LastWaveTurn)
                {
                    cast.LastWaveTurn = point.Turn;
                    cast.WaveVictims.Clear();
                }
                foreach (var target in GuardVictims(point.Position, 1.3f, int.MaxValue))
                {
                    if (!guard.Alive) break;
                    if (!cast.WaveVictims.Add(target.Id)) continue;
                    var context = CaptureActionContext(State(guard));
                    context.Presentation = cast.Action;
                    ChenDamage(guard, target, Mathf.Max(cast.WaveAttack * 5.5f, target.Health * .06f), true, 3, context);
                }
            }
        }

        void BuildChenWave(ChenDawnCast cast, Vector2 direction)
        {
            var position = cast.Action.From;
            double at = cast.Action.LaunchedAt;
            int turn = 0;
            cast.Path.Add(new ChenWavePoint(position, direction, at, turn));
            while (at < cast.Until - 1e-7)
            {
                double next = Math.Min(cast.Until, at + ChenWaveSample);
                float distance = (float)(next - at) * ChenWaveSpeed;
                float clearance = ChenTravelBeforeTurn(position, direction);
                position += direction * Mathf.Min(distance, clearance);
                if (clearance < distance - 1e-6f)
                {
                    var right = new Vector2(direction.y, -direction.x);
                    if (ChenTravelBeforeTurn(position, right) > 1e-6f || ChenTravelBeforeTurn(position, -direction) > 1e-6f ||
                        ChenTravelBeforeTurn(position, -right) > 1e-6f)
                    {
                        double turnAt = at + clearance / ChenWaveSpeed;
                        cast.Path.Add(new ChenWavePoint(position, direction, turnAt, turn));
                        direction = right;
                        cast.Path.Add(new ChenWavePoint(position, direction, turnAt, ++turn));
                        position += direction * Mathf.Min(distance - clearance, ChenTravelBeforeTurn(position, direction));
                    }
                }
                at = next;
                cast.Path.Add(new ChenWavePoint(position, direction, at, turn));
            }
        }

        float ChenTravelBeforeTurn(Vector2 position, Vector2 direction)
        {
            bool horizontal = direction.x != 0;
            float coordinate = horizontal ? position.x : position.y;
            float side = horizontal ? position.y : position.x;
            bool positive = (horizontal ? direction.x : direction.y) > 0;
            float low = horizontal ? BattleAreaStart : -.5f;
            float high = (horizontal ? Width : Height) - .5f;
            float travel = positive ? high - coordinate - ChenWaveClearance : coordinate - low - ChenWaveClearance;
            if (Terrain != null)
                foreach (var region in Terrain.Regions)
                {
                    RectInt cells = region.cells;
                    int sideMin = horizontal ? cells.yMin : cells.xMin;
                    int sideMax = horizontal ? cells.yMax : cells.xMax;
                    if (side <= sideMin - .5f || side >= sideMax - .5f) continue;
                    low = (horizontal ? cells.xMin : cells.yMin) - .5f;
                    high = (horizontal ? cells.xMax : cells.yMax) - .5f;
                    if (positive ? coordinate >= high : coordinate <= low) continue;
                    travel = Mathf.Min(travel, positive ? low - coordinate - ChenWaveClearance : coordinate - high - ChenWaveClearance);
                }
            return Mathf.Max(0, travel);
        }
    }
}
