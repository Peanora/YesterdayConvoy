using System;
using System.Collections.Generic;
using UnityEngine;

namespace YesterdayConvoy
{
    public enum FormationPhase { Preparation, Fighting, Exiting, Won, Lost }

    // This isolated prototype intentionally uses simplified skills, not the original character kits.
    public class FormationActor
    {
        public int Id, Kind;
        public string Name, ArtId;
        public Vector2 Position, Facing;
        public float Health, MaxHealth, Attack, Armor, Range, Interval, MoveSpeed;
        public float SkillCharge, SkillRequired, DamageDone, HealingDone;
        public double LastActionAt = -100, LastSkillAt = -100, LastHitAt = -100, LastDeathAt = -100;
        public bool Alive, IsMoving;
        public int TargetId = -1;
        internal double NextAttackAt, ActionUntil, BuffUntil;
    }

    public sealed class FormationFighter : FormationActor
    {
        public int Slot, Star = 1, Deaths;
        public bool Enabled;
        public Vector2 Spawn;
        public float RespawnRemaining;
        internal double RebornAt;
    }

    public sealed class FormationDefender : FormationActor
    {
        public bool Healer;
    }

    public sealed class FormationHit
    {
        public double At;
        public Vector2 From, To;
        public int FromId, ToId;
        public float Amount;
        public bool Heal, Skill, Killed;
    }

    /// <summary>
    /// Pure combat state for the formation experiment. No scene, asset, input, or wall-clock access.
    /// Cells are centred on integer coordinates. Only preparation accepts roster mutations.
    /// </summary>
    public sealed class FormationBattleModel
    {
        public const int Width = 14, Height = 8, DeploymentColumns = 6;
        const double StepSeconds = .025;
        const float PersonalSpace = .48f;
        static readonly string[] FighterNames = { "重装防御者", "猎狗", "士兵", "弩手", "术师", "游击队传令兵" };
        static readonly string[] FighterArt = { "M10", "M02", "M07", "M08", "M09", "M17" };
        static readonly string[] DefenderNames = { "米格鲁", "卡缇", "斑点", "玫兰莎", "月见夜", "翎羽", "克洛丝", "史都华德", "炎熔", "芙蓉", "安赛尔", "梓兰" };
        static readonly int[] RoundCounts = { 4, 6, 9, 12, 16 };

        public readonly List<FormationFighter> Units = new List<FormationFighter>();
        public readonly List<FormationDefender> Defenders = new List<FormationDefender>();
        public readonly List<FormationHit> Hits = new List<FormationHit>();
        public FormationPhase Phase { get; private set; }
        public int Round { get; private set; }
        public int Seed { get; private set; }
        public int Capacity { get; private set; }
        public double Clock { get; private set; }
        public double LimitSeconds = 90, ReviveSeconds = 8;
        public double TimeRemaining => Math.Max(0, LimitSeconds - Clock);
        public bool Paused;
        public float Speed = 1;
        public int EnabledCount
        {
            get { int count = 0; foreach (var unit in Units) if (unit.Enabled) count++; return count; }
        }
        public int RemainingDefenders
        {
            get { int count = 0; foreach (var defender in Defenders) if (defender.Alive) count++; return count; }
        }

        double accumulator;

        public FormationBattleModel(int seed = 20261117) { Reset(seed); }

        public void Reset(int seed)
        {
            Seed = seed; Round = 1; Capacity = 4; Speed = 1; Paused = false;
            Units.Clear();
            Vector2[] positions = { new Vector2(5, 2), new Vector2(4, 5), new Vector2(4, 3), new Vector2(2, 3), new Vector2(2, 5), new Vector2(1, 2) };
            for (int i = 0; i < 6; i++)
                Units.Add(new FormationFighter { Id = i, Slot = i, Kind = i, Name = FighterNames[i], ArtId = FighterArt[i], Enabled = i < 4, Spawn = positions[i] });
            PrepareRound();
        }

        public bool TryPlace(int slot, int col, int row)
        {
            if (Phase != FormationPhase.Preparation || col < 0 || col >= DeploymentColumns || row < 0 || row >= Height) return false;
            var unit = FindSlot(slot);
            if (unit == null || !unit.Enabled) return false;
            var destination = new Vector2(col, row);
            FormationFighter occupant = null;
            foreach (var other in Units)
                if (other != unit && other.Enabled && other.Spawn == destination) { occupant = other; break; }
            if (occupant != null) { occupant.Spawn = unit.Spawn; occupant.Position = occupant.Spawn; }
            unit.Spawn = destination; unit.Position = destination;
            return true;
        }

        public bool ToggleSlot(int slot)
        {
            if (Phase != FormationPhase.Preparation) return false;
            var unit = FindSlot(slot);
            if (unit == null || unit.Enabled && EnabledCount <= 1 || !unit.Enabled && EnabledCount >= Capacity) return false;
            if (!unit.Enabled)
            {
                Vector2 position = unit.Spawn;
                if (CellOccupied(position, slot))
                {
                    bool found = false;
                    for (int x = DeploymentColumns - 1; x >= 0 && !found; x--)
                        for (int y = 0; y < Height; y++)
                            if (!CellOccupied(new Vector2(x, y), slot)) { position = new Vector2(x, y); found = true; break; }
                    if (!found) return false;
                }
                unit.Spawn = position;
            }
            unit.Enabled = !unit.Enabled;
            ResetFighter(unit);
            return true;
        }

        public bool Upgrade(int slot)
        {
            if (Phase != FormationPhase.Preparation) return false;
            var unit = FindSlot(slot);
            if (unit == null || unit.Star >= 3) return false;
            unit.Star++; ConfigureFighter(unit); ResetFighter(unit);
            return true;
        }

        public bool Expand()
        {
            if (Phase != FormationPhase.Preparation || Capacity >= 6) return false;
            Capacity++; return true;
        }

        public bool StartBattle()
        {
            if (Phase != FormationPhase.Preparation || EnabledCount == 0) return false;
            Clock = 0; accumulator = 0; Hits.Clear(); Paused = false;
            foreach (var unit in Units) ResetFighter(unit);
            foreach (var defender in Defenders) ResetActor(defender, true, Vector2.left);
            Phase = FormationPhase.Fighting;
            return true;
        }

        public bool Retry()
        {
            PrepareRound(); return true;
        }

        public bool NextRound()
        {
            if (Phase != FormationPhase.Won || Round >= RoundCounts.Length) return false;
            Round++; PrepareRound(); return true;
        }

        public void Advance(float realDt)
        {
            if (Paused || !Running || float.IsNaN(realDt) || float.IsInfinity(realDt) || realDt <= 0 || float.IsNaN(Speed) || float.IsInfinity(Speed) || Speed <= 0) return;
            accumulator += realDt * (double)Mathf.Clamp(Speed, 0, 2);
            while (Running)
            {
                double remaining = LimitSeconds - Clock;
                if (remaining <= 1e-8) { FinishIfAtGate(); if (Running) Phase = FormationPhase.Lost; break; }
                double step = Math.Min(StepSeconds, remaining);
                // Inputs arrive as floats; decimal frame batches such as 7.95f can miss a tick by <1us.
                if (accumulator + 1e-6 < step) break;
                accumulator = Math.Max(0, accumulator - step);
                Clock += step;
                Tick((float)step);
                FinishIfAtGate();
                if (Running && Clock + 1e-8 >= LimitSeconds) Phase = FormationPhase.Lost;
            }
        }

        bool Running => Phase == FormationPhase.Fighting || Phase == FormationPhase.Exiting;

        void PrepareRound()
        {
            Clock = 0; accumulator = 0; Hits.Clear(); Paused = false;
            foreach (var unit in Units) { ConfigureFighter(unit); ResetFighter(unit); }
            BuildDefenders(); Phase = FormationPhase.Preparation;
        }

        void ConfigureFighter(FormationFighter unit)
        {
            float[] hp = { 1100, 360, 680, 420, 380, 560 };
            float[] attack = { 58, 72, 88, 66, 80, 36 };
            float[] armor = { 20, 2, 9, 4, 3, 8 };
            float[] range = { .85f, .8f, .85f, 4.1f, 3.3f, 3.4f };
            float[] interval = { 1.2f, .7f, 1, 1.1f, 1.6f, 1.4f };
            float[] move = { 1.1f, 1.8f, 1.25f, 1.2f, 1.05f, 1.0f };
            float[] skill = { 8, 6, 8, 8, 10, 8 };
            float multiplier = unit.Star == 3 ? 2.6f : unit.Star == 2 ? 1.65f : 1;
            int k = unit.Kind;
            unit.MaxHealth = hp[k] * multiplier; unit.Attack = attack[k] * multiplier;
            unit.Armor = armor[k] + (unit.Star - 1) * 4; unit.Range = range[k];
            unit.Interval = interval[k]; unit.MoveSpeed = move[k]; unit.SkillRequired = skill[k];
        }

        void BuildDefenders()
        {
            Defenders.Clear();
            var rng = new FormationRandom(unchecked((uint)Seed ^ ((uint)Round * 0x9e3779b9u)));
            var cells = new List<Vector2>();
            for (int x = 8; x <= 12; x++) for (int y = 0; y < Height; y++) cells.Add(new Vector2(x, y));
            for (int i = cells.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var temp = cells[i]; cells[i] = cells[j]; cells[j] = temp; }
            float healthScale = 1 + (Round - 1) * .26f, attackScale = 1 + (Round - 1) * .14f;
            for (int i = 0; i < RoundCounts[Round - 1]; i++)
            {
                // Most are ordinary melee/marksman guards. At most two medics prevents unbounded sustain.
                int kind = i == 5 || i == 12 ? 9 + rng.Next(2) : new[] { 0, 1, 3, 4, 5, 6, 7, 8, 11 }[rng.Next(9)];
                bool healer = kind == 9 || kind == 10;
                bool ranged = kind >= 6;
                bool elite = Round >= 3 && i % 6 == 4;
                float hp = (kind <= 1 ? 620 : ranged ? 350 : 430) * healthScale * (elite ? 1.3f : 1);
                Defenders.Add(new FormationDefender
                {
                    Id = 100 + i, Kind = kind, Name = DefenderNames[kind] + (elite ? "·精英" : ""), ArtId = "D" + (kind + 1).ToString("00"),
                    Position = cells[i], MaxHealth = hp, Attack = (healer ? 32 : ranged ? 42 : 48) * attackScale * (elite ? 1.15f : 1),
                    Armor = (kind <= 1 ? 18 : 5) + Round * 2, Range = ranged ? 3.7f : 1.05f,
                    Interval = healer ? 2.4f : kind == 6 ? 1.0f : 1.3f, SkillRequired = 10 + i % 4, Healer = healer
                });
            }
            foreach (var defender in Defenders) ResetActor(defender, true, Vector2.left);
        }

        static void ResetActor(FormationActor actor, bool alive, Vector2 facing)
        {
            actor.Health = alive ? actor.MaxHealth : 0; actor.Alive = alive; actor.IsMoving = false; actor.Facing = facing;
            actor.SkillCharge = 0; actor.DamageDone = 0; actor.HealingDone = 0; actor.TargetId = -1;
            actor.LastActionAt = actor.LastSkillAt = actor.LastHitAt = actor.LastDeathAt = -100;
            actor.NextAttackAt = actor.ActionUntil = actor.BuffUntil = 0;
        }

        static void ResetFighter(FormationFighter unit)
        {
            unit.Position = unit.Spawn; unit.Deaths = 0; unit.RespawnRemaining = 0; unit.RebornAt = 0;
            ResetActor(unit, unit.Enabled, Vector2.right);
        }

        void Tick(float dt)
        {
            Hits.RemoveAll(hit => hit.At < Clock - 3);
            foreach (var unit in Units)
            {
                unit.IsMoving = false;
                if (!unit.Enabled) continue;
                if (!unit.Alive)
                {
                    unit.RespawnRemaining = (float)Math.Max(0, unit.RebornAt - Clock);
                    if (Clock + 1e-8 >= unit.RebornAt)
                    {
                        unit.Alive = true; unit.Health = unit.MaxHealth; unit.Position = unit.Spawn;
                        unit.ActionUntil = Clock + .2; unit.NextAttackAt = Math.Max(unit.NextAttackAt, Clock + .2);
                        unit.TargetId = -1; unit.Facing = Vector2.right;
                        // Charge is preserved on death and does not grow during the respawn wait.
                    }
                    continue;
                }
                unit.SkillCharge = Mathf.Min(unit.SkillRequired, unit.SkillCharge + dt);
            }
            if (Phase == FormationPhase.Fighting)
            {
                foreach (var unit in Units) if (unit.Enabled && unit.Alive) TickFighter(unit, dt);
                foreach (var defender in Defenders) if (defender.Alive) TickDefender(defender, dt);
                if (RemainingDefenders == 0)
                {
                    Phase = FormationPhase.Exiting;
                    foreach (var unit in Units) unit.TargetId = -1;
                }
            }
            if (Phase == FormationPhase.Exiting)
            {
                foreach (var unit in Units)
                    if (unit.Enabled && unit.Alive && Clock >= unit.ActionUntil)
                        Move(unit, new Vector2(Width - 1, unit.Position.y), dt, false);
            }
        }

        void TickFighter(FormationFighter unit, float dt)
        {
            var target = FighterTarget(unit);
            unit.TargetId = target != null ? target.Id : -1;
            if (target == null || Clock < unit.ActionUntil) return;
            float distance = Vector2.Distance(unit.Position, target.Position);
            if (distance > unit.Range)
            {
                Move(unit, target.Position, dt, true); return;
            }
            Face(unit, target.Position);
            if (unit.SkillCharge + 1e-5f >= unit.SkillRequired && TryFighterSkill(unit, target)) return;
            if (Clock + 1e-8 < unit.NextAttackAt) return;
            unit.LastActionAt = Clock; unit.ActionUntil = Clock + Math.Min(.36, unit.Interval * .45);
            unit.NextAttackAt = Clock + unit.Interval;
            float multiplier = unit.Kind == 2 && unit.BuffUntil > Clock ? 1.6f : 1;
            Damage(unit, target, unit.Attack * multiplier, unit.Kind == 4, false);
        }

        FormationDefender FighterTarget(FormationFighter unit)
        {
            FormationDefender nearest = null; float best = float.MaxValue;
            foreach (var defender in Defenders)
            {
                if (!defender.Alive) continue;
                float d = (defender.Position - unit.Position).sqrMagnitude;
                if (d < best - 1e-5f || Mathf.Abs(d - best) < 1e-5f && (nearest == null || defender.Id < nearest.Id)) { nearest = defender; best = d; }
            }
            return nearest;
        }

        bool TryFighterSkill(FormationFighter unit, FormationDefender target)
        {
            if (unit.Kind == 5)
            {
                FormationFighter wounded = MostWoundedFighter(unit.Position, 4.5f);
                if (wounded == null || wounded.Health > wounded.MaxHealth * .85f) return false;
                UseSkill(unit, .65); Heal(unit, wounded, wounded.MaxHealth * .32f, true); return true;
            }
            UseSkill(unit, unit.Kind == 0 ? .3 : .65);
            switch (unit.Kind)
            {
                case 0: unit.BuffUntil = Clock + 4; break;
                case 1:
                    Damage(unit, target, unit.Attack * 1.45f, false, true);
                    if (target.Alive) Damage(unit, target, unit.Attack * 1.45f, false, true);
                    break;
                case 2:
                    unit.BuffUntil = Clock + 4;
                    Damage(unit, target, unit.Attack * 1.6f, false, true); break;
                case 3:
                    for (int i = 0; i < 3 && target.Alive; i++) Damage(unit, target, unit.Attack * 1.15f, false, true);
                    break;
                case 4:
                    Vector2 centre = target.Position;
                    foreach (var defender in Defenders)
                        if (defender.Alive && (defender.Position - centre).sqrMagnitude <= 1.5f * 1.5f)
                            Damage(unit, defender, unit.Attack * 2.2f, true, true);
                    break;
            }
            return true;
        }

        void UseSkill(FormationActor actor, double duration)
        {
            actor.SkillCharge = Mathf.Max(0, actor.SkillCharge - actor.SkillRequired);
            actor.LastSkillAt = actor.LastActionAt = Clock; actor.ActionUntil = Clock + duration;
            actor.NextAttackAt = Math.Max(actor.NextAttackAt, Clock + duration);
        }

        void TickDefender(FormationDefender defender, float dt)
        {
            defender.IsMoving = false;
            defender.SkillCharge = Mathf.Min(defender.SkillRequired, defender.SkillCharge + dt);
            if (Clock < defender.ActionUntil || Clock + 1e-8 < defender.NextAttackAt) return;
            if (defender.Healer)
            {
                FormationDefender wounded = null; float proportion = .96f;
                foreach (var other in Defenders)
                    if (other.Alive && Vector2.Distance(defender.Position, other.Position) <= defender.Range && other.Health / other.MaxHealth < proportion)
                    { wounded = other; proportion = other.Health / other.MaxHealth; }
                if (wounded == null) return;
                bool skill = defender.SkillCharge + 1e-5f >= defender.SkillRequired;
                if (skill) UseSkill(defender, .5);
                defender.LastActionAt = Clock; defender.ActionUntil = Clock + .4; defender.NextAttackAt = Clock + defender.Interval;
                Face(defender, wounded.Position); Heal(defender, wounded, defender.Attack * (skill ? 2 : 1), skill); return;
            }
            FormationFighter target = null;
            // Keep a legal target to make a front liner's initial contact protect the rear line.
            foreach (var unit in Units)
                if (unit.Id == defender.TargetId && LegalTarget(defender, unit)) { target = unit; break; }
            if (target == null)
            {
                float best = float.MaxValue;
                foreach (var unit in Units)
                {
                    if (!LegalTarget(defender, unit)) continue;
                    float d = (unit.Position - defender.Position).sqrMagnitude;
                    if (d < best - 1e-5f || Mathf.Abs(d - best) < 1e-5f && (target == null || unit.Id < target.Id)) { target = unit; best = d; }
                }
            }
            defender.TargetId = target != null ? target.Id : -1;
            if (target == null) return;
            bool empowered = defender.SkillCharge + 1e-5f >= defender.SkillRequired;
            if (empowered) UseSkill(defender, .5);
            defender.LastActionAt = Clock; defender.ActionUntil = Clock + .35; defender.NextAttackAt = Clock + defender.Interval;
            Face(defender, target.Position);
            bool arts = defender.Kind == 4 || defender.Kind == 7 || defender.Kind == 8 || defender.Kind == 11;
            if (defender.Kind == 8)
            {
                Vector2 centre = target.Position;
                foreach (var unit in Units)
                    if (unit.Enabled && unit.Alive && (unit.Position - centre).sqrMagnitude <= 1.2f * 1.2f)
                        Damage(defender, unit, defender.Attack * (empowered ? 1.6f : 1), true, empowered);
            }
            else Damage(defender, target, defender.Attack * (empowered ? 1.6f : 1), arts, empowered);
        }

        static bool LegalTarget(FormationDefender source, FormationFighter target) => target.Enabled && target.Alive && (source.Position - target.Position).sqrMagnitude <= source.Range * source.Range;

        FormationFighter MostWoundedFighter(Vector2 centre, float range)
        {
            FormationFighter result = null; float proportion = 1;
            foreach (var unit in Units)
                if (unit.Enabled && unit.Alive && Vector2.Distance(centre, unit.Position) <= range && unit.Health / unit.MaxHealth < proportion)
                { result = unit; proportion = unit.Health / unit.MaxHealth; }
            return result;
        }

        void Damage(FormationActor source, FormationActor target, float raw, bool arts, bool skill)
        {
            if (!source.Alive || !target.Alive) return;
            float reduction = 1;
            var fighter = target as FormationFighter;
            if (fighter != null && fighter.Kind == 0 && fighter.BuffUntil > Clock) reduction = .45f;
            float amount = Mathf.Min(target.Health, Mathf.Max(1, raw - target.Armor * (arts ? .2f : 1)) * reduction);
            target.Health = Mathf.Max(0, target.Health - amount); source.DamageDone += amount; target.LastHitAt = Clock;
            bool killed = target.Health <= 0;
            if (killed)
            {
                target.Alive = false; target.IsMoving = false; target.LastDeathAt = Clock; target.TargetId = -1; target.BuffUntil = 0;
                if (fighter != null)
                {
                    fighter.Deaths++; fighter.RebornAt = Clock + Math.Max(0, ReviveSeconds);
                    fighter.RespawnRemaining = (float)Math.Max(0, ReviveSeconds);
                }
            }
            Hits.Add(new FormationHit { At = Clock, From = source.Position, To = target.Position, FromId = source.Id, ToId = target.Id, Amount = amount, Heal = false, Skill = skill, Killed = killed });
        }

        void Heal(FormationActor source, FormationActor target, float raw, bool skill)
        {
            if (!source.Alive || !target.Alive) return;
            float amount = Mathf.Min(raw, target.MaxHealth - target.Health);
            if (amount <= 0) return;
            target.Health += amount; source.HealingDone += amount;
            Hits.Add(new FormationHit { At = Clock, From = source.Position, To = target.Position, FromId = source.Id, ToId = target.Id, Amount = amount, Heal = true, Skill = skill });
        }

        void Move(FormationFighter unit, Vector2 goal, float dt, bool avoid)
        {
            Vector2 delta = goal - unit.Position;
            if (delta.sqrMagnitude < .000001f) return;
            Vector2 direction = delta.normalized;
            if (avoid)
            {
                Vector2 separation = Vector2.zero;
                foreach (var other in Units)
                {
                    if (other == unit || !other.Enabled || !other.Alive) continue;
                    Vector2 apart = unit.Position - other.Position; float distance = apart.magnitude;
                    if (distance >= PersonalSpace) continue;
                    if (distance < .001f) apart = new Vector2(0, unit.Id < other.Id ? -1 : 1);
                    else apart /= distance;
                    separation += apart * ((PersonalSpace - distance) / PersonalSpace);
                }
                // Local steering only: it creates readable spacing without a new navigation system.
                direction = (direction + separation * 1.6f).normalized;
            }
            Vector2 before = unit.Position;
            float travel = Mathf.Min(unit.MoveSpeed * dt, delta.magnitude);
            Vector2 next = before + direction * travel;
            next.x = Mathf.Clamp(next.x, 0, Width - 1); next.y = Mathf.Clamp(next.y, 0, Height - 1);
            if (avoid)
            {
                foreach (var defender in Defenders)
                {
                    if (!defender.Alive) continue;
                    Vector2 away = next - defender.Position;
                    if (away.sqrMagnitude >= .36f * .36f) continue;
                    next = defender.Position + (away.sqrMagnitude > .00001f ? away.normalized : -direction) * .36f;
                }
            }
            next.x = Mathf.Clamp(next.x, 0, Width - 1); next.y = Mathf.Clamp(next.y, 0, Height - 1);
            unit.Position = next; unit.IsMoving = (next - before).sqrMagnitude > .0000001f;
            if (unit.IsMoving) unit.Facing = (next - before).normalized;
        }

        static void Face(FormationActor actor, Vector2 target)
        {
            Vector2 difference = target - actor.Position;
            if (difference.sqrMagnitude > .00001f) actor.Facing = difference.normalized;
        }

        void FinishIfAtGate()
        {
            if (Phase != FormationPhase.Exiting) return;
            foreach (var unit in Units)
                if (unit.Enabled && unit.Alive && unit.Position.x >= Width - 1 - .0001f) { Phase = FormationPhase.Won; return; }
        }

        FormationFighter FindSlot(int slot)
        {
            foreach (var unit in Units) if (unit.Slot == slot) return unit;
            return null;
        }

        bool CellOccupied(Vector2 cell, int exceptSlot)
        {
            foreach (var unit in Units) if (unit.Slot != exceptSlot && unit.Enabled && unit.Spawn == cell) return true;
            return false;
        }

        // Explicit PRNG avoids framework-specific System.Random sequence changes in recorded seeds.
        struct FormationRandom
        {
            uint state;
            public FormationRandom(uint seed) { state = seed == 0 ? 0x6d2b79f5u : seed; }
            public int Next(int maximum)
            {
                state ^= state << 13; state ^= state >> 17; state ^= state << 5;
                return (int)(state % (uint)maximum);
            }
        }
    }
}
