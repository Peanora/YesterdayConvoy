using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Source seconds only. The controller injects metadata; the model never reads art or files.</summary>
    public sealed class FormationAnimationProfile
    {
        public float AttackDuration, AttackContact, SkillDuration, SkillContact;
        // Source seconds; leave zero when the original flight time is not established.
        public float AttackFlightSeconds;
        public float ProjectileSpeed, ProjectileRaiseHeight, ProjectileDelay, ProjectileNoRaiseThreshold;
        public string ProjectileMovement;
        public float SkillProjectileSpeed, SkillProjectileRaiseHeight, SkillProjectileDelay, SkillProjectileWindup;
        public string SkillProjectileMovement;
        public float BaseRate = 2;
        public bool ScaleAttackRate = true, ScaleSkillRate = true, HasOrdinaryAttack = true;
    }

    /// <summary>A real action and its release point, independent from the gameplay action lock.</summary>
    public sealed class FormationPresentationAction
    {
        public int Sequence { get; internal set; }
        public int ActorId { get; internal set; }
        public int TargetId { get; internal set; }
        public double At { get; internal set; }
        public double LaunchedAt { get; internal set; }
        public double ContactAt { get; internal set; }
        public double EndAt { get; internal set; }
        public Vector2 From { get; internal set; }
        public Vector2 To { get; internal set; }
        public bool Skill { get; internal set; }
        public bool Heal { get; internal set; }
        public float Rate { get; internal set; }
        public string Animation { get; internal set; }
        public float ProjectileRaiseHeight { get; internal set; }
        public string ProjectileMovement { get; internal set; }
        public bool ProjectileOnly { get; internal set; }
        public int ParentSequence { get; internal set; }
        public int ProjectileIndex { get; internal set; }
        public bool Cancelled { get; internal set; }
    }

    public sealed partial class FormationBattleModel
    {
        readonly Dictionary<string, FormationAnimationProfile> animationProfiles = new Dictionary<string, FormationAnimationProfile>(StringComparer.Ordinal);
        readonly List<FormationPresentationAction> presentationActions = new List<FormationPresentationAction>();
        public IReadOnlyList<FormationPresentationAction> PresentationActions => presentationActions;
        int presentationSequence;
        FormationActor activePresentationOwner;
        AutoActionContext activePresentationContext;
        readonly Dictionary<int, SkillActionCompletion> skillActionCompletions = new Dictionary<int, SkillActionCompletion>();

        public void SetAnimationProfile(string artOrCardId, FormationAnimationProfile profile)
        {
            if (string.IsNullOrEmpty(artOrCardId)) throw new ArgumentException("An animation profile needs a stable unit ID.", nameof(artOrCardId));
            string key = FormationUnitCatalog.Find(artOrCardId)?.ArtId ??
                FormationUnitCatalog.Find(FormationCampaignRules.Find(artOrCardId)?.CardId)?.ArtId ?? artOrCardId;
            if (profile == null) { animationProfiles.Remove(key); return; }
            foreach (float value in new[] { profile.AttackDuration, profile.AttackContact, profile.SkillDuration, profile.SkillContact, profile.AttackFlightSeconds,
                profile.ProjectileSpeed, profile.ProjectileRaiseHeight, profile.ProjectileDelay,profile.ProjectileNoRaiseThreshold,
                profile.SkillProjectileSpeed, profile.SkillProjectileRaiseHeight, profile.SkillProjectileDelay, profile.SkillProjectileWindup })
                if (float.IsNaN(value) || float.IsInfinity(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(profile), "Source timings must be finite and nonnegative.");
            if (float.IsNaN(profile.BaseRate) || float.IsInfinity(profile.BaseRate) || profile.BaseRate <= 0 ||
                profile.AttackContact > profile.AttackDuration || profile.SkillContact > profile.SkillDuration)
                throw new ArgumentOutOfRangeException(nameof(profile), "Invalid source animation rate or contact time.");
            animationProfiles[key] = new FormationAnimationProfile
            {
                AttackDuration = profile.AttackDuration, AttackContact = profile.AttackContact,
                AttackFlightSeconds = profile.AttackFlightSeconds,
                ProjectileSpeed = profile.ProjectileSpeed, ProjectileRaiseHeight = profile.ProjectileRaiseHeight,
                ProjectileDelay = profile.ProjectileDelay, ProjectileMovement = profile.ProjectileMovement,
                ProjectileNoRaiseThreshold=profile.ProjectileNoRaiseThreshold,
                SkillProjectileSpeed=profile.SkillProjectileSpeed, SkillProjectileRaiseHeight=profile.SkillProjectileRaiseHeight,
                SkillProjectileDelay=profile.SkillProjectileDelay, SkillProjectileMovement=profile.SkillProjectileMovement,
                SkillProjectileWindup=profile.SkillProjectileWindup,
                SkillDuration = profile.SkillDuration, SkillContact = profile.SkillContact, BaseRate = profile.BaseRate,
                ScaleAttackRate = profile.ScaleAttackRate, ScaleSkillRate = profile.ScaleSkillRate,
                HasOrdinaryAttack = profile.HasOrdinaryAttack
            };
        }

        bool AllowsOrdinaryAttack(FormationActor actor) => !animationProfiles.TryGetValue(actor.ArtId, out var profile) || profile.HasOrdinaryAttack;
        void ResetAnimationPresentation()
        {
            // Keep the sequence monotonic when a persistent view observes a retry/new round.
            presentationActions.Clear();
            suiClawStrikes.Clear();
            skillActionCompletions.Clear(); activePresentationOwner = null; activePresentationContext = null;
            foreach (var actor in Units.Cast<FormationActor>().Concat(Defenders))
            {
                actor.PresentationAction = null; actor.PresentationStartedAt = actor.PresentationDuration = actor.PresentationMoveLockUntil = 0;
                actor.PresentationRate = 1;
                if (actor is FormationFighter fighter) fighter.GroundDistanceTravelled = 0;
            }
        }

        FormationPresentationAction BeginPresentation(FormationActor actor, FormationActor target, bool skill, bool heal, double logicalDuration)
        {
            float rate = 1, projectileRaise = 0; string projectileMovement = null;
            double duration = logicalDuration, contact = 0, flight = 0;
            if (animationProfiles.TryGetValue(actor.ArtId, out var profile))
            {
                rate = profile.BaseRate;
                if (!skill && profile.ScaleAttackRate && actor.Interval > 0 && State(actor).BaseInterval > 0)
                    rate *= State(actor).BaseInterval / actor.Interval;
                if (skill && profile.ScaleSkillRate)
                {
                    float haste = Buff(actor, "haste");
                    if (haste > 0) rate /= haste;
                    if (Key(actor) == "B04" && State(actor).Stage > 0) rate /= .8f;
                }
                duration = (skill ? profile.SkillDuration : profile.AttackDuration) / rate;
                contact = (skill ? profile.SkillContact : profile.AttackContact) / rate;
                if (!skill)
                {
                    contact += profile.ProjectileDelay / rate;
                    flight = profile.ProjectileSpeed > 0 && target != null ? Vector2.Distance(actor.Position, target.Position) / profile.ProjectileSpeed / rate : profile.AttackFlightSeconds / rate;
                    projectileRaise = Vector2.Distance(actor.Position,target?.Position??actor.Position)<=profile.ProjectileNoRaiseThreshold?0:profile.ProjectileRaiseHeight; projectileMovement = profile.ProjectileMovement;
                }
            }
            actor.PresentationStartedAt = Clock;
            actor.PresentationDuration = Math.Max(0, duration);
            actor.PresentationRate = rate;
            actor.PresentationMoveLockUntil = Math.Min(Clock + Math.Max(0, duration), actor.ActionUntil);
            var action = new FormationPresentationAction
            {
                Sequence = ++presentationSequence, ActorId = actor.Id, TargetId = target?.Id ?? -1,
                At = Clock, LaunchedAt = Clock + contact, ContactAt = Clock + contact + flight, EndAt = Clock + Math.Max(0, duration),
                From = actor.Position, To = target?.Position ?? actor.Position,
                Skill = skill, Heal = heal, Rate = rate, Animation = actor.PresentationAction,
                ProjectileRaiseHeight = projectileRaise, ProjectileMovement = projectileMovement
            };
            presentationActions.Add(action);
            return action;
        }

        // Only per-action counters are swapped when an old windup overlaps a newer cast.
        // Persistent combo progress, buffs, item stacks and resources stay on the actor.
        sealed class AutoActionContext
        {
            internal int Action, ActionHits, ComboPerAction, SkillBeats;
            internal float SkillSPReturned, PromptReturned;
            internal bool CapturesCast;
            internal int CastOrdinal;
            internal SkillActionCompletion SkillCompletion;
            internal FormationPresentationAction Presentation;
            internal Dictionary<string, float> ActionStacks;
            internal int[] SkillTargets;
            internal Dictionary<int, float> SkillDamage, SkillHealing, SkillShields;
        }
        static readonly string[] PresentationActionStackKeys = { "tape_auto", "last_skill_raw", "cast_multiplier", "hasteused", "resonant_cast" };
        AutoActionContext CaptureActionContext(CombatState state) => new AutoActionContext
        {
            Action = state.Action, ActionHits = state.ActionHits, ComboPerAction = state.ComboPerAction, SkillBeats = state.SkillBeats,
            SkillSPReturned = state.SkillSPReturned, PromptReturned = state.PromptReturned,
            ActionStacks = PresentationActionStackKeys.ToDictionary(key => key, key => state.Stacks.TryGetValue(key, out var value) ? value : 0),
            SkillTargets = state.SkillTargets.ToArray(), SkillDamage = new Dictionary<int, float>(state.SkillDamage),
            SkillHealing = new Dictionary<int, float>(state.SkillHealing), SkillShields = new Dictionary<int, float>(state.SkillShields)
        };
        static void CopyActionDictionary(Dictionary<int, float> target, Dictionary<int, float> values)
        { target.Clear(); foreach (var pair in values) target.Add(pair.Key, pair.Value); }
        void RestoreActionContext(CombatState state, AutoActionContext context)
        {
            state.Action = context.Action; state.ActionHits = context.ActionHits; state.ComboPerAction = context.ComboPerAction; state.SkillBeats = context.SkillBeats;
            state.SkillSPReturned = context.SkillSPReturned; state.PromptReturned = context.PromptReturned;
            foreach (var value in context.ActionStacks) state.Stacks[value.Key] = value.Value;
            state.SkillTargets.Clear(); state.SkillTargets.UnionWith(context.SkillTargets);
            CopyActionDictionary(state.SkillDamage, context.SkillDamage); CopyActionDictionary(state.SkillHealing, context.SkillHealing); CopyActionDictionary(state.SkillShields, context.SkillShields);
        }
        void QueueOrdinaryAction(FormationFighter actor, FormationActor target, FormationPresentationAction presentation, Action resolve)
        {
            var state = State(actor); int life = state.LifeVersion; var context = CaptureActionContext(state);
            context.Presentation = presentation;
            Action contact = () =>
            {
                if (presentation.Cancelled || !actor.Alive || State(actor).LifeVersion != life || target == null || !target.Alive)
                { presentation.Cancelled = true; return; }
                presentation.To = target.Position;
                RunInPresentationContext(actor, context, resolve);
            };
            if (presentation.ContactAt <= Clock + 1e-8) contact();
            else Schedule(presentation.ContactAt, contact);
        }

        void UpdateActionContext(AutoActionContext target, CombatState state)
        {
            var fresh = CaptureActionContext(state);
            target.ActionHits = fresh.ActionHits; target.ComboPerAction = fresh.ComboPerAction; target.SkillBeats = fresh.SkillBeats;
            target.SkillSPReturned = fresh.SkillSPReturned; target.PromptReturned = fresh.PromptReturned;
            target.ActionStacks = fresh.ActionStacks; target.SkillTargets = fresh.SkillTargets;
            target.SkillDamage = fresh.SkillDamage; target.SkillHealing = fresh.SkillHealing; target.SkillShields = fresh.SkillShields;
        }
        void RunInPresentationContext(FormationActor actor, AutoActionContext context, Action resolve)
        {
            var state = State(actor);
            var newer = state.Action == context.Action ? null : CaptureActionContext(state);
            int globalCasts = state.Casts;
            var previousOwner = activePresentationOwner; var previousContext = activePresentationContext;
            if (newer != null) RestoreActionContext(state, context);
            if (context.CapturesCast) state.Casts = context.CastOrdinal;
            activePresentationOwner = actor; activePresentationContext = context;
            try { resolve(); }
            finally
            {
                // Some original skills start a real ordinary follow-up from their callback.
                // BeginAction saved the old scope before clearing those per-action totals.
                // A genuinely new action is the latest state, not an interrupted old cast.
                bool createdNewAction = state.Action != context.Action && (newer == null || state.Action > newer.Action);
                if (!createdNewAction) UpdateActionContext(context, state);
                int newCasts = context.CapturesCast ? state.Casts - context.CastOrdinal : 0;
                if (newer != null && !createdNewAction) RestoreActionContext(state, newer);
                if (context.CapturesCast) state.Casts = globalCasts + newCasts;
                activePresentationOwner = previousOwner; activePresentationContext = previousContext;
            }
        }
        void PreserveActivePresentationContext(FormationActor actor)
        {
            if (activePresentationOwner == actor && activePresentationContext != null && State(actor).Action == activePresentationContext.Action)
                UpdateActionContext(activePresentationContext, State(actor));
        }

        sealed class SkillActionCompletion
        {
            internal FormationFighter Actor;
            internal FormationPresentationAction Presentation;
            internal AutoActionContext Context;
            internal int Life, PendingOwned;
            internal double LatestOwnedAt;
            internal bool ContactResolved, Finished;
        }
        void QueueSkillAction(FormationFighter actor, FormationPresentationAction presentation, Action resolve)
        {
            var context = CaptureActionContext(State(actor)); context.CapturesCast = true; context.CastOrdinal = State(actor).Casts;
            context.Presentation = presentation;
            var completion = new SkillActionCompletion
            {
                Actor = actor, Presentation = presentation, Context = context,
                Life = State(actor).LifeVersion, LatestOwnedAt = presentation.ContactAt
            };
            context.SkillCompletion = completion; skillActionCompletions[presentation.Sequence] = completion;
            Action contact = () =>
            {
                completion.ContactResolved = true;
                if (!actor.Alive || State(actor).LifeVersion != completion.Life)
                { presentation.Cancelled = true; return; }
                var target = Units.Cast<FormationActor>().Concat(Defenders).Concat(Summons).FirstOrDefault(a => a.Id == presentation.TargetId);
                if (target != null) presentation.To = target.Position;
                RunInPresentationContext(actor, context, resolve);
            };
            if (presentation.ContactAt <= Clock + 1e-8) contact();
            else Schedule(presentation.ContactAt, contact);
        }

        // A segment records one real gameplay release. It never starts another cast,
        // consumes SP or changes the actor's body-action/lock. Unknown flight is zero.
        void QueueSkillProjectile(FormationFighter actor, FormationPresentationAction parent, FormationDefender target,
            int index, string animation, double launchedAt, Action impact, bool requiresLiveTarget)
        {
            animationProfiles.TryGetValue(actor.ArtId, out var profile);
            float rate = parent.Rate > 0 ? parent.Rate : 1;
            double release = launchedAt + (profile?.SkillProjectileDelay ?? 0) / rate;
            double flight = profile != null && profile.SkillProjectileSpeed > 0 ?
                Vector2.Distance(actor.Position, target.Position) / profile.SkillProjectileSpeed / rate : 0;
            if (Key(actor) == "WIS" && animation == "Skill_3_Loop") flight = Math.Max(WisadelShellMinimumFlight, flight);
            var projectile = new FormationPresentationAction
            {
                Sequence=++presentationSequence, ActorId=actor.Id, TargetId=target.Id,
                At=release-(profile?.SkillProjectileWindup??0)/rate, LaunchedAt=release, ContactAt=release+flight, EndAt=release+flight,
                From=actor.Position, To=target.Position, Skill=true, Heal=false, Rate=rate, Animation=animation,
                ProjectileOnly=true, ParentSequence=parent.Sequence, ProjectileIndex=index,
                ProjectileRaiseHeight=profile?.SkillProjectileRaiseHeight??0, ProjectileMovement=profile?.SkillProjectileMovement
            };
            presentationActions.Add(projectile);
            var context = activePresentationOwner == actor ? activePresentationContext : null;
            var completion = context?.SkillCompletion;
            int life = State(actor).LifeVersion;
            if(completion!=null){completion.PendingOwned++;completion.LatestOwnedAt=Math.Max(completion.LatestOwnedAt,projectile.ContactAt);}
            Action land=()=>
            {
                try
                {
                    // Preserve the explicitly documented current dead-owner policy.
                    if(!actor.Alive||State(actor).LifeVersion!=life||requiresLiveTarget&&!target.Alive)
                    {projectile.Cancelled=true;return;}
                    projectile.To=target.Position;
                    if(context==null)impact();
                    else
                    {
                        var previous = context.Presentation;
                        context.Presentation = projectile;
                        try { RunInPresentationContext(actor,context,impact); }
                        finally { context.Presentation = previous; }
                    }
                }
                finally{if(completion!=null)completion.PendingOwned--;}
            };
            if(projectile.ContactAt<=Clock+1e-8)land();else Schedule(projectile.ContactAt,land);
        }

        FormationPresentationAction HitPresentation(FormationActor actor) =>
            activePresentationOwner == actor ? activePresentationContext?.Presentation : null;

        // ScheduleOwned called by a skill's real switch/segments inherits that action.
        // No other owner or later cast can borrow its per-action caps or replay totals.
        void ScheduleOwnedPresentation(FormationActor owner, double at, Action fire)
        {
            int life = State(owner).LifeVersion;
            var context = activePresentationOwner == owner ? activePresentationContext : null;
            var completion = context?.SkillCompletion;
            if (completion != null) { completion.PendingOwned++; completion.LatestOwnedAt = Math.Max(completion.LatestOwnedAt, at); }
            Schedule(at, () =>
            {
                try
                {
                    if (!owner.Alive || State(owner).LifeVersion != life) return;
                    if (context == null) fire(); else RunInPresentationContext(owner, context, fire);
                }
                finally { if (completion != null) completion.PendingOwned--; }
            });
        }

        // Only derived completion waits for the real transaction. The configured
        // ActionUntil, NextAttackAt and SP consumption are never extended here.
        void ScheduleSkillCompletion(FormationFighter actor, FormationPresentationAction presentation, double originalFinishAt, Action finish)
        {
            if (!skillActionCompletions.TryGetValue(presentation.Sequence, out var completion))
                throw new InvalidOperationException("Queue the skill transaction before scheduling its completion.");
            Action attempt = null;
            attempt = () =>
            {
                if (completion.Finished) return;
                if (!actor.Alive || State(actor).LifeVersion != completion.Life || presentation.Cancelled)
                { completion.Finished = true; skillActionCompletions.Remove(presentation.Sequence); return; }
                if (!completion.ContactResolved || completion.PendingOwned > 0)
                {
                    double next = !completion.ContactResolved ? presentation.ContactAt : completion.LatestOwnedAt;
                    Schedule(Math.Max(Clock + StepSeconds, next + .0001), attempt); return;
                }
                completion.Finished = true;
                RunInPresentationContext(actor, completion.Context, finish);
                skillActionCompletions.Remove(presentation.Sequence);
            };
            Schedule(Math.Max(Clock, originalFinishAt), attempt);
        }
    }
}
