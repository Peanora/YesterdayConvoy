using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Approved card-top threshold feedback, drawn over the existing lineup without controls.</summary>
    public static class SynergyActivationPresentation
    {
        const float BadgeDelay = 1f / 30f;
        const float Grow = 13f / 30f;
        const float Hold = 8f / 30f;
        const float Fade = 32f / 30f;
        const float EntryFlash = .25f;

        public static void DrawCard(Rect bounds, string cardId, SynergyActivationFeedback.Batch batch)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint || batch == null ||
                bounds.width <= 0 || bounds.height <= 0 || string.IsNullOrEmpty(cardId)) return;
            int count = 0;
            SynergyActivationFeedback.Pulse first = null;
            for (int i = 0; i < batch.Activations.Count; i++)
            {
                var pulse = batch.Activations[i];
                if (!pulse.ContainsMember(cardId)) continue;
                if (first == null) first = pulse;
                count++;
            }
            if (count == 0 || batch.Age < 0 || batch.Age >= batch.Duration) return;

            Color previousColor = GUI.color;
            Matrix4x4 previousMatrix = GUI.matrix;
            try
            {
                var grade = SynergyBadgePresentation.GradeFor(first.Name, first.Tier);
                var edge = Color.Lerp(SynergyBadgePresentation.ColorFor(grade), Color.white, .45f);
                if (batch.IsAddedMember(cardId) && batch.Age < EntryFlash)
                {
                    float entryAlpha = Mathf.Sin(Mathf.PI * Mathf.Pow(batch.Age / EntryFlash, .7f));
                    Border(bounds, edge, entryAlpha * .11f, 4, previousColor);
                    Border(Inset(bounds, 1), Color.white, entryAlpha * .95f, 1.35f, previousColor);
                }

                float elapsed = batch.Age - BadgeDelay;
                if (elapsed <= 0) return;
                float growth = Mathf.Clamp01(elapsed / Grow);
                float fade = Mathf.Clamp01((elapsed - Grow - Hold) / Fade);
                float alpha = Smooth(growth) * (1 - Smooth(fade));
                float scale = Mathf.Lerp(.28f, 1, 1 - Mathf.Pow(1 - growth, 3)) * Mathf.Lerp(1, .86f, Smooth(fade));
                float edgeAlpha = alpha * Mathf.Exp(-Mathf.Max(0, elapsed - .12f) * 3.2f) * .32f;
                DrawTopEdge(bounds, edge, edgeAlpha, previousColor);

                // Simultaneous bonds share one sound/batch. Their badges occupy separate positions
                // over the same card, instead of overwriting one another or extending into adjacent slots.
                const float gap = 2;
                float size = Mathf.Min(bounds.width * .68f, (bounds.width - 4 - gap * (count - 1)) / count);
                int index = 0;
                for (int i = 0; i < batch.Activations.Count; i++)
                {
                    var pulse = batch.Activations[i];
                    if (!pulse.ContainsMember(cardId)) continue;
                    var center = new Vector2(bounds.center.x + (index - (count - 1) * .5f) * (size + gap), bounds.yMin);
                    DrawBadge(center, size * scale, pulse, growth, fade, alpha, previousColor);
                    index++;
                }
            }
            finally
            {
                GUI.color = previousColor;
                GUI.matrix = previousMatrix;
            }
        }

        static float Smooth(float value) => value * value * (3 - 2 * value);
        static Rect Inset(Rect r, float amount) => new Rect(r.x + amount, r.y + amount, r.width - amount * 2, r.height - amount * 2);
        static Rect Centered(Vector2 center, float size) => new Rect(center.x - size * .5f, center.y - size * .5f, size, size);

        static void Tint(Color parent, Color color, float alpha)
        {
            GUI.color = new Color(parent.r * color.r, parent.g * color.g, parent.b * color.b, parent.a * color.a * alpha);
        }

        static void Fill(Rect bounds, Color color, float alpha, Color parent)
        {
            Tint(parent, color, alpha);
            GUI.DrawTexture(bounds, Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
        }

        static void Border(Rect bounds, Color color, float alpha, float width, Color parent)
        {
            Fill(new Rect(bounds.x, bounds.y, bounds.width, width), color, alpha, parent);
            Fill(new Rect(bounds.x, bounds.yMax - width, bounds.width, width), color, alpha, parent);
            Fill(new Rect(bounds.x, bounds.y, width, bounds.height), color, alpha, parent);
            Fill(new Rect(bounds.xMax - width, bounds.y, width, bounds.height), color, alpha, parent);
        }

        static void DrawTopEdge(Rect bounds, Color color, float alpha, Color parent)
        {
            if (alpha <= 0) return;
            Fill(new Rect(bounds.x + 1, bounds.y, bounds.width - 2, 3), color, alpha * .15f, parent);
            Fill(new Rect(bounds.x + 1, bounds.y + 1, bounds.width - 2, .85f), color, alpha, parent);
            Fill(new Rect(bounds.x + 1, bounds.y + 1, .8f, bounds.height * .26f), color, alpha, parent);
            Fill(new Rect(bounds.xMax - 2, bounds.y + 1, .8f, bounds.height * .26f), color, alpha, parent);
        }

        static void DrawBadge(Vector2 center, float size, SynergyActivationFeedback.Pulse pulse,
            float growth, float fade, float alpha, Color parent)
        {
            if (size <= 0 || alpha <= 0) return;
            var grade = SynergyBadgePresentation.GradeFor(pulse.Name, pulse.Tier);
            var frame = SynergyBadgePresentation.FrameFor(grade);
            var glyph = SynergyBadgePresentation.IconFor(pulse.Name);
            if (frame == null || glyph == null) return;
            Matrix4x4 previous = GUI.matrix;
            try
            {
                float turn = (1 - Smooth(growth)) * -13.75f + Mathf.Sin(fade * Mathf.PI) * 3.15f;
                GUIUtility.RotateAroundPivot(turn, center);
                var tint = SynergyBadgePresentation.ColorFor(grade);
                if (growth < .85f)
                {
                    float fragments = Mathf.Sin(Mathf.PI * growth) * .65f;
                    for (int i = 0; i < 4; i++)
                    {
                        float angle = i * Mathf.PI * .5f + .5f - growth * .5f;
                        float distance = size * (.57f - growth * .1f);
                        Vector2 p = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                        float segment = Mathf.Max(.6f, size * .034f);
                        Line(p + new Vector2(-segment, segment), p + new Vector2(segment, -segment), tint, fragments, .7f, parent);
                        Line(p + new Vector2(segment, -segment), p + new Vector2(segment * 2, segment * .7f), tint, fragments, .7f, parent);
                    }
                }
                // Reuse the already approved translucent plate at three radii; no per-frame textures,
                // material instances, counts or text are introduced by this feedback.
                Tint(parent, Color.white, alpha * .035f);
                GUI.DrawTexture(Centered(center, size * 1.13f), frame, ScaleMode.StretchToFill, true);
                Tint(parent, Color.white, alpha * .065f);
                GUI.DrawTexture(Centered(center, size * 1.05f), frame, ScaleMode.StretchToFill, true);
                Tint(parent, Color.white, alpha * .23f);
                GUI.DrawTexture(Centered(center, size), frame, ScaleMode.StretchToFill, true);
                Diamond(center, size * .92f, grade, alpha * .60f, parent);
                Diamond(center, size * .79f, grade, alpha * .60f, parent);
                Tint(parent, Color.white, alpha);
                GUI.DrawTexture(Centered(center, size * (.38f / .68f)), glyph, ScaleMode.StretchToFill, true);
            }
            finally { GUI.matrix = previous; }
        }

        static void Diamond(Vector2 center, float size, SynergyVisualGrade grade, float alpha, Color parent)
        {
            float half = size * .5f;
            Vector2 top = center + Vector2.up * -half, right = center + Vector2.right * half;
            Vector2 bottom = center + Vector2.up * half, left = center + Vector2.right * -half;
            var upper = Color.Lerp(SynergyBadgePresentation.ColorFor(grade, .25f), Color.white, .4f);
            var lower = Color.Lerp(SynergyBadgePresentation.ColorFor(grade, .75f), Color.white, .4f);
            Line(top, right, upper, alpha * .1f, 3, parent);
            Line(left, top, upper, alpha * .1f, 3, parent);
            Line(right, bottom, lower, alpha * .1f, 3, parent);
            Line(bottom, left, lower, alpha * .1f, 3, parent);
            Line(top, right, upper, alpha, .65f, parent);
            Line(left, top, upper, alpha, .65f, parent);
            Line(right, bottom, lower, alpha, .65f, parent);
            Line(bottom, left, lower, alpha, .65f, parent);
        }

        static void Line(Vector2 a, Vector2 b, Color color, float alpha, float width, Color parent)
        {
            Vector2 direction = b - a;
            if (direction.sqrMagnitude <= .00001f) return;
            Matrix4x4 previous = GUI.matrix;
            try
            {
                GUI.matrix = previous * Matrix4x4.TRS(a, Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg), Vector3.one);
                Fill(new Rect(0, -width * .5f, direction.magnitude, width), color, alpha, parent);
            }
            finally { GUI.matrix = previous; }
        }
    }
}
