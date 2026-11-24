using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        const float BattleRewardFall = .32f, BattleRewardBounce = .16f, BattleRewardLifetime = .93f;

        sealed class BattleRewardDrop
        {
            public Transform Root;
            public MeshRenderer Crystal, Contact, Ring;
            public Vector3 LandingOffset;
            public double StartsAt;
        }

        readonly List<BattleRewardDrop> battleRewardDrops = new List<BattleRewardDrop>();
        readonly HashSet<int> shownGuardRewardDrops = new HashSet<int>();
        MaterialPropertyBlock battleRewardTint;
        Mesh battleRewardRing;
        internal int ActiveBattleRewardDrops => battleRewardDrops.Count;
        internal int BattleRewardDropsShown => shownGuardRewardDrops.Count;

        void TickBattleRewardDrops()
        {
            if (!CombatViewActive)
            {
                foreach (var drop in battleRewardDrops) RemoveBattleRewardDrop(drop);
                battleRewardDrops.Clear();
                shownGuardRewardDrops.Clear();
                return;
            }
            if (Campaign == null || Campaign.IsPractice) return;
            double now = Model.Clock;
            int limit = Campaign.GuardRewardCueLimit(Model.EncounterProgress, Model.InitialDefenderCount);
            if (shownGuardRewardDrops.Count < limit)
            {
                foreach (var guard in Model.Defenders.Where(guard => !guard.Alive && guard.LastDeathAt >= 0 &&
                    !shownGuardRewardDrops.Contains(guard.Id) && Model.IsInitialGuard(guard)).OrderBy(guard => guard.LastDeathAt))
                {
                    double startsAt = guard.LastDeathAt + GuardDeathDuration(guard);
                    if (now < startsAt || now >= startsAt + BattleRewardLifetime) continue;
                    shownGuardRewardDrops.Add(guard.Id);
                    ShowBattleRewardDrop(guard, startsAt);
                    if (shownGuardRewardDrops.Count >= limit) break;
                }
            }
            for (int index = battleRewardDrops.Count - 1; index >= 0; index--)
            {
                var drop = battleRewardDrops[index];
                float age = (float)(now - drop.StartsAt);
                if (age >= BattleRewardLifetime)
                {
                    RemoveBattleRewardDrop(drop);
                    battleRewardDrops.RemoveAt(index);
                }
                else PresentBattleRewardDrop(drop, age);
            }
        }

        void ShowBattleRewardDrop(FormationDefender guard, double startsAt)
        {
            EnsureRewardGeometry();
            battleRewardTint ??= new MaterialPropertyBlock();
            if (battleRewardRing == null) battleRewardRing = RewardRing(1, 0);
            var root = new GameObject("Guard reward drop").transform;
            root.SetParent(transform, false);
            root.position = ActorWorldPoint(guard) + Vector3.up * .025f;
            // Keep the landing point on the same surface as the guard's feet.
            Vector2 tileCenter = Vector2Int.RoundToInt(guard.Position);
            Vector2 direction = new Vector2(guard.Id % 2 == 0 ? .23f : -.23f, -.14f);
            Vector2 nudge = guard.Position + direction - tileCenter;
            nudge = new Vector2(Mathf.Clamp(nudge.x, -.34f, .34f), Mathf.Clamp(nudge.y, -.34f, .34f));
            Vector2 travel = tileCenter + nudge - guard.Position;
            var drop = new BattleRewardDrop
            {
                Root = root,
                Crystal = RewardMeshChild("Falling crystal", root, RewardShell("白"), rewardMaterial),
                Contact = RewardMeshChild("Ground contact", root, rewardShadowMesh, rewardDecal),
                Ring = RewardMeshChild("Landing ring", root, battleRewardRing, rewardDecal),
                LandingOffset = new Vector3(travel.x, 0, travel.y),
                StartsAt = startsAt
            };
            drop.Contact.transform.localPosition = drop.LandingOffset;
            drop.Ring.transform.localPosition = drop.LandingOffset + Vector3.up * .006f;
            battleRewardDrops.Add(drop);
        }

        void PresentBattleRewardDrop(BattleRewardDrop drop, float age)
        {
            const float radius = .16f;
            float falling = Mathf.Clamp01(age / BattleRewardFall);
            float landed = Mathf.Max(0, age - BattleRewardFall);
            float bounce = Mathf.Sin(Mathf.PI * Mathf.Clamp01(landed / BattleRewardBounce)) * .115f;
            float remaining = 1 - Mathf.InverseLerp(.53f, BattleRewardLifetime, age);
            float size = radius * Mathf.SmoothStep(0, 1, remaining);
            drop.Crystal.transform.localPosition = falling * drop.LandingOffset +
                Vector3.up * (size + .64f * (1 - falling * falling) + bounce);
            drop.Crystal.transform.localScale = Vector3.one * size;
            drop.Crystal.transform.localRotation = Quaternion.Euler(18 + falling * 155, age * 195, -16);

            battleRewardTint.Clear();
            battleRewardTint.SetFloat("_Boost", 1.07f + .48f * Mathf.Clamp01(1 - landed / .13f) * falling);
            drop.Crystal.SetPropertyBlock(battleRewardTint);
            drop.Contact.transform.localScale = Vector3.one * Mathf.Lerp(.27f, .39f, falling);
            battleRewardTint.Clear();
            battleRewardTint.SetColor("_Color", new Color(1, 1, 1, remaining * Mathf.Lerp(.42f, .90f, falling)));
            drop.Contact.SetPropertyBlock(battleRewardTint);

            float ringAge = Mathf.Clamp01(landed / .34f);
            drop.Ring.enabled = age >= BattleRewardFall && ringAge < 1;
            if (!drop.Ring.enabled) return;
            drop.Ring.transform.localScale = Vector3.one * Mathf.Lerp(.20f, .67f, 1 - Mathf.Pow(1 - ringAge, 2));
            battleRewardTint.SetColor("_Color", new Color(1, 1, 1, .95f * (1 - ringAge)));
            drop.Ring.SetPropertyBlock(battleRewardTint);
        }

        void RemoveBattleRewardDrop(BattleRewardDrop drop)
        {
            drop.Root.gameObject.SetActive(false);
            Destroy(drop.Root.gameObject);
        }
    }
}
