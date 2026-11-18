using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace YesterdayConvoy
{
    // This layer paints the real burn domain. Only the combat model applies hits.
    sealed class FormationTalulahRain : IDisposable
    {
        const float WaveSpacing = 1f;
        const float SweepTime = 1.45f;
        const float FallTime = .72f;
        const float EmberTail = .38f;
        const int DropsPerRow = 3;
        const int GroundOrder = 4550;
        static readonly Quaternion GroundRotation = Quaternion.LookRotation(-Vector3.up, Vector3.forward);
        static readonly Color FrontColor = new Color(1f, .91f, .72f);
        static readonly Color TailColor = new Color(1f, .68f, .35f);
        static readonly Color EmberColor = new Color(1f, .59f, .23f);

        readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();
        readonly Transform root;
        readonly Material material;
        readonly IReadOnlyList<Sprite> fireFrames;
        readonly Sprite[] emberFrames;
        int used;

        internal FormationTalulahRain(Transform parent, IReadOnlyList<Sprite> frames)
        {
            var shader = Resources.Load<Shader>("TalulahAttackAdditive");
            var emberTexture = Resources.Load<Texture2D>("FireRainEmbers");
            if (frames == null || frames.Count == 0 || shader == null || emberTexture == null)
                throw new InvalidOperationException("The formation fire rain requires the existing Talulah flame frames, ember sheet and additive material.");

            fireFrames = frames;
            root = new GameObject("Formation Talulah fire rain").transform;
            root.SetParent(parent, false);
            material = new Material(shader) { name = "Formation Talulah fire rain additive" };
            emberFrames = new Sprite[25];
            const int columns = 5;
            int cellWidth = emberTexture.width / columns;
            int cellHeight = emberTexture.height / columns;
            for (int i = 0; i < emberFrames.Length; i++)
            {
                var rect = new Rect(i % columns * cellWidth, (columns - 1 - i / columns) * cellHeight, cellWidth, cellHeight);
                emberFrames[i] = Sprite.Create(emberTexture, rect, new Vector2(.5f, .5f), cellWidth);
            }
        }

        internal void BeginFrame() => used = 0;

        internal void EndFrame()
        {
            for (int i = used; i < pool.Count; i++)
                pool[i].enabled = false;
        }

        internal void Draw(Camera camera, FormationBattleTerrainLayout terrain, double clock, double startedAt, double until)
        {
            float age = (float)(clock - startedAt);
            float duration = (float)(until - startedAt);
            if (age < 0 || age >= duration) return;

            // The board is known even when only one defender is left in a corner.
            const float left = FormationBattleModel.BattleAreaStart;
            const float right = FormationBattleModel.Width - .5f;
            float fade = Smooth(age / .24f) * Smooth((duration - age) / .58f);
            if (fade <= 0) return;

            DrawEmbers(terrain, age, left, right, fade);
            // One wave per second follows the burn domain rhythm. The individual
            // bursts on people are drawn by the actual hit events independently.
            int currentWave = Mathf.FloorToInt(age / WaveSpacing);
            for (int wave = Mathf.Max(0, currentWave - 1); wave <= currentWave; wave++)
            {
                float waveAge = age - wave * WaveSpacing;
                float intensity = fade * (wave < 2 ? 1f : .73f);
                for (int row = 0; row < FormationBattleModel.Height; row++)
                {
                    DrawSweep(camera, terrain, row, wave, waveAge, left, right, intensity);
                    for (int column = 0; column < DropsPerRow; column++)
                        DrawDrop(camera, terrain, row, column, wave, waveAge, left, right, intensity);
                }
            }
        }

        void DrawSweep(Camera camera, FormationBattleTerrainLayout terrain, int row, int wave, float age, float left, float right, float intensity)
        {
            int seed = row + wave * 43;
            float progress = (age - row * .021f) / SweepTime;
            if (progress < 0 || progress >= 1) return;

            float x = Mathf.Lerp(left - .36f, right + .7f, progress);
            float y = row + Mathf.Sin(progress * 5f + seed) * .16f;
            float fade = Smooth(progress * 7f) * Smooth((1f - progress) * 7f) * intensity;
            if (x >= left && x <= right)
            {
                var cell = new Vector2(x, y);
                DrawSprite("Sweeping flame", Frame(fireFrames, age, seed, 27),
                    GroundPoint(terrain, cell) + Vector3.up * (.44f + Jitter(seed) * .15f),
                    camera.transform.rotation * Quaternion.Euler(0, 0, 90), 1.04f, 2.85f,
                    FrontColor, fade * .84f, Order(y, 2));
            }

            float tailX = x - 1.16f;
            if (tailX >= left && tailX <= right)
            {
                var tail = new Vector2(tailX, y);
                DrawSprite("Flame wake", Frame(fireFrames, age, seed + 7, 31),
                    GroundPoint(terrain, tail) + Vector3.up * .05f,
                    GroundRotation * Quaternion.Euler(0, 0, 90), .95f, 2.28f, TailColor, fade * .56f, GroundOrder + 2);
            }
        }

        void DrawDrop(Camera camera, FormationBattleTerrainLayout terrain, int row, int column, int wave,
            float age, float left, float right, float intensity)
        {
            int seed = row * 11 + column * 89 + wave * 191;
            float elapsed = age - Jitter(seed) * .26f;
            if (elapsed < 0 || elapsed >= FallTime + EmberTail) return;

            float strip = (column + .14f + .72f * Jitter(seed + 7)) / DropsPerRow;
            var cell = new Vector2(Mathf.Lerp(left, right, strip), row + (Jitter(seed + 3) - .5f) * .44f);
            var ground = GroundPoint(terrain, cell);
            if (elapsed < FallTime)
            {
                float progress = elapsed / FallTime;
                float lift = 1f - progress * progress;
                // Each tongue drops into a real board position from above and left.
                // Their reach uses all rows and thirds, including unoccupied cells.
                float height = lift * (2.75f + Jitter(seed + 11) * 1.15f);
                var point = ground + Vector3.up * (.23f + height) - camera.transform.right * (1.05f * lift);
                float alpha = Smooth(progress * 7f) * Smooth((1f - progress) * 6f) * intensity;
                DrawSprite("Falling fire", Frame(fireFrames, elapsed, seed, 29), point,
                    camera.transform.rotation * Quaternion.Euler(0, 0, 18 + 8 * Jitter(seed + 15)),
                    .76f + Jitter(seed + 20) * .42f, 1.4f + Jitter(seed + 24) * .73f,
                    FrontColor, alpha * .85f, Order(cell.y, 12));
                return;
            }

            float tail = (elapsed - FallTime) / EmberTail;
            DrawSprite("Rain embers on the board", Frame(emberFrames, elapsed, seed, 32), ground + Vector3.up * .065f,
                GroundRotation, 1.2f, 1.45f, EmberColor, intensity * (1 - tail) * .52f, GroundOrder + 3);
        }

        void DrawEmbers(FormationBattleTerrainLayout terrain, float age, float left, float right, float intensity)
        {
            float warmed = Smooth(age / .8f);
            for (int row = 0; row < FormationBattleModel.Height; row++)
            {
                for (int column = 0; column < 5; column++)
                {
                    int seed = row * 31 + column * 77;
                    float x = Mathf.Lerp(left + .16f, right - .16f, (column + .2f + .6f * Jitter(seed)) / 5f);
                    var cell = new Vector2(x, row + (Jitter(seed + 17) - .5f) * .49f);
                    float flicker = .54f + .46f * Mathf.Sin(age * 3f + Jitter(seed + 9) * 2f * Mathf.PI);
                    DrawSprite("Smouldering ground", Frame(emberFrames, age, seed, 12), GroundPoint(terrain, cell) + Vector3.up * .035f,
                        GroundRotation, 1.3f, 1.1f, TailColor, warmed * flicker * intensity * .4f, GroundOrder);
                }
            }
        }

        void DrawSprite(string label, Sprite sprite, Vector3 position, Quaternion rotation, float width, float height, Color tint, float alpha, int order)
        {
            if (sprite == null || alpha <= .01f) return;
            if (used == pool.Count)
            {
                var view = new GameObject(label, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                view.transform.SetParent(root, false);
                view.sharedMaterial = material;
                pool.Add(view);
            }

            var renderer = pool[used++];
            if (renderer.gameObject.name != label) renderer.gameObject.name = label;
            renderer.enabled = true;
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            tint.a = alpha;
            renderer.color = tint;
            renderer.transform.SetPositionAndRotation(position, rotation);
            Vector2 size = sprite.bounds.size;
            renderer.transform.localScale = new Vector3(width / Mathf.Max(.01f, size.x), height / Mathf.Max(.01f, size.y), 1);
        }

        static Vector3 GroundPoint(FormationBattleTerrainLayout terrain, Vector2 cell)
            => FormationBattleController.WorldPoint(cell) + Vector3.up * (terrain?.HeightAt(cell) ?? 0);

        static Sprite Frame(IReadOnlyList<Sprite> sprites, float age, int phase, float fps)
            => sprites[(Mathf.FloorToInt(age * fps * 2f) + phase) % sprites.Count];

        static float Jitter(int seed) => Mathf.Repeat(Mathf.Sin(seed * 17.853f + 5.371f) * 43758.5453f, 1);
        static float Smooth(float value) => Mathf.SmoothStep(0, 1, Mathf.Clamp01(value));
        static int Order(float row, int offset) => 5000 - Mathf.RoundToInt(row * 50) + offset;

        public void Dispose()
        {
            root.gameObject.SetActive(false);
            Object.Destroy(root.gameObject);
            Object.Destroy(material);
            foreach (var frame in emberFrames) Object.Destroy(frame);
            pool.Clear();
        }
    }

    public sealed partial class FormationBattleController
    {
        FormationTalulahRain formationTalulahRain;

        void PresentFormationTalulahRain(FormationBattleModel.LegacyZoneSnapshot zone)
        {
            if (!ViewingDefenders || !UseFormationBattleCamera) return;
            if (formationTalulahRain == null)
            {
                var card = Array.Find(formationCards, candidate => candidate != null && candidate.CardId == "CARD_TALULAH_TRIAL");
                formationTalulahRain = new FormationTalulahRain(actorRoot, card?.ArticulatedArt?.FireFrames);
            }
            formationTalulahRain.Draw(battleCamera, Model.Terrain, Model.Clock, zone.At, zone.Until);
        }
    }
}
