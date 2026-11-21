using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        float showcaseTargetPressure;
        string showcaseGuardArt;
        bool showcaseUseCurrentBoard;
        internal string ShowcaseFixtureNote => "展示夹具：一星原数值、无装备；预置10级编队与右侧起点，3名高生命展示靶。" +
            "靶默认不反击；治疗、受击及阶段片段按字幕启用首靶压力。" +
            "技能依正常技力和动作规则触发；不是自然对局或平衡演示。";

        // Called only by the explicitly requested showcase recorder. Normal play never
        // invokes this fixture or changes its lineup, targets, camera or combat clock.
        internal IEnumerator PrepareShowcaseFixture(string[] unitIds, int talulahCost = 3, int wisadelSkill = 3) =>
            PrepareShowcaseRoleFixture(unitIds, talulahCost, wisadelSkill, null, false, false);

        internal IEnumerator PrepareShowcaseRoleFixture(string[] unitIds, int talulahCost, int wisadelSkill,
            string guardArt, bool snowBoss, bool useCurrentBoard)
        {
            if (!Ready) throw new InvalidOperationException("The saved formation scene is not ready.");
            if (unitIds == null || unitIds.Length < 1 || unitIds.Length > 10)
                throw new ArgumentException("A showcase needs one to ten distinct formal characters.", nameof(unitIds));
            if (talulahCost < 3 || talulahCost > 5 || wisadelSkill < 1 || wisadelSkill > 3)
                throw new ArgumentOutOfRangeException("Invalid showcase skill variant.");
            var definitions = unitIds.Select(FormationCampaignRules.Find).ToArray();
            if (definitions.Any(spec => spec == null) || definitions.Select(spec => spec.CardId).Distinct().Count() != definitions.Length)
                throw new ArgumentException("Showcase characters must be distinct entries from the formal v0.7 roster.", nameof(unitIds));

            CancelFormationDrag();
            CloseCampaignPanels();
            CloseFormationTraitDetails();
            SetMarketVisible(false);
            DisposeFormationNativeEffects();
            foreach (var body in bodies.Values)
                if (body != null) { body.gameObject.SetActive(false); Destroy(body.gameObject); }
            bodies.Clear();
            pinned.Clear();
            ArtMemoryCache.Pin(Array.Empty<string>());
            approvedShop?.ResetShopPresentation();

            showcaseGuardArt = guardArt;
            showcaseUseCurrentBoard = useCurrentBoard;
            string node = guardArt == "D25" ? "1-9" : guardArt == "D29" ? "2-7" : snowBoss ? "3-7" : "1-1";
            Session = guardArt == null ? FormationShopSession.StartNew(seed, fullCampaign: true) :
                FormationShopSession.StartPractice(seed, Array.FindIndex(FormationCampaignRules.Nodes, entry => entry.Id == node));
            if (Session.Run.OpeningRewardsPending && !Session.CompleteOpeningDelivery())
                throw new InvalidOperationException("Could not deliver the production opening rewards for the showcase.");
            for (int choice = 0; Session.Run.HasBlockingChoice; choice++)
            {
                if (choice >= 32 || !Session.Choose(0).Success)
                    throw new InvalidOperationException("Could not resolve the production opening choices for the showcase.");
            }
            Session.Run.WisadelSkill = wisadelSkill;
            string talulah = FormationCampaignRules.Find("TAL").CardId;
            if (definitions.Any(spec => spec.Id == "TAL"))
                for (int cost = 3; cost < talulahCost; cost++)
                {
                    var promoted = Shop.GrantAtLeast(talulah, 2);
                    if (!promoted.Success || !Shop.ResolveTalulah(true).Success)
                        throw new InvalidOperationException("Could not prepare the requested Talulah cost through her production promotion.");
                }
            Shop.Preset(definitions.Select(spec => new ShopTrialModel.Piece { Id = spec.CardId, Star = 1 }),
                Array.Empty<string>(), level: 10, xp: 0, gold: 0);
            Model = new FormationBattleModel(seed, terrain != null ? terrain.BuildLayout() : null);
            Session.ApplyTo(Model);
            // Clear any default placement selected by shared opening card identities,
            // then use the real reserve-to-board transaction for every showcase member.
            foreach (var piece in Shop.Bench.ToArray())
                if (Session.Seats.InLineup(piece.Instance))
                {
                    int reserve = Session.Seats.FirstEmpty(false, 10);
                    if (reserve < 0 || !Session.Move(piece.Instance, false, reserve).Success)
                        throw new InvalidOperationException("Could not clear a default showcase seat.");
                }
            for (int index = 0; index < definitions.Length; index++)
            {
                var piece = Shop.Bench.Single(card => card.Id == definitions[index].CardId);
                var anchor = new Vector2Int(8 - index % 5 * 2, 2 + index / 5 * 3);
                var result = Session.TryDeploy(piece.Instance, anchor);
                if (!result.Success) throw new InvalidOperationException("Invalid showcase deployment: " + result.Message);
            }
            Session.SavePositions(Model);
            Model.LimitSeconds = double.PositiveInfinity;
            Model.Speed = 1;
            showcaseTargetPressure = 0;
            if (guardArt == null) PrepareShowcaseTargets();
            else Model.PrepareShowcaseGuard(guardArt);
            campaignResultSettled = false;
            campaignInventoryOpen = campaignFormationOpen = campaignRouteOpen = campaignScoutingOpen = false;
            campaignSeaPainting = false;
            formationMenuOpen = shopOddsVisible = false;
            selectedPiece = 0;
            formationInspectedTag = null;
            lastSoundAt = -10;
            // Keep the initialized original culling mask and fixed camera poses. A
            // re-initialization from an already filtered scouting mask hides both halves.
            RefreshFormationOperatingVisibility();
            EnsureFormationArt();
            double artDeadline = Time.realtimeSinceStartupAsDouble + 600;
            while (!RosterArtReady)
            {
                if (Time.realtimeSinceStartupAsDouble > artDeadline)
                    throw new TimeoutException("Showcase source animation pages did not warm completely.");
                EnsureFormationArt();
                yield return null;
            }
            SelectFirstLineup();
            SyncActors();
            approvedShop?.SyncShopBenchModels();
            SetFormationView(false, true);
            yield return null;
        }

        internal void BeginShowcaseFixture()
        {
            StartBattle();
            if (Model.Phase != FormationPhase.Fighting)
                throw new InvalidOperationException("The production formation model did not start the showcase battle.");
            // Deployment remains legal and saved on the left. Only this opt-in visual
            // fixture starts the actual actors just inside the visible right battlefield.
            float[] rows = { 3f, 1.8f, 4.2f, .6f, 5.4f, 6.6f, 2.4f, 3.6f, 1.2f, 4.8f };
            for (int index = 0; index < Model.Units.Count; index++)
            {
                var unit = Model.Units[index];
                unit.Position = new Vector2(11.25f + (index / 7) * .9f, rows[index]);
                unit.Facing = Vector2.right;
            }
            if (showcaseGuardArt == null) PrepareShowcaseTargets();
            else Model.BeginShowcaseGuard(showcaseGuardArt);
            Model.Paused = false;
            RefreshFormationOperatingVisibility();
            SetFormationView(true, true);
            SyncActors();
        }

        internal void SetShowcaseTargetPressure(float attack, float interval = 1.5f) =>
            SetShowcaseTargetPressureInRange(attack,interval,100);

        internal float ShowcaseGuardMotionDuration(FormationDefender guard, string key) =>
            art.TryGetValue(FormationBodyArtId(guard), out var source) && source.Clips.TryGetValue(key, out var sequence)
                ? sequence.Duration : 0;

        internal void SetShowcaseTargetPressureInRange(float attack, float interval, float range)
        {
            if (Model == null || attack < 0 || float.IsNaN(attack) || float.IsInfinity(attack) ||
                interval <= 0 || float.IsNaN(interval) || float.IsInfinity(interval))
                throw new ArgumentOutOfRangeException("Invalid showcase target pressure.");
            showcaseTargetPressure = attack;
            for (int index = 0; index < Model.Defenders.Count; index++)
            {
                var target = Model.Defenders[index];
                target.Attack = index == 0 ? attack : 0;
                target.Range = index == 0 && attack > 0 ? range : 0;
                target.Interval = interval;
                // RequiredSP=0 means 'skill ready' in the actual defender model.
                // A very large requirement keeps this fixture on ordinary attacks.
                target.SkillRequired = float.MaxValue;
                target.SkillCharge = 0;
                target.NextAttackAt = index == 0 && attack > 0 ? Model.Clock + .2 : double.PositiveInfinity;
            }
        }

        internal void RestoreShowcaseTargetHealth()
        {
            if (Model == null || showcaseGuardArt != null) return;
            foreach (var target in Model.Defenders)
            {
                target.Health = target.MaxHealth;
                target.Alive = true;
            }
        }

        void PrepareShowcaseTargets()
        {
            Model.Defenders.Clear();
            float lastGuardColumn = FormationBattleModel.Width - 2;
            Vector2[] points = showcaseUseCurrentBoard
                ? new[] { new Vector2(lastGuardColumn - .55f, 3.2f), new Vector2(lastGuardColumn, 2.6f), new Vector2(lastGuardColumn, 4.15f) }
                : new[] { new Vector2(20.5f, 3.2f), new Vector2(21.7f, 2.8f), new Vector2(21.7f, 3.9f) };
            for (int index = 0; index < points.Length; index++)
                Model.Defenders.Add(new FormationDefender
                {
                    Id = 900000 + index,
                    ArtId = "D01",
                    Name = "展示靶（不反击）",
                    Position = points[index],
                    Facing = Vector2.left,
                    Alive = true,
                    MaxHealth = 10000000,
                    Health = 10000000,
                    Attack = 0,
                    Armor = 0,
                    MagicResistance = 0,
                    Range = 0,
                    Interval = float.MaxValue,
                    MoveSpeed = 0,
                    SkillRequired = float.MaxValue,
                    NextAttackAt = double.PositiveInfinity
                });
        }
    }

    public sealed partial class FormationBattleModel
    {
        internal FormationPresentationAction ShowcaseGuardVisualAction { get; private set; }
        internal FormationPresentationAction ShowcaseChenLaunch { get; private set; }

        internal void PrepareShowcaseGuard(string artId)
        {
            var node = Campaign.CurrentNode;
            var boss = FormationCampaignRules.Bosses.FirstOrDefault(entry => entry.NodeId == node.Id && entry.ArtId == artId);
            var cell = new Vector2Int(17, 3);
            var skillCell = new Vector2Int(16, 3);
            var targetCell = new Vector2Int(15, 3);
            var allyCell = new Vector2Int(17, 4);
            bool reserved = artId == "D23" || artId == "D28";
            if (Terrain != null && (!Terrain.CanPlaceDefender(cell) || !Terrain.IsWalkable(cell) ||
                reserved && (!Terrain.CanPlaceDefender(skillCell) || !Terrain.IsWalkable(skillCell) ||
                    Terrain.HeightAt(skillCell) != 0) || !Terrain.IsWalkable(targetCell) ||
                (artId == "D10" || artId == "D11" || artId == "D29") && !Terrain.CanPlaceDefender(allyCell)))
                throw new InvalidOperationException("Showcase guard fixture cells are unavailable.");
            foreach (var key in combat.Keys.Where(key => key >= 100).ToArray()) combat.Remove(key);
            Defenders.Clear();
            AddCampaignDefender(node, null, artId, cell, 1, boss);
            if (reserved) Defenders[0].SkillCell = skillCell;
            if (Defenders[0].Healer && artId != "D23" || artId == "D29")
            {
                AddCampaignDefender(node, null, "D01", allyCell, 1, null);
                Defenders[1].Attack = 0;
                Defenders[1].Range = 0;
                Defenders[1].MaxHealth = 100000;
            }
        }

        internal void BeginShowcaseGuard(string artId)
        {
            // StartBattle reprojects the campaign roster. Replace its freshly created
            // guards and immediately run their regular initialization before the first tick.
            ShowcaseGuardVisualAction = null;
            ShowcaseChenLaunch = null;
            PrepareShowcaseGuard(artId);
            BeginCampaignBattle();
            LimitSeconds = double.PositiveInfinity;
            var guard = Defenders[0];
            var target = Units.First(unit => unit.Enabled);
            target.Position = guard.ArtId switch
            {
                "D23" => (Vector2)guard.SkillCell.Value + Vector2.left * .5f,
                "D28" => (Vector2)guard.SkillCell.Value + Vector2.left * 1.7f,
                _ => guard.Position + Vector2.left * Mathf.Min(.8f, guard.Range * .8f)
            };
            // Separate the reviewed muzzle, flight and target from each other on the capture board.
            float reviewDistance = artId == "D24" ? 3.6f : artId == "D09" ? 2.8f : 0;
            if (reviewDistance > 0)
            {
                Vector2 reviewTarget = guard.Position + Vector2.left * Mathf.Min(reviewDistance, guard.Range * .95f);
                if (Terrain == null || Terrain.IsWalkable(reviewTarget)) target.Position = reviewTarget;
            }
            target.Facing = Vector2.right;
            target.Health = target.MaxHealth = 100000;
            target.NextAttackAt = double.PositiveInfinity;
            if (Defenders.Count > 1) Defenders[1].Health *= .55f;
            if (guard.ArtId == "D23") guard.Health *= .18f;
        }

        internal void QueueShowcaseSnowBossTransition()
        {
            // Stage the lethal threshold, then let an actual production projectile
            // hit resolve the transition and trigger the 2-second freeze.
            Defenders[0].Health = 1;
            Units.First(unit => unit.Enabled).NextAttackAt = Clock;
        }

        internal void PreviewShowcaseGuardMotion(string motion, float sourceSeconds)
        {
            var guard = Defenders[0];
            guard.LastActionAt = guard.LastSkillAt = Clock;
            guard.PresentationStartedAt = Clock;
            guard.PresentationDuration = sourceSeconds / FormationBattleController.FormationVisualRate;
            guard.PresentationRate = FormationBattleController.FormationVisualRate;
            guard.PresentationAction = motion;
            if (guard.ArtId == "D32")
            {
                guard.NextAttackAt = double.PositiveInfinity;
                guard.SkillRequired = float.MaxValue;
                var target = Units.First(unit => unit.Enabled);
                if (motion == "skill_begin")
                {
                    var kit = Kit(guard);
                    kit.ActiveUntil = Clock + 20;
                    double launch = Clock + sourceSeconds / guard.PresentationRate;
                    ShowcaseChenLaunch = new FormationPresentationAction
                    {
                        Sequence = ++presentationSequence, ActorId = guard.Id, Skill = true,
                        At = Clock, LaunchedAt = launch, ContactAt = launch, EndAt = kit.ActiveUntil,
                        Rate = guard.PresentationRate, Animation = motion, From = guard.Position,
                        To = guard.Position + guard.Facing
                    };
                }
                else if (motion == "skill_attack")
                {
                    double contact = Clock + (16d / 30) / guard.PresentationRate;
                    ShowcaseGuardVisualAction = new FormationPresentationAction
                    {
                        Sequence = ++presentationSequence, ActorId = guard.Id, Skill = true,
                        At = Clock, LaunchedAt = contact, ContactAt = contact, EndAt = Clock + guard.PresentationDuration,
                        Rate = guard.PresentationRate, Animation = motion, From = guard.Position,
                        To = target.Position, TargetId = target.Id
                    };
                }
            }
            else if (guard.ArtId == "D30" && motion == "skill_attack")
            {
                const double sourceContact = 32d / 60;
                double contact = Clock + sourceContact / guard.PresentationRate;
                ShowcaseGuardVisualAction = new FormationPresentationAction
                {
                    Sequence = ++presentationSequence, ActorId = guard.Id, Skill = true,
                    At = Clock, LaunchedAt = contact, ContactAt = contact,
                    EndAt = Clock + guard.PresentationDuration, Rate = guard.PresentationRate,
                    Animation = motion, From = guard.Position, To = guard.Position + guard.Facing * 2
                };
            }
        }

        // Source-animation review only: no combat simulation or skill/SP alteration.
        // This is deliberately labelled separately from the natural AI combat clips.
        internal void AdvanceShowcaseWalk(float seconds)
        {
            Clock += seconds;
            foreach(var unit in Units)
            {
                float travel=(float)Clock*unit.MoveSpeed;
                unit.GroundDistanceTravelled=travel;
                float leg=travel%4;
                unit.Position=new Vector2(15+(leg<=2?leg:4-leg),3);
                unit.Facing=leg<=2?Vector2.right:Vector2.left;
                unit.IsMoving=true;
                unit.ActionUntil=0;
                unit.PresentationAction=null;
            }
        }
    }
}
