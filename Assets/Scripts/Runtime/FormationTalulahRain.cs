using System;
using UnityEngine;

namespace YesterdayConvoy
{
    public sealed partial class FormationBattleController
    {
        const float TalulahSweepTime = 3.5f;
        const float TalulahSweepInterval = 2.1f;
        const float TalulahLaneStagger = .022f;
        Material talulahSweepMaterial;

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
            float marker = art.ContactTime(motion);
            if (float.IsNaN(marker)) return false;

            float rate = actor.PresentationRate > 0 ? actor.PresentationRate : LegacyVisualRate;
            float sourceDuration = art.Duration(motion);
            double raisedStart = Math.Min(marker + .05f, sourceDuration);
            double raisedEnd = Math.Min(marker + .50f, sourceDuration);
            double sustainedAt = actor.LastSkillAt + raisedStart / rate;
            double recoveryAt = rainEnd - (sourceDuration - raisedEnd) / rate;
            if (now < sustainedAt)
                age = Math.Max(0, now - actor.LastSkillAt) * rate;
            else if (now >= recoveryAt)
                age = raisedEnd + (now - recoveryAt) * rate;
            else
            {
                // The original raised-hand segment keeps the sword at rest while
                // the cape, hair and arm move. Ease through those existing frames
                // and finish on the same frame the original recovery begins from.
                double duration = recoveryAt - sustainedAt;
                double preferredPeriod = Math.PI * (raisedEnd - raisedStart) / rate;
                int cycles = Math.Max(0, (int)Math.Round(duration / preferredPeriod - .5));
                double wave = Math.PI * (2 * cycles + 1) * (now - sustainedAt) / duration;
                age = raisedStart + (raisedEnd - raisedStart) * .5 * (1 - Math.Cos(wave));
            }
            key = motion;
            loop = false;
            return true;
        }

        void PresentFormationTalulahRain(FormationBattleModel.LegacyZoneSnapshot zone, FormationActor source)
        {
            if (!ViewingDefenders || !UseFormationBattleCamera || Model.Clock < zone.At || Model.Clock >= zone.Until) return;
            var stream = BossRosterSupplementArt.Talulah("original-energy-stream");
            if (!stream.Sequence.Ready) return;
            if (talulahSweepMaterial == null)
            {
                var shader = Resources.Load<Shader>("TalulahGroundFlame");
                if (shader != null) talulahSweepMaterial = new Material(shader) { name = "Formation / Talulah ground flame detail" };
            }

            const float firstColumn = FormationBattleModel.BattleAreaStart + .10f;
            const float lastColumn = FormationBattleModel.Width - 1f;
            float zoneAge = (float)(Model.Clock - zone.At);
            float zoneLength = (float)(zone.Until - zone.At);
            float scale = LegacyScale(source);
            LegacyTalulah("target-burn-loop", zoneAge * LegacyVisualRate, zone.Center, source, -2, true);
            float finalStart = zoneLength - TalulahSweepTime - (FormationBattleModel.Height - 1) * TalulahLaneStagger;
            for (float startsAt = 0; startsAt <= finalStart && startsAt <= zoneAge; startsAt += TalulahSweepInterval)
            {
                for (int row = 0; row < FormationBattleModel.Height; row++)
                {
                    float sweepAge = zoneAge - startsAt - row * TalulahLaneStagger;
                    if (sweepAge < 0 || sweepAge >= TalulahSweepTime) continue;
                    float progress = sweepAge / TalulahSweepTime;
                    float frameAge = Mathf.Repeat(sweepAge * LegacyVisualRate, stream.Duration);
                    var cell = new Vector2(Mathf.Lerp(firstColumn, lastColumn, progress), row);
                    Vector3 point = ActorWorldPoint(cell) + battleCamera.transform.up * .12f;
                    var wave = LegacyDraw(stream.At(frameAge), talulahSweepMaterial ?? legacyHdrMaterial, point, cell,
                        scale * 1.15f, -5, hdr: stream.HdrRange);
                    if (wave != null) wave.color = new Color(1, 1, 1, Mathf.Clamp01((TalulahSweepTime - sweepAge) / .15f));
                }
            }
        }

        void PresentFormationTalulahRainHit(FormationHit hit, FormationActor source)
        {
            float age = (float)(Model.Clock - hit.At);
            if (age < 0 || age >= .29f) return;
            float scale = LegacyScale(source);
            Vector3 ground = ActorWorldPoint(hit.To) + Vector3.up * .035f;
            if (!IsFormationTalulahRainContact(hit))
            {
                var residue = BossRosterSupplementArt.Talulah("target-burn-loop");
                if (!residue.Sequence.Ready) return;
                var embers = LegacyDraw(residue.At(.20f + age * LegacyVisualRate), legacyHdrMaterial,
                    ground, hit.To, scale * .78f, -4, hdr: residue.HdrRange);
                if (embers != null) embers.color = new Color(1, .88f, .70f, .39f * (1 - age / .29f));
                return;
            }

            var impact = BossRosterSupplementArt.Talulah("original-fire-impact");
            if (!impact.Sequence.Ready) return;
            // Skip the transparent pre-roll so the first visible flash lands on
            // the same simulation step as the committed damage.
            var frame = impact.At(2f / impact.Sequence.Fps + age * LegacyVisualRate);
            var flash = LegacyDraw(frame, legacyHdrMaterial, ground, hit.To, scale * .93f, -7,
                hdr: impact.HdrRange, flat: true);
            if (flash != null)
            {
                flash.name = "Formation Talulah / rain impact";
                flash.color = new Color(1, .82f, .64f, Mathf.Clamp01((.29f - age) / .14f));
            }

            var target = FormationFeedbackActor(hit.ToId);
            var bodyPoint = FormationFeedbackContactPoint(target, hit.To, .48f);
            var blaze = LegacyDraw(frame, legacyHdrMaterial, bodyPoint, hit.To, scale * .69f, 35, hdr: impact.HdrRange);
            if (blaze != null)
            {
                blaze.name = "Formation Talulah / rain blaze";
                blaze.color = new Color(1, .96f, .82f, .88f * Mathf.Clamp01((.29f - age) / .12f));
            }
        }
    }
}
