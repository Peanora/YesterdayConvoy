using System;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleModel
    {
        // The screen fire enters from the left of each board row. A living
        // defender intercepts that row before a unit farther to the right.
        internal FormationDefender[] TalulahRainVictims(int cap) => Defenders
            .Where(defender => defender.Alive)
            .GroupBy(defender => Mathf.Clamp(Mathf.RoundToInt(defender.Position.y), 0, Height - 1))
            .Select(row => row.OrderBy(defender => defender.Position.x).ThenBy(defender => defender.Id).First())
            .OrderBy(defender => defender.Position.y).ThenBy(defender => defender.Id)
            .Take(cap).ToArray();

        internal float TalulahRainStopColumn(int row)
        {
            var blocker = Defenders.Where(defender => defender.Alive &&
                Mathf.Clamp(Mathf.RoundToInt(defender.Position.y), 0, Height - 1) == row)
                .OrderBy(defender => defender.Position.x).ThenBy(defender => defender.Id).FirstOrDefault();
            return blocker?.Position.x ?? Width - .5f;
        }
    }

    public sealed partial class FormationBattleController
    {
        // BsnakeScreenAttack uses projectile_enemy_bsnake. Its source moves at
        // 2.5 world units/s and spawns a projectile in each row within .1 s.
        const float TalulahSourceProjectileSpeed = 2.5f;
        const float TalulahSourceScale = 1.60714286f;
        const float TalulahSourceWaveInterval = 2f;
        const float TalulahScreenSpawnWindow = .1f;
        const string TalulahLeftFireKey = "original-fire-screen-left";
        const string TalulahLeftFireName = "Formation Talulah / left screen fire";

        bool IsFormationTalulahRainContact(FormationHit hit)
        {
            if(hit.SourceArtId!="TALULAH_TRIAL"||Campaign?.TalulahCost!=5||!hit.Skill||hit.Secondary)return false;
            var cast=hit.Action;
            // The six direct segments occur within the opening two seconds. The
            // secondary burn and three-star periodic bonus own no impact flash.
            return cast!=null&&cast.Skill&&hit.At<cast.ContactAt+2;
        }

        bool PresentFormationTalulahRainPose(FormationUnitArt art, FormationActor actor, int phase, double now,
            ref string key, ref double age, ref bool loop)
        {
            double rainEnd = Model.TalulahRainEnd(actor);
            if (now >= rainEnd || actor.LastSkillAt < actor.LastDeathAt) return false;
            string motion = phase > 0 ? "Skill_2_B" : "Skill_2_A";
            if (!art.HasAction(motion)) return false;
            float rate = actor.PresentationRate > 0 ? actor.PresentationRate : LegacyVisualRate;
            double elapsed = Math.Max(0, now - actor.LastSkillAt);
            double castDuration = art.Duration(motion) / rate;
            // The real Skill_2 recovery stays at source speed. The v0.7 burn
            // continues under the corresponding ordinary idle animation.
            loop = elapsed >= castDuration;
            key = loop ? (phase > 0 ? "Idle_B" : "idle") : motion;
            age = loop ? (elapsed - castDuration) * LegacyVisualRate : elapsed * rate;
            return true;
        }

        void PresentFormationTalulahRain(FormationBattleModel.LegacyZoneSnapshot zone, FormationActor source)
        {
            if (!ViewingDefenders || !UseFormationBattleCamera || Model.Clock < zone.At || Model.Clock >= zone.Until) return;
            var stream = BossRosterSupplementArt.Talulah("original-fire-stream");
            if (!stream.Sequence.Ready) return;
            const float firstColumn = FormationBattleModel.BattleAreaStart;
            const float lastColumn = FormationBattleModel.Width - .5f;
            const float worldSpeed = TalulahSourceProjectileSpeed * TalulahSourceScale;
            double zoneAge = Model.Clock - zone.At;
            float scale = LegacyScale(source);
            for (float startsAt = 0; startsAt <= zoneAge; startsAt += TalulahSourceWaveInterval / LegacyVisualRate)
            {
                for (int row = 0; row < FormationBattleModel.Height; row++)
                {
                    // The native random spawn offset is distributed deterministically
                    // over the current board rows so capture and pause agree.
                    float laneDelay = row * TalulahScreenSpawnWindow / (FormationBattleModel.Height - 1);
                    double sourceAge = (zoneAge - startsAt) * LegacyVisualRate - laneDelay;
                    if (sourceAge < 0) continue;
                    float column = firstColumn + (float)sourceAge * worldSpeed;
                    float stopColumn = Mathf.Min(lastColumn, Model.TalulahRainStopColumn(row));
                    if (column >= stopColumn) continue;
                    var cell = new Vector2(column, row);
                    var point = ActorWorldPoint(cell) + Vector3.up * .025f;
                    LegacyDraw(stream.At(sourceAge), legacyHdrMaterial, point, cell, scale, -5, hdr: stream.HdrRange);
                }
            }
        }

        void PresentFormationTalulahLeftFire(FormationBattleModel.LegacyZoneSnapshot zone)
        {
            if (!ViewingDefenders || !UseFormationBattleCamera || battleCamera == null) return;
            var clip = BossRosterSupplementArt.Talulah(TalulahLeftFireKey);
            if (!clip.Sequence.Ready) return;
            var frame = clip.At((Model.Clock - zone.At) * LegacyVisualRate);
            if (frame == null) return;

            // Reserve the edge of the screen for the source fire, independently of the lanes.
            // The small offscreen edge reads as a continuous front while the HUD stays clear.
            const float leftEdge = -.018f;
            const float maxWidth = .40f;
            const float maxHeight = .80f;
            float depth = battleCamera.nearClipPlane + 1;
            var screenSpan = battleCamera.ViewportToWorldPoint(new Vector3(1, 1, depth)) -
                battleCamera.ViewportToWorldPoint(new Vector3(0, 0, depth));
            float width = Vector3.Dot(screenSpan, battleCamera.transform.right);
            float height = Vector3.Dot(screenSpan, battleCamera.transform.up);
            float scale = Mathf.Min(maxWidth * width / frame.bounds.size.x, maxHeight * height / frame.bounds.size.y);
            float x = leftEdge - frame.bounds.min.x * scale / width;
            float y = .5f - frame.bounds.center.y * scale / height;
            var point = battleCamera.ViewportToWorldPoint(new Vector3(x, y, depth));
            var visibleCell = new Vector2(FormationBattleModel.BattleAreaStart, (FormationBattleModel.Height - 1) * .5f);
            var effect = LegacyDraw(frame, legacyHdrMaterial, point, visibleCell, scale, 600, hdr: clip.HdrRange, gammaOutput: true);
            if (effect != null) effect.name = TalulahLeftFireName;
        }

        void PresentFormationTalulahHit(FormationHit hit, FormationFighter source)
        {
            if (hit.Secondary) return;
            double age = (Model.Clock - hit.At) * LegacyVisualRate;
            bool reborn = (hit.Action?.NativePhase ?? source.NativePhase) > 0;
            var target = FormationFeedbackActor(hit.ToId);
            var point = FormationFeedbackContactPoint(target, hit.To, .48f);
            if (!hit.Skill)
            {
                LegacyTalulah(reborn ? "attack-B-hit" : "attack-A-hit", age, hit.To, source, 35, position: point);
                return;
            }
            if (hit.Action?.Skill != true) return;

            // Ignite's original _hitEffects are attached at the actual hit.
            if (Campaign.TalulahCost <= 3)
            {
                LegacyTalulah(reborn ? "ignite-B-target" : "ignite-A-target", age, hit.To, source, 35, position: point);
                return;
            }
            if (!IsFormationTalulahRainContact(hit)) return;
            var clip = BossRosterSupplementArt.Talulah("original-fire-impact");
            if (!clip.Sequence.Ready) return;
            var ground = ActorWorldPoint(hit.To) + Vector3.up * .025f;
            var impact = LegacyDraw(clip.At(age), legacyHdrMaterial, ground, hit.To, LegacyScale(source), 35, hdr: clip.HdrRange);
            if (impact != null) impact.name = "Formation Talulah / rain impact";
        }
    }
}
