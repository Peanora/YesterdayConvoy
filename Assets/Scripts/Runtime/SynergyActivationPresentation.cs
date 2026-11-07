using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Approved V4 threshold presentation over the existing lineup; no input or battle state.</summary>
    public static class SynergyActivationPresentation
    {
        const float Lifetime = 1.78f;
        // The approved canvas used 140px-wide cards. All lengths follow the actual IMGUI slot width.
        const float ReferenceCardWidth = 140;
        static Texture2D softLine, softHalo;

        struct Motion
        {
            public float OuterDegrees, InnerDegrees, Scale, Squash, Coverage, Fade;
            public float Border, Badge, GlyphAlpha, GlyphScale;
        }

        public static void DrawCard(Rect bounds, string cardId, SynergyActivationFeedback.Batch batch)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint || batch == null ||
                bounds.width <= 0 || bounds.height <= 0 || string.IsNullOrEmpty(cardId) ||
                batch.Age < 0 || batch.Age >= Lifetime || batch.Age >= batch.Duration ||
                batch.Activations.Count == 0) return;

            // The feedback queue presents one bond at a time. Its members share one full-size badge.
            var pulse = batch.Activations[0];
            if (!pulse.ContainsMember(cardId)) return;
            var frame = SynergyBadgePresentation.FrameFor(SynergyBadgePresentation.GradeFor(pulse.Name, pulse.Tier));
            var glyph = SynergyBadgePresentation.IconFor(pulse.Name);
            if (frame == null || glyph == null) return;

            Color previousColor = GUI.color;
            Matrix4x4 previousMatrix = GUI.matrix;
            try
            {
                EnsureSoftTextures();
                var grade = SynergyBadgePresentation.GradeFor(pulse.Name, pulse.Tier);
                float unit = bounds.width / ReferenceCardWidth;
                var motion = Evaluate(batch.Age);
                DrawBorder(bounds, grade, batch.Age, motion.Border, unit, previousColor);
                DrawBadge(bounds, frame, glyph, grade, batch.Age, motion, unit, previousColor);
            }
            finally
            {
                GUI.color = previousColor;
                GUI.matrix = previousMatrix;
            }
        }

        static float Smooth(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3 - 2 * value);
        }

        static float Ease(float value) => 1 - Mathf.Pow(1 - Mathf.Clamp01(value), 3);

        static Motion Evaluate(float time)
        {
            float turn = Mathf.Clamp01((time - .04f) / .34f);
            float fade = Smooth((time - .72f) / 1.06f);
            float outer = Mathf.Lerp(-64, 6, 1 - Mathf.Pow(1 - turn, 2));
            if (time > .38f) outer = Mathf.Lerp(6, 0, Smooth((time - .38f) / .20f));
            float inner = Mathf.Lerp(32, -4, Smooth((time - .06f) / .35f));
            if (time > .41f) inner = Mathf.Lerp(-4, 0, Smooth((time - .41f) / .21f));
            float scale = time < .27f ? Mathf.Lerp(.68f, 1.055f, Ease(time / .27f)) :
                Mathf.Lerp(1.055f, 1, Smooth((time - .27f) / .25f));
            return new Motion
            {
                OuterDegrees = outer,
                InnerDegrees = inner,
                Scale = scale * (1 - .07f * fade),
                Squash = Mathf.Lerp(.84f, 1, Smooth(time / .42f)),
                Coverage = Mathf.Lerp(.33f, 1, Smooth(time / .23f)),
                Fade = fade,
                Border = Smooth(time / .08f) * (1 - Smooth((time - .62f) / 1.08f)) *
                    (.83f + .17f * Mathf.Exp(-Mathf.Pow((time - .18f) / .11f, 2))),
                Badge = Smooth((time - .01f) / .09f) * (1 - fade),
                GlyphAlpha = Smooth((time - .13f) / .19f) * (1 - fade),
                GlyphScale = Mathf.Lerp(.76f, 1, Ease((time - .13f) / .23f)) * (1 - .06f * fade)
            };
        }

        static void EnsureSoftTextures()
        {
            // Only first use uploads these tiny cached alpha masks. No frame allocates textures/materials.
            if (softLine == null)
            {
                softLine = new Texture2D(1, 64, TextureFormat.RGBA32, false)
                {
                    name = "Synergy activation soft line", hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
                };
                var pixels = new Color[64];
                for (int y = 0; y < pixels.Length; y++)
                {
                    float distance = (y + .5f - 32) / 10.666667f;
                    pixels[y] = new Color(1, 1, 1, Mathf.Exp(-.5f * distance * distance));
                }
                softLine.SetPixels(pixels);
                softLine.Apply(false, true);
            }
            if (softHalo == null)
            {
                softHalo = new Texture2D(64, 64, TextureFormat.RGBA32, false)
                {
                    name = "Synergy activation soft halo", hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
                };
                var pixels = new Color[64 * 64];
                for (int y = 0; y < 64; y++)
                    for (int x = 0; x < 64; x++)
                    {
                        float dx = (x + .5f - 32) / 32, dy = (y + .5f - 32) / 32;
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = distance < .5f ? Mathf.Lerp(1, .08f / .19f, distance * 2) :
                            Mathf.Lerp(.08f / .19f, 0, (distance - .5f) * 2);
                        pixels[y * 64 + x] = new Color(1, 1, 1, distance >= 1 ? 0 : alpha);
                    }
                softHalo.SetPixels(pixels);
                softHalo.Apply(false, true);
            }
        }

        static void Tint(Color parent, Color color, float alpha)
        {
            GUI.color = new Color(parent.r * color.r, parent.g * color.g, parent.b * color.b, parent.a * color.a * alpha);
        }

        static void Fill(Rect bounds, Color color, float alpha, Color parent, Texture2D texture = null)
        {
            if (alpha <= .0001f || bounds.width <= 0 || bounds.height <= 0) return;
            Tint(parent, color, alpha);
            GUI.DrawTexture(bounds, texture != null ? texture : Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
        }

        static Color GradeColor(SynergyVisualGrade grade, float vertical, float white = 0) =>
            Color.Lerp(SynergyBadgePresentation.ColorFor(grade, vertical), Color.white, white);

        static void Line(Vector2 a, Vector2 b, Color color, float alpha, float width, Color parent, Texture2D texture = null)
        {
            Vector2 direction = b - a;
            if (direction.sqrMagnitude <= .000001f || alpha <= .0001f || width <= 0) return;
            Matrix4x4 previous = GUI.matrix;
            try
            {
                GUI.matrix = previous * Matrix4x4.TRS(a,
                    Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg), Vector3.one);
                Fill(new Rect(0, -width * .5f, direction.magnitude, width), color, alpha, parent, texture);
            }
            finally { GUI.matrix = previous; }
        }

        static void GlowingLine(Vector2 a, Vector2 b, SynergyVisualGrade grade, float vertical,
            float alpha, float width, float blur, float white, Color parent)
        {
            if (blur > 0)
                Line(a, b, GradeColor(grade, vertical), alpha * .30f, width + blur * 2, parent, softLine);
            Line(a, b, GradeColor(grade, vertical, white), alpha, width, parent);
        }

        static void RectLayer(Rect bounds, SynergyVisualGrade grade, float alpha, float width,
            float blur, float white, Color parent)
        {
            GlowingLine(new Vector2(bounds.xMin, bounds.yMin), new Vector2(bounds.xMax, bounds.yMin),
                grade, 0, alpha, width, blur, white, parent);
            GlowingLine(new Vector2(bounds.xMax, bounds.yMax), new Vector2(bounds.xMin, bounds.yMax),
                grade, 1, alpha, width, blur, white, parent);
            int bands = grade == SynergyVisualGrade.Prismatic ? 6 : 1;
            for (int i = 0; i < bands; i++)
            {
                float from = (float)i / bands, to = (float)(i + 1) / bands;
                float y1 = Mathf.Lerp(bounds.yMin, bounds.yMax, from), y2 = Mathf.Lerp(bounds.yMin, bounds.yMax, to);
                GlowingLine(new Vector2(bounds.xMin, y1), new Vector2(bounds.xMin, y2),
                    grade, (from + to) * .5f, alpha, width, blur, white, parent);
                GlowingLine(new Vector2(bounds.xMax, y1), new Vector2(bounds.xMax, y2),
                    grade, (from + to) * .5f, alpha, width, blur, white, parent);
            }
        }

        static Vector2 PerimeterPoint(Rect r, float distance)
        {
            float perimeter = 2 * (r.width + r.height);
            distance = Mathf.Repeat(distance, perimeter);
            if (distance < r.width) return new Vector2(r.xMin + distance, r.yMin);
            distance -= r.width;
            if (distance < r.height) return new Vector2(r.xMax, r.yMin + distance);
            distance -= r.height;
            if (distance < r.width) return new Vector2(r.xMax - distance, r.yMax);
            return new Vector2(r.xMin, r.yMax - (distance - r.width));
        }

        static void PerimeterSegment(Rect r, float from, float length, SynergyVisualGrade grade,
            float alpha, float width, float blur, float white, Color parent)
        {
            if (length <= 0 || r.width <= 0 || r.height <= 0) return;
            float perimeter = 2 * (r.width + r.height);
            float position = Mathf.Repeat(from, perimeter), remaining = length;
            int edge = position < r.width ? 0 : position < r.width + r.height ? 1 :
                position < 2 * r.width + r.height ? 2 : 3;
            // A front is 14% of the perimeter: straight spans stay whole and only real corners split it.
            for (int spans = 0; spans < 8 && remaining > .00001f; spans++)
            {
                float corner = edge == 0 ? r.width : edge == 1 ? r.width + r.height :
                    edge == 2 ? 2 * r.width + r.height : perimeter;
                float run = Mathf.Min(remaining, Mathf.Max(0, corner - position));
                if (run > .00001f)
                    PerimeterSpan(PerimeterPoint(r, position), PerimeterPoint(r, position + run), r,
                        grade, alpha, width, blur, white, parent);
                remaining -= run;
                position = edge == 3 ? 0 : corner;
                edge = (edge + 1) & 3;
            }
        }

        static void PerimeterSpan(Vector2 a, Vector2 b, Rect r, SynergyVisualGrade grade,
            float alpha, float width, float blur, float white, Color parent)
        {
            if (grade != SynergyVisualGrade.Prismatic || Mathf.Abs(a.y - b.y) <= .00001f)
            {
                float vertical = Mathf.Clamp01(((a.y + b.y) * .5f - r.yMin) / r.height);
                GlowingLine(a, b, grade, vertical, alpha, width, blur, white, parent);
                return;
            }
            // Only a prismatic vertical span needs the existing six top-to-bottom color bands.
            float minimum = Mathf.Min(a.y, b.y), maximum = Mathf.Max(a.y, b.y);
            for (int band = 0; band < 6; band++)
            {
                float from = Mathf.Max(minimum, r.yMin + r.height * band / 6);
                float to = Mathf.Min(maximum, r.yMin + r.height * (band + 1) / 6);
                if (to <= from) continue;
                var first = new Vector2(a.x, a.y <= b.y ? from : to);
                var last = new Vector2(a.x, a.y <= b.y ? to : from);
                float vertical = Mathf.Clamp01(((from + to) * .5f - r.yMin) / r.height);
                GlowingLine(first, last, grade, vertical, alpha, width, blur, white, parent);
            }
        }

        static void Glint(Vector2 center, float arm, SynergyVisualGrade grade, float vertical,
            float alpha, float width, float blur, Color parent)
        {
            float diameter = blur * 2;
            Fill(new Rect(center.x - blur, center.y - blur, diameter, diameter), GradeColor(grade, vertical),
                alpha * .18f, parent, softHalo);
            GlowingLine(center - Vector2.right * arm, center + Vector2.right * arm,
                grade, vertical, alpha, width, blur * .45f, .94f, parent);
            GlowingLine(center - Vector2.up * arm, center + Vector2.up * arm,
                grade, vertical, alpha, width, blur * .45f, .94f, parent);
        }

        static void DrawBorder(Rect bounds, SynergyVisualGrade grade, float time, float alpha, float unit, Color parent)
        {
            if (alpha <= .001f) return;
            var box = new Rect(bounds.x + unit * 2, bounds.y + unit * 2, bounds.width - unit * 4, bounds.height - unit * 4);
            RectLayer(box, grade, alpha * .30f, 5 * unit, 19 * unit, 0, parent);
            RectLayer(box, grade, alpha * .65f, 3.2f * unit, 9 * unit, 0, parent);
            RectLayer(box, grade, alpha * .95f, 2.2f * unit, 3 * unit, .50f, parent);
            RectLayer(box, grade, alpha, .9f * unit, 0, .94f, parent);

            float sweep = Mathf.Clamp01(time / .47f);
            if (sweep < 1)
            {
                float perimeter = 2 * (box.width + box.height), head = box.width * .5f, tail = perimeter * .14f;
                float light = alpha * (1 - Smooth((sweep - .72f) / .28f));
                PerimeterSegment(box, head + sweep * perimeter * .5f - tail, tail,
                    grade, light, 2.8f * unit, 13 * unit, .94f, parent);
                PerimeterSegment(box, head - sweep * perimeter * .5f, tail,
                    grade, light, 2.8f * unit, 13 * unit, .94f, parent);
            }

            float sparkle = Mathf.Sin(Mathf.PI * Mathf.Clamp01(time / .65f));
            if (sparkle <= 0) return;
            for (int i = 0; i < 4; i++)
            {
                var corner = new Vector2((i & 1) == 0 ? box.xMin : box.xMax, i < 2 ? box.yMin : box.yMax);
                float arm = (3 + 2 * Mathf.Sin(time * 9 + i)) * unit;
                Glint(corner, arm, grade, i < 2 ? 0 : 1, sparkle * .75f, unit, 10 * unit, parent);
            }
        }

        static Vector2 DiamondPoint(int index, float half)
        {
            switch (index & 3)
            {
                case 0: return new Vector2(0, -half);
                case 1: return new Vector2(half, 0);
                case 2: return new Vector2(0, half);
                default: return new Vector2(-half, 0);
            }
        }

        static void Diamond(float half, float coverage, SynergyVisualGrade grade, float alpha,
            float width, float blur, float white, Color parent)
        {
            float fraction = coverage * .5f;
            for (int i = 0; i < 4; i++)
            {
                Vector2 corner = DiamondPoint(i, half);
                Vector2 before = Vector2.Lerp(corner, DiamondPoint(i + 3, half), fraction);
                Vector2 after = Vector2.Lerp(corner, DiamondPoint(i + 1, half), fraction);
                GlowingLine(before, corner, grade, Mathf.Clamp01((before.y + corner.y) / (4 * half) + .5f),
                    alpha, width, blur, white, parent);
                GlowingLine(corner, after, grade, Mathf.Clamp01((corner.y + after.y) / (4 * half) + .5f),
                    alpha, width, blur, white, parent);
            }
        }

        static Matrix4x4 Local(Matrix4x4 parent, Vector2 center, float degrees, float squash) =>
            parent * Matrix4x4.TRS(center, Quaternion.Euler(0, 0, degrees), new Vector3(1, squash, 1));

        static void DrawBadge(Rect bounds, Texture2D frame, Texture2D glyph, SynergyVisualGrade grade,
            float time, Motion motion, float unit, Color parent)
        {
            if (motion.Badge <= .001f) return;
            float size = bounds.width * .61f * motion.Scale;
            var center = new Vector2(bounds.center.x, bounds.y - unit);
            Matrix4x4 previous = GUI.matrix;
            try
            {
                GUI.matrix = Local(previous, center, 0, 1);
                Fill(new Rect(-size * .8f, -size * .8f, size * 1.6f, size * 1.6f),
                    GradeColor(grade, .5f), motion.Badge * .19f, parent, softHalo);

                if (time > .035f && time < .43f)
                {
                    float trail = 1 - Smooth((time - .27f) / .16f);
                    for (int i = 0; i < 2; i++)
                    {
                        var earlier = Evaluate(Mathf.Max(0, time - (i == 0 ? .065f : .032f)));
                        GUI.matrix = Local(previous, center, earlier.OuterDegrees, earlier.Squash);
                        Diamond(size * .47f * (i == 0 ? .98f : 1), .55f, grade,
                            motion.Badge * trail * (i == 0 ? .10f : .18f), unit, 5 * unit, .50f, parent);
                    }
                }

                GUI.matrix = Local(previous, center, motion.OuterDegrees, motion.Squash);
                // The confirmed grade plate is translucent; its later white glyph stays upright.
                Tint(parent, Color.white, motion.Badge * .17f);
                GUI.DrawTexture(new Rect(-size * .5f, -size * .5f, size, size), frame, ScaleMode.StretchToFill, true);
                Diamond(size * .47f, motion.Coverage, grade, motion.Badge * .96f, 1.65f * unit, 8 * unit, .50f, parent);
                Diamond(size * .47f, motion.Coverage, grade, motion.Badge * .65f, .65f * unit, 2 * unit, .94f, parent);

                float sparkle = Mathf.Sin(Mathf.PI * Mathf.Clamp01((time - .06f) / .48f));
                if (sparkle > 0)
                {
                    var square = new Rect(-size * .332f, -size * .332f, size * .664f, size * .664f);
                    var point = PerimeterPoint(square, Mathf.Clamp01((time - .06f) / .48f) * size * 2.656f);
                    GUI.matrix = Local(previous, center, motion.OuterDegrees, motion.Squash) *
                        Matrix4x4.Rotate(Quaternion.Euler(0, 0, -45));
                    Glint(point, 3 * unit, grade, Mathf.Clamp01(point.y / size + .5f),
                        sparkle * motion.Badge, 1.1f * unit, 9 * unit, parent);
                }

                GUI.matrix = Local(previous, center, motion.InnerDegrees, Mathf.Lerp(.9f, 1, Smooth(time / .48f)));
                Diamond(size * .39f, Mathf.Lerp(.5f, 1, Smooth((time - .04f) / .25f)),
                    grade, motion.Badge * .64f, .9f * unit, 4 * unit, .50f, parent);

                // Restore the unrotated badge-local matrix before the later, always-upright white glyph.
                GUI.matrix = Local(previous, center, 0, 1);
                float glyphSize = bounds.width * .37f * motion.GlyphScale;
                Tint(parent, Color.white, motion.GlyphAlpha);
                GUI.DrawTexture(new Rect(-glyphSize * .5f, -glyphSize * .5f, glyphSize, glyphSize),
                    glyph, ScaleMode.StretchToFill, true);
            }
            finally { GUI.matrix = previous; }
        }
    }
}
