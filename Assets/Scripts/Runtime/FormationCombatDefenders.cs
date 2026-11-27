using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleModel
    {
        // Source units per second, multiplied by the same rate as the source action.
        internal const float GuardRangedProjectileSpeed = 10;
        internal const float ExusiaiProjectileSpeed = 30;
        internal const double ExusiaiAttackRelease = 16d / 60;
        const double Mon3trReconstructedAttackInterval = 1.35;
        static readonly double[] ExusiaiVolleyReleases = { 10d / 60, 12d / 60, 14d / 60, 16d / 60, 18d / 60 };

        internal sealed class FormationGuardEffect
        {
            internal string Kind;
            internal Vector2 Origin, Direction;
            internal float Radius, Length;
            internal double StartedAt, EndsAt, ImpactAt;
            internal double PlacedAt;
        }

        sealed class GuardKit
        {
            internal FormationDefender Guard;
            internal Vector2 OriginalPosition;
            internal Vector2 ArchivedFacing;
            internal int Casts, EffectiveHeals, VolleyRounds;
            internal int ArchivedAmmo;
            internal bool SupernovaOverload;
            internal FormationRandom SupernovaTargetRandom;
            internal MakotoKit Makoto;
            internal double FirstContact = double.PositiveInfinity;
            internal double LastCast, ActiveUntil, NextPlacement;
            internal float OutsideHealing;
            internal int OrdinaryWolfTarget = -1;
            internal float OrdinaryWolfDamageScale;
            internal GuardHuntWolf[] HuntWolves;
            internal double HuntAdvancedAt, HuntNextAuraAt;
            internal bool SignatureEnded;
            internal FormationGuardEffect Effect;
            internal FormationGuardEffect[] ChessPieces;
            internal Vector2Int? WangCenter;
            internal ChenDawnCast ChenDawn;
            internal Dictionary<Vector2Int, int> SnowTiles;
            internal HashSet<int> InSnow;
            internal Dictionary<int, Vector2Int> SnowInduction;
            internal int SnowStrikes;
            internal double NextSnowTick, NextSnowGrowth;
        }

        readonly Dictionary<int, GuardKit> guardKits = new Dictionary<int, GuardKit>();

        GuardKit Kit(FormationDefender guard)
        {
            if (!guardKits.TryGetValue(guard.Id, out var kit) || kit.Guard != guard)
                guardKits[guard.Id] = kit = new GuardKit { Guard = guard, OriginalPosition = guard.Position };
            return kit;
        }

        void ResetGuardKit(FormationDefender guard)
        {
            guardKits.Remove(guard.Id);
            var kit = Kit(guard);
            if (guard.IsSnowBoss) ResetSnowBoss(guard, kit);
            if (guard.IsSupernovaBoss) ResetSupernovaBoss(guard, kit);
            if (guard.IsMakotoBoss) ResetMakotoBoss(guard, kit);
            if (guard.ArtId == "D28") kit.WangCenter = FindWangCrossCenter(guard);
        }

        internal FormationGuardEffect GuardEffect(FormationDefender guard)
        {
            if (!guard.Alive) return null;
            if (guardKits.TryGetValue(guard.Id, out var kit) && kit.Guard == guard && kit.Effect?.EndsAt > Clock)
                return kit.Effect;
            if (Phase == FormationPhase.Preparation && guard.ArtId == "D28")
            {
                var preview = Kit(guard);
                if ((preview.WangCenter ??= FindWangCrossCenter(guard)) is { } center)
                    return new FormationGuardEffect { Kind = "chess", Origin = center, EndsAt = double.PositiveInfinity };
            }
            return null;
        }

        internal bool GuardSkillActive(FormationDefender guard) =>
            guard.Alive && guardKits.TryGetValue(guard.Id, out var kit) && kit.Guard == guard && kit.ActiveUntil > Clock;

        internal bool GuardTransformed(FormationDefender guard) => guard.ArtId == "D23" && GuardSkillActive(guard);

        internal double GuardSignatureStart(FormationDefender guard) =>
            guardKits.TryGetValue(guard.Id, out var kit) && kit.Guard == guard && kit.Casts > 0
                ? kit.LastCast : guard.LastSkillAt >= 0 ? guard.LastSkillAt : double.NegativeInfinity;

        internal double GuardSignatureEnd(FormationDefender guard) =>
            guardKits.TryGetValue(guard.Id, out var kit) && kit.Guard == guard ? kit.ActiveUntil : 0;

        internal Vector2 GuardHomePosition(FormationDefender guard) =>
            guardKits.TryGetValue(guard.Id, out var kit) && kit.Guard == guard
                ? kit.OriginalPosition : guard.Position;

        static bool UsesGuardTimeCharge(FormationDefender guard)
        {
            switch (guard.ArtId)
            {
                case "D21": case "D22": case "D23": case "D24": case "D26": case "D27": case "D28": return false;
                default: return true;
            }
        }

        float GuardMovementSlow(FormationFighter fighter)
        {
            float slow = Buff(fighter, "guard_chess_slow");
            foreach (var guard in Defenders)
            {
                if (guard.ArtId != "D27") continue;
                if (guard.IsSnowBoss)
                {
                    if (SnowBossSnow(guard).ContainsKey(Vector2Int.RoundToInt(fighter.Position)))
                        slow = Mathf.Max(slow, .25f);
                    continue;
                }
                var effect = GuardEffect(guard);
                if (effect != null && (fighter.Position - effect.Origin).sqrMagnitude <= effect.Radius * effect.Radius)
                    return Mathf.Max(slow, .25f);
            }
            return slow;
        }

        float GuardProjectileMultiplier(FormationFighter source, FormationDefender target, bool area)
        {
            // Flight is available only for attacks with a confirmed projectile presentation.
            var shot = HitPresentation(source);
            if (shot == null || shot.ContactAt <= shot.LaunchedAt || !shot.ProjectileOnly && (area || shot.TargetId != target.Id))
                return 1;
            foreach (var guard in Defenders)
            {
                if (guard.ArtId != "D22") continue;
                var wall = Kit(guard).Effect;
                if (wall?.Kind != "firewall") continue;
                float start = shot.From.x - wall.Origin.x;
                float end = shot.To.x - wall.Origin.x;
                if (start * end >= 0) continue;
                float fraction = start / (start - end);
                float cross = Mathf.Lerp(shot.From.y, shot.To.y, fraction);
                double crossingAt = shot.LaunchedAt + (shot.ContactAt - shot.LaunchedAt) * fraction;
                double until = guard.Alive ? wall.EndsAt : Math.Min(wall.EndsAt, guard.LastDeathAt);
                if (crossingAt >= wall.StartedAt && crossingAt < until && Mathf.Abs(cross - wall.Origin.y) <= wall.Radius)
                    return .85f;
            }
            return 1;
        }

        bool GuardSkillCellIsFree(FormationDefender guard)
        {
            if (!guard.SkillCell.HasValue) return false;
            Vector2 cell = guard.SkillCell.Value;
            return cell.x >= DeploymentColumns + 2 && cell.x < Width - 1 && cell.y >= 0 && cell.y < Height &&
                (Terrain == null || Terrain.CanPlaceDefender(cell) && Terrain.IsWalkable(cell) && Terrain.HeightAt(cell) == 0) &&
                !Defenders.Any(other => other != guard && other.Alive && (other.Position - cell).sqrMagnitude < .01f);
        }

        static bool GuardInRange(Vector2 origin, FormationFighter target, float range) =>
            (target.Position - origin).sqrMagnitude <= range * range;

        IEnumerable<FormationFighter> GuardTargets() =>
            Units.Concat(Summons).Where(u => u.Enabled && u.Alive && HasEnteredBattleArea(u));

        FormationFighter[] GuardVictims(Vector2 origin, float radius, int cap) =>
            GuardTargets().Where(u => GuardInRange(origin, u, radius))
                .OrderBy(u => (u.Position - origin).sqrMagnitude).ThenBy(u => u.Id).Take(cap).ToArray();

        FormationFighter[] GuardLine(FormationGuardEffect line, int cap) =>
            GuardTargets().Select(u => new { Unit = u, Offset = u.Position - line.Origin })
                .Where(v =>
                {
                    float along = Vector2.Dot(v.Offset, line.Direction);
                    return along >= 0 && along <= line.Length &&
                        (v.Offset - line.Direction * along).sqrMagnitude <= line.Radius * line.Radius;
                })
                .OrderBy(v => v.Offset.sqrMagnitude).ThenBy(v => v.Unit.Id).Take(cap).Select(v => v.Unit).ToArray();

        FormationPresentationAction BeginGuardAction(FormationDefender guard, bool skill, double duration,
            FormationActor target = null, bool heal = false, string motion = null)
        {
            var action = BeginAction(guard, duration, skill, target, heal);
            if (motion != null) action.Animation = guard.PresentationAction = motion;
            if ((guard.ArtId is "D23" or "D26" or "D28" or "D30" || guard.ArtId == "D21" && motion == "skill_end") && motion != null)
            {
                // These signatures supply individual source motion lengths.
                guard.PresentationDuration = duration;
                action.EndAt = action.At + duration;
            }
            if (target == null) return action;

            // Basic role skills with no separate body clip replay the retained normal action.
            // Their actual spell or projectile still leaves on that pose's source marker.
            if (skill && motion == null && (guard.ArtId is "D05" or "D07" or "D08" or "D09" or "D10" or "D11" or "D12") &&
                animationProfiles.TryGetValue(guard.ArtId, out var profile))
                action.ContactAt = action.LaunchedAt = action.At + profile.AttackContact / action.Rate;

            switch (guard.ArtId)
            {
                case "D05": case "D07": case "D08": case "D12":
                    SetGuardProjectileFlight(action, GuardRangedProjectileSpeed);
                    break;
                case "D21" when motion != "skill_begin":
                    action.LaunchedAt = action.At + .4 / action.Rate;
                    SetGuardProjectileFlight(action, GuardRangedProjectileSpeed);
                    break;
                case "D23" when heal:
                    action.LaunchedAt = action.At + 34d / 60 / action.Rate;
                    SetGuardProjectileFlight(action, GuardRangedProjectileSpeed);
                    break;
                case "D24" when !skill:
                    action.LaunchedAt = action.At + ExusiaiAttackRelease / action.Rate;
                    SetGuardProjectileFlight(action, ExusiaiProjectileSpeed);
                    break;
                case "D25" when action.Animation is "attack" or "skill_attack":
                    double sourceContact = action.Animation == "skill_attack" ? .5 : 20d / 60;
                    action.ContactAt = action.LaunchedAt = action.At + sourceContact / action.Rate;
                    break;
            }
            return action;
        }

        static void SetGuardProjectileFlight(FormationPresentationAction action, float sourceSpeed)
        {
            action.ProjectileMovement = "AdvancedMovement";
            action.ContactAt = action.LaunchedAt + Vector2.Distance(action.From, action.To) / sourceSpeed / action.Rate;
        }

        void GuardContact(FormationDefender guard, FormationPresentationAction action, Action resolve, double fallback = 0,
            FormationActor trackedTarget = null)
        {
            if (action.ContactAt <= action.At && fallback > 0)
                action.ContactAt = action.LaunchedAt = action.At + fallback;
            var context = CaptureActionContext(State(guard));
            context.Presentation = action;
            void Contact()
            {
                if (!guard.Alive || action.Cancelled || SupernovaBossTransitionActive(guard) || MakotoBossTransitionActive(guard))
                { action.Cancelled = true; return; }
                if (trackedTarget?.Alive == true) action.To = trackedTarget.Position;
                else if (action.ProjectileOnly) { action.Cancelled = true; return; }
                RunInPresentationContext(guard, context, resolve);
            }
            if (action.ContactAt <= Clock + 1e-8) Contact();
            else Schedule(action.ContactAt, Contact);
        }

        void TickCampaignDefender(FormationDefender guard)
        {
            var state = State(guard);
            var kit = Kit(guard);
            if (guard.IsSnowBoss)
            {
                TickSnowBoss(guard, kit);
                return;
            }
            if (guard.IsSupernovaBoss)
            {
                TickSupernovaBoss(guard, kit);
                return;
            }
            if (guard.IsMakotoBoss)
            {
                TickMakotoBoss(guard, kit.Makoto);
                return;
            }
            if (guard.ArtId == "D28")
            {
                TickGuardChess(guard, kit);
                return;
            }
            if (guard.ArtId == "D30")
            {
                TickGuardMlynar(guard, kit);
                return;
            }
            if (guard.ArtId == "D32")
            {
                TickChenDawn(guard, kit);
                return;
            }

            if (guard.ArtId == "D21" && GuardSkillActive(guard)) TickGuardHunt(guard, kit);
            if (Buff(guard, "stun") > 0 || Buff(guard, "freeze") > 0) return;
            if (guard.ArtId == "D31" || IsArchivedSignatureGuard(guard.ArtId))
            {
                TickArchivedGuard(guard, kit);
                return;
            }
            if ((guard.ArtId is "D21" or "D26") && TryEndRecurringGuardSkill(guard, kit)) return;
            bool silverAshSkill = guard.ArtId == "D26" && GuardSkillActive(guard);
            var candidates = GuardTargets().Where(u => silverAshSkill
                ? SilverAshFieldContains(kit.Effect, u.Position)
                : GuardInRange(guard.Position, u, guard.Range) && !(guard.Range < 2 && State(u).RidingCarrier != 0)).ToArray();
            var target = candidates.Where(u => Buff(u, "taunt") > 0)
                .OrderBy(u => (u.Position - guard.Position).sqrMagnitude).ThenBy(u => u.Id).FirstOrDefault()
                ?? candidates.FirstOrDefault(u => u.Id == guard.TargetId)
                ?? candidates.OrderBy(u => (u.Position - guard.Position).sqrMagnitude).ThenBy(u => u.Id).FirstOrDefault();
            var threat = target;
            if (threat == null && guard.ArtId == "D22")
                threat = GuardVictims(guard.Position, 3.2f, 1).FirstOrDefault();
            if (threat != null && double.IsPositiveInfinity(kit.FirstContact)) kit.FirstContact = Clock;
            UpdateGuardCharge(guard, kit);
            if (Clock < guard.ActionUntil) return;

            bool attackReady = Clock >= guard.NextAttackAt;
            bool ready = guard.SkillCharge + 1e-5 >= guard.SkillRequired;
            if (state.Boss && attackReady && ready && TryGuardBossSkill(guard, state, kit, target)) return;
            if (guard.ArtId == "D22" && threat != null && TryGuardSignature(guard, kit, threat, attackReady)) return;
            if (guard.ArtId == "D23")
            {
                if (attackReady) TickMon3tr(guard, kit, target);
                return;
            }
            if (guard.Healer)
            {
                if (!attackReady) return;
                var wounded = WoundedGuards(guard.Position, guard.Range, ready ? 2 : 1, guard);
                if (wounded.Length == 0) return;
                var action = BeginGuardAction(guard, ready, ready ? state.GuardActionSeconds : .35, wounded[0], heal: true);
                guard.NextAttackAt = Clock + guard.Interval;
                if (ready) guard.SkillCharge = 0;
                GuardContact(guard, action, () =>
                {
                    foreach (var other in wounded)
                        HealGuard(guard, other, guard.Attack * (ready ? 1.6f : 1) + other.MaxHealth * (ready ? .04f : .02f), ready);
                });
                return;
            }
            if (target == null) return;
            guard.TargetId = target.Id;
            if (!silverAshSkill) Face(guard, target.Position);

            if (!UsesGuardTimeCharge(guard))
            {
                if (guard.ArtId != "D22" && TryGuardSignature(guard, kit, target, attackReady)) return;
                if (guard.ArtId == "D27") return;
                ready = false;
            }

            if (!attackReady) return;
            bool activeMotion = (guard.ArtId == "D21" || guard.ArtId == "D22" || guard.ArtId == "D25") && GuardSkillActive(guard);
            var basic = BeginGuardAction(guard, ready, ready ? state.GuardActionSeconds : .35f, target,
                motion: activeMotion ? "skill_attack" : null);
            guard.NextAttackAt = Clock + (guard.ArtId == "D21" ? LapplandAttackInterval : guard.Interval) / GuardAttackFrequency(guard);
            if (ready) guard.SkillCharge = 0;
            GuardContact(guard, basic, () =>
            {
                if (state.Template == 2 && ready)
                {
                    AddGuardShield(guard, .25f, 8, "guard");
                    return;
                }
                int type = state.Template == 4 || state.Template == 6 ? 1 : 0;
                int count = ready && state.Template == 1 ? 3 : 1;
                var targets = candidates.OrderBy(u => u == target ? 0 : 1)
                    .ThenBy(u => (u.Position - target.Position).sqrMagnitude).Take(count).ToArray();
                if (state.Template == 4 && ready)
                    targets = candidates.Where(u => GuardInRange(target.Position, u, 1.8f)).ToArray();
                if (state.Template == 6 && ready)
                    foreach (var unit in targets) GiveBuff(unit, "slow", .4f, 6);
                int hits = state.Template == 3 && ready ? 3 : 1;
                float multiplier = ready ? (state.Template == 3 ? .8f : state.Template == 1 ? 1.5f : state.Template == 4 ? 1.8f : 1.6f) : 1;
                for (int i = 0; i < hits; i++)
                    foreach (var unit in targets) CombatDamage(guard, unit, guard.Attack * multiplier, type, ready, false);
            }, trackedTarget: basic.ProjectileMovement != null || guard.ArtId == "D09" ? target : null);
            if (guard.ArtId == "D21" && !activeMotion) QueueLapplandOrdinaryWolf(guard, kit, target, basic);
        }

        FormationDefender[] WoundedGuards(Vector2 origin, float radius, int cap, FormationDefender source) =>
            Defenders.Where(d => d.Alive && d.Health > 0 && d.Health < d.MaxHealth && (d == source || Kit(d).OutsideHealing < d.MaxHealth * .2f) &&
                (d.Position - origin).sqrMagnitude <= radius * radius)
                .OrderBy(d => d.Health / d.MaxHealth).ThenBy(d => d.Id).Take(cap).ToArray();

        float HealGuard(FormationDefender source, FormationDefender target, float raw, bool skill)
        {
            if (!target.Alive || target.Health <= 0) return 0;
            var recipient = Kit(target);
            float amount = Mathf.Min(raw*(1-Buff(target,"eternal_heal_down")), target.MaxHealth - target.Health);
            if (source != target) amount = Mathf.Min(amount, target.MaxHealth * .2f - recipient.OutsideHealing);
            if (amount <= 0) return 0;
            if (source != target) recipient.OutsideHealing += amount;
            target.Health += amount;
            source.HealingDone += amount;
            Hits.Add(new FormationHit
            {
                At = Clock, From = source.Position, To = target.Position, FromId = source.Id, ToId = target.Id,
                Amount = amount, Heal = true, Skill = skill, SourceArtId = source.ArtId, Action = HitPresentation(source)
            });
            return amount;
        }

        void AddGuardShield(FormationDefender guard, float fraction, double duration, string key)
        {
            var shields = State(guard).Shields;
            shields.RemoveAll(shield => shield.Key == key);
            shields.Add(new CombatShield { Amount = guard.MaxHealth * fraction, Until = Clock + duration, Key = key, Source = guard });
            guard.Shield = shields.Sum(shield => shield.Amount);
        }

        bool TryGuardBossSkill(FormationDefender guard, CombatState state, GuardKit kit, FormationFighter target)
        {
            if (Campaign.CurrentNode.Plane == 3 && target == null) return false;
            guard.SkillCharge = 0;
            switch (Campaign.CurrentNode.Plane)
            {
                case 1:
                    BeginGuardAction(guard, true, state.GuardActionSeconds, motion: "skill_begin");
                    AddGuardShield(guard, .10f, 6, "boss");
                    kit.ActiveUntil = Clock + 6;
                    return true;
                case 2:
                    var wounded = WoundedGuards(guard.Position, float.PositiveInfinity, 2, guard);
                    var action = BeginGuardAction(guard, true, state.GuardActionSeconds, wounded.FirstOrDefault(),
                        heal: wounded.Length > 0, motion: "skill_begin");
                    AddGuardShield(guard, .05f, 8, "boss");
                    kit.ActiveUntil = Clock + 8;
                    GuardContact(guard, action, () =>
                    {
                        foreach (var ally in wounded) HealGuard(guard, ally, ally.MaxHealth * .04f, true);
                        if (state.Stage > 0)
                            foreach (var fighter in Units.Where(u => u.Enabled && u.Alive)) GiveBuff(fighter, "heal_down", .2f, 8);
                    });
                    return true;
                default:
                    StartGuardBossSlash(guard, state, kit, target);
                    return true;
            }
        }

        static bool GuardCastReady(GuardKit kit, double clock, double firstDelay, double cooldown) =>
            clock >= (kit.Casts == 0 ? kit.FirstContact + firstDelay : kit.LastCast + cooldown);

        void UpdateGuardCharge(FormationDefender guard, GuardKit kit)
        {
            if (UsesGuardTimeCharge(guard) || guard.ArtId == "D23" || double.IsPositiveInfinity(kit.FirstContact)) return;
            if (guard.ArtId == "D22" && kit.Casts > 0 && Campaign.CurrentNode.Id == "1-6") return;
            double recharge = guard.ArtId is "D21" or "D26" ? kit.ActiveUntil : kit.LastCast;
            double charge = kit.Casts == 0 ? State(guard).GuardInitialSP + Clock - kit.FirstContact : Clock - recharge;
            guard.SkillCharge = Mathf.Clamp((float)charge, 0, guard.SkillRequired);
        }

        FormationPresentationAction StartGuardCast(FormationDefender guard, GuardKit kit, FormationFighter target, double startup)
        {
            kit.Casts++;
            kit.LastCast = Clock;
            guard.SkillCharge = 0;
            return BeginGuardAction(guard, true, startup, target, motion: "skill_begin");
        }

        bool TryGuardSignature(FormationDefender guard, GuardKit kit, FormationFighter target, bool attackReady)
        {
            switch (guard.ArtId)
            {
                case "D21":
                    if (!RecurringGuardCastReady(kit, 12, 30)) return false;
                    StartGuardHunt(guard, kit, target);
                    return true;
                case "D22":
                    bool firstWallOnly = Campaign.CurrentNode.Id == "1-6";
                    if ((firstWallOnly && kit.Casts > 0) || !GuardCastReady(kit, Clock, 0, 22)) return false;
                    StartGuardWall(guard, kit, target);
                    return true;
                case "D24":
                    if (Clock >= kit.ActiveUntil) kit.VolleyRounds = 0;
                    if (kit.VolleyRounds > 0)
                    {
                        if (attackReady) FireGuardVolley(guard, kit, target);
                        return true;
                    }
                    if (!GuardCastReady(kit, Clock, 10, 25)) return false;
                    StartGuardCast(guard, kit, target, 7d / 60);
                    kit.VolleyRounds = 4;
                    kit.ActiveUntil = Clock + 12;
                    return true;
                case "D26":
                    if (GuardSkillActive(guard))
                    {
                        if (attackReady) FireGuardSilverAshWave(guard, kit, target);
                        return true;
                    }
                    if (!RecurringGuardCastReady(kit, 12, 24)) return false;
                    StartGuardSilverAshSkill(guard, kit, target);
                    return true;
                case "D27":
                    if (!GuardCastReady(kit, Clock, 10, 24)) return false;
                    StartGuardSnow(guard, kit, target);
                    return true;
                default: return false;
            }
        }

        void StartGuardWall(FormationDefender guard, GuardKit kit, FormationFighter target)
        {
            // The retained 1.6 s opening clip at the board's 2× visual rate.
            var action = StartGuardCast(guard, kit, target, .8);
            GuardContact(guard, action, () =>
            {
                kit.Effect = new FormationGuardEffect
                {
                    Kind = "firewall", Origin = guard.Position + Vector2.left * .5f, Direction = Vector2.left,
                    Radius = 2, Length = 4, StartedAt = Clock, EndsAt = Clock + 6
                };
                kit.ActiveUntil = kit.Effect.EndsAt;
            }, .8);
        }

        void TickMon3tr(FormationDefender guard, GuardKit kit, FormationFighter target)
        {
            if (GuardTransformed(guard))
            {
                if (target != null) AttackFromMon3tr(guard, target);
                return;
            }
            if (kit.Casts == 0 && kit.EffectiveHeals >= 6 && GuardSkillCellIsFree(guard))
            {
                var attackers = GuardVictims(guard.SkillCell.Value, 1.4f, 1);
                if (attackers.Length > 0)
                {
                    TransformMon3tr(guard, kit, attackers[0]);
                    return;
                }
            }

            var wounded = WoundedGuards(guard.Position, guard.Range, 3, guard);
            if (wounded.Length == 0) return;
            var action = BeginGuardAction(guard, false, .35, wounded[0], heal: true);
            GuardContact(guard, action, () =>
            {
                bool effective = false;
                float heal = guard.Attack;
                foreach (var ally in wounded)
                {
                    effective |= HealGuard(guard, ally, heal, false) > 0;
                    heal *= .75f;
                }
                if (!effective || kit.Casts > 0) return;
                kit.EffectiveHeals++;
                guard.SkillCharge = Mathf.Min(kit.EffectiveHeals, 6);
            });
        }

        void TransformMon3tr(FormationDefender guard, GuardKit kit, FormationFighter target)
        {
            var action = StartGuardCast(guard, kit, target, 1d / 3);
            GuardContact(guard, action, () =>
            {
                if (!GuardSkillCellIsFree(guard)) return;
                guard.Position = guard.SkillCell.Value;
                guard.Range = 1.4f;
                guard.TargetId = -1;
                guard.NextAttackAt = Clock;
                kit.ActiveUntil = Clock + 10;
                ScheduleOwned(guard, kit.ActiveUntil, () =>
                {
                    guard.Position = kit.OriginalPosition;
                    guard.Range = State(guard).BaseRange;
                    guard.TargetId = -1;
                    BeginGuardAction(guard, true, 5d / 12, motion: "skill_end");
                });
            }, 1d / 3);
        }

        void AttackFromMon3tr(FormationDefender guard, FormationFighter target)
        {
            Face(guard, target.Position);
            guard.TargetId = target.Id;
            var action = BeginGuardAction(guard, true, .75, target, motion: "skill_attack");
            action.ContactAt = action.LaunchedAt = action.At + (2d / 3) / action.Rate;
            guard.NextAttackAt = Clock + Mon3trReconstructedAttackInterval;
            GuardContact(guard, action, () =>
            {
                if (!GuardTransformed(guard)) return;
                foreach (var unit in GuardVictims(guard.Position, guard.Range, 3))
                    CombatDamage(guard, unit, guard.Attack * 3.8f, 2, true, false);
            });
        }

        void FireGuardVolley(FormationDefender guard, GuardKit kit, FormationFighter target)
        {
            var volley = BeginGuardAction(guard, true, 2d / 3, target, motion: "skill_attack");
            guard.NextAttackAt = Clock + guard.Interval;
            kit.VolleyRounds--;
            if (kit.VolleyRounds == 0) kit.ActiveUntil = Clock + 2d / 3;
            volley.LaunchedAt = volley.At + ExusiaiVolleyReleases[0] / volley.Rate;
            for (int i = 0; i < ExusiaiVolleyReleases.Length; i++)
            {
                double release = volley.At + ExusiaiVolleyReleases[i] / volley.Rate;
                var shot = new FormationPresentationAction
                {
                    Sequence = ++presentationSequence, ActorId = guard.Id, TargetId = target.Id,
                    At = volley.At, LaunchedAt = release, From = volley.From, To = volley.To,
                    Skill = true, Rate = volley.Rate, Animation = volley.Animation,
                    ProjectileOnly = true, ParentSequence = volley.Sequence, ProjectileIndex = i
                };
                SetGuardProjectileFlight(shot, ExusiaiProjectileSpeed);
                shot.EndAt = shot.ContactAt;
                volley.ContactAt = shot.ContactAt;
                presentationActions.Add(shot);
                GuardContact(guard, shot, () => CombatDamage(guard, target, guard.Attack * .55f, 0, true, false),
                    trackedTarget: target);
            }
        }

        void StartGuardSnow(FormationDefender guard, GuardKit kit, FormationFighter target)
        {
            var action = StartGuardCast(guard, kit, target, 1d / 6);
            Vector2 center = target.Position;
            GuardContact(guard, action, () =>
            {
                var snow = new FormationGuardEffect
                {
                    Kind = "snow", Origin = center, Radius = 2,
                    StartedAt = Clock, EndsAt = Clock + 8
                };
                kit.Effect = snow;
                kit.ActiveUntil = snow.EndsAt;
                for (int tick = 1; tick <= 8; tick++)
                    ScheduleOwned(guard, snow.StartedAt + tick, () =>
                    {
                        foreach (var unit in GuardVictims(snow.Origin, snow.Radius, int.MaxValue))
                            CombatDamage(guard, unit, guard.Attack * .60f, 1, true, false);
                    });
            }, 1d / 6);
        }

        void StartGuardBossSlash(FormationDefender guard, CombatState state, GuardKit kit, FormationFighter target)
        {
            Face(guard, target.Position);
            guard.TargetId = target.Id;
            int strikes = state.Stage >= 2 ? 5 : 3;
            var slash = new FormationGuardEffect
            {
                Kind = "slash", Origin = guard.Position, Direction = guard.Facing,
                Radius = 1, Length = new Vector2(Width, Height).magnitude,
                StartedAt = Clock, ImpactAt = Clock + 3, EndsAt = Clock + 3.3
            };
            kit.Effect = slash;
            kit.ActiveUntil = slash.EndsAt;
            BeginGuardAction(guard, true, state.GuardActionSeconds, target, motion: "skill_begin");
            ScheduleOwned(guard, slash.ImpactAt - .3, () =>
            {
                var action = BeginGuardAction(guard, true, .6, target, motion: "skill_attack");
                guard.LastSkillAt = slash.StartedAt;
                action.ContactAt = action.LaunchedAt = slash.ImpactAt;
                GuardContact(guard, action, () =>
                {
                    var victims = GuardLine(slash, 5);
                    void Strike()
                    {
                        foreach (var unit in victims) CombatDamage(guard, unit, guard.Attack * 1.6f, 0, true, false);
                    }
                    if (strikes == 3)
                    {
                        for (int i = 0; i < strikes; i++) Strike();
                    }
                    else
                    {
                        // Bound the five board-authored contacts to one retained 2× slash clip.
                        Strike();
                        for (int i = 1; i < strikes; i++) ScheduleOwned(guard, slash.ImpactAt + i * .055, Strike);
                    }
                });
            });
        }
    }
}
