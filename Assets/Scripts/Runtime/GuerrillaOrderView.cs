using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>The approved cut-in is presentation only; its members come from the successful order's lineup snapshot.</summary>
    public sealed class GuerrillaOrderView : MonoBehaviour
    {
        public const float FadeInDuration = .14f;
        public const float HoldDuration = .18f;
        public const float FadeOutDuration = .40f;
        public const float Duration = FadeInDuration + HoldDuration + FadeOutDuration;
        public const int SlotCount = 9;

        // These positions are painted into the approved illustration. Missing members never move their neighbours.
        static readonly string[] NativeMembers = { "CARD_M15", "CARD_M16", "CARD_M03", "CARD_M20", "CARD_M19", "CARD_M18", "CARD_M17" };
        readonly string[] slots = new string[SlotCount];
        readonly Sprite[] extensions = new Sprite[2];
        ReadOnlyCollection<string> visibleSlots;
        ManualBattleController owner;
        ManualBattle observed;
        int run = -1;
        double started = double.NegativeInfinity;
        bool active, pausedSound;
        Material material;
        Texture2D approved;
        AudioClip shing;
        AudioSource sound;

        public IReadOnlyList<string> SlotMembers { get { CheckIdentity(); return visibleSlots ?? (visibleSlots = Array.AsReadOnly(slots)); } }
        public int PlayCount { get; private set; }
        public bool Playing { get { CheckIdentity(); return active && CanPresent && CurrentAge < Duration; } }
        public float Age { get { CheckIdentity(); return active ? CurrentAge : 0; } }
        public float Opacity => Playing ? Smooth(Age / FadeInDuration) * (1 - Smooth((Age - FadeInDuration - HoldDuration) / FadeOutDuration)) : 0;
        public double PendingUntil { get { CheckIdentity(); return active && CanPresent && CurrentAge < Duration ? started + Duration : double.NegativeInfinity; } }
        public bool SoundPaused { get { CheckIdentity(); return Playing && sound != null && pausedSound; } }
        public bool SoundPlaying { get { CheckIdentity(); return !SoundPaused && Playing && sound != null && sound.isPlaying; } }
        public bool AssetsReady => approved != null && material != null && material.shader.isSupported && shing != null;
        /// <summary>Physical top-left screen coordinates, including the shared 16:9 letterbox offset.</summary>
        public Rect ScreenBounds { get { var v = TacticalViewport.Current; return new Rect(v.Offset.x, v.Offset.y, 1280 * v.Scale, 720 * v.Scale); } }
        float CurrentAge => owner == null || owner.Battle == null ? 0 : (float)Math.Max(0, owner.PresentationClock - started);
        bool CanPresent => isActiveAndEnabled && owner != null && owner.isActiveAndEnabled && owner.Battle != null && !owner.TestSelectionOpen && !owner.ArtLoading;
        static float Smooth(float value) { value = Mathf.Clamp01(value); return value * value * (3 - 2 * value); }

        public void Initialize(ManualBattleController controller, Font font)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            Clear(); owner = controller; observed = owner.Battle; run = observed?.RunId ?? -1;
            PrepareMaterial();
        }

        /// <summary>Call once after a newly successful order. No deployment, life, tier or damage state is written here.</summary>
        public void Play(IEnumerable<string> members, IReadOnlyDictionary<string, Sprite> extraPortraits = null)
        {
            if (members == null) throw new ArgumentNullException(nameof(members));
            CheckIdentity();
            if (owner == null || owner.Battle == null) return;
            Clear();
            var unique = new HashSet<string>(members.Where(id => !string.IsNullOrEmpty(id)), StringComparer.Ordinal);
            for (int i = 0; i < NativeMembers.Length; i++) if (unique.Remove(NativeMembers[i])) slots[i] = NativeMembers[i];
            int extension = 0;
            foreach (string id in unique.OrderBy(id => id, StringComparer.Ordinal))
            {
                if (extension == 2) break;
                slots[7 + extension] = id;
                if (extraPortraits != null && extraPortraits.TryGetValue(id, out var portrait)) extensions[extension] = portrait;
                extension++;
            }
            observed = owner.Battle; run = observed.RunId; started = owner.PresentationClock; active = true; PlayCount++;
            PrepareMaterial();
            if (sound != null && shing != null)
            {
                sound.clip = shing; sound.pitch = Mathf.Clamp(owner.EffectiveSimulationSpeed, .03f, 2); sound.mute = owner.Muted;
                sound.Play();
                if (observed.IsPaused) { sound.Pause(); pausedSound = true; }
            }
        }

        /// <summary>The actual production material, also usable for offscreen rendering and pixel inspection.</summary>
        public Material PrepareMaterial()
        {
            if (material == null)
            {
                approved = Resources.Load<Texture2D>("GuerrillaOrder/approved-cut-in");
                var shader = Resources.Load<Shader>("GuerrillaOrder/GuerrillaOrder");
                shing = Resources.Load<AudioClip>("GuerrillaOrder/shing");
                if (approved == null || shader == null || shing == null) throw new InvalidOperationException("Missing approved guerrilla cut-in illustration, shader or shing.");
                material = new Material(shader) { name = "Guerrilla order / approved member slices", hideFlags = HideFlags.DontSave, mainTexture = approved };
                sound = gameObject.AddComponent<AudioSource>(); sound.playOnAwake = false; sound.loop = false; sound.spatialBlend = 0; sound.volume = .55f;
            }
            material.SetVector("_MembersA", new Vector4(Has(0), Has(1), Has(2), Has(3)));
            material.SetVector("_MembersB", new Vector4(Has(4), Has(5), Has(6), Has(7)));
            material.SetFloat("_Member8", Has(8));
            material.SetFloat("_Opacity", Opacity);
            ConfigureExtension(0); ConfigureExtension(1);
            return material;
        }
        float Has(int i) => slots[i] == null ? 0 : 1;

        void CheckIdentity()
        {
            if (owner == null) return;
            var battle = owner.Battle;
            if (!ReferenceEquals(observed, battle) || run != (battle?.RunId ?? -1))
            { Clear(); observed = battle; run = battle?.RunId ?? -1; }
        }
        void Clear()
        {
            active = false; started = double.NegativeInfinity; pausedSound = false;
            Array.Clear(slots, 0, slots.Length); Array.Clear(extensions, 0, extensions.Length);
            if (sound != null) sound.Stop();
        }

        void LateUpdate()
        {
            CheckIdentity();
            if (!Playing) { if (sound != null) sound.Stop(); pausedSound = false; return; }
            if (sound == null) return;
            sound.mute = owner.Muted; sound.pitch = Mathf.Clamp(owner.EffectiveSimulationSpeed, .03f, 2);
            if (owner.Battle.IsPaused)
            { if (!pausedSound) { sound.Pause(); pausedSound = true; } return; }
            if (pausedSound) { sound.UnPause(); pausedSound = false; }
            // Keep manual simulation stepping and speed changes aligned with the same clock as the fade.
            if (Age >= shing.length) { sound.Stop(); return; }
            int sample = Mathf.Clamp(Mathf.RoundToInt(Age * shing.frequency), 0, shing.samples - 1);
            if (Math.Abs(sound.timeSamples - sample) > shing.frequency * .045f) sound.timeSamples = sample;
        }

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || !Playing) return;
            var production = PrepareMaterial();
            Matrix4x4 matrix = GUI.matrix; Color color = GUI.color; int depth = GUI.depth;
            try
            {
                GUI.matrix = Matrix4x4.identity; GUI.color = Color.white; GUI.depth = -2000;
                Graphics.DrawTexture(ScreenBounds, approved, new Rect(0, 0, 1, 1), 0, 0, 0, 0, Color.white, production);
            }
            finally { GUI.matrix = matrix; GUI.color = color; GUI.depth = depth; }
        }
        void ConfigureExtension(int index)
        {
            var sprite = extensions[index]; string suffix = index == 0 ? "A" : "B";
            bool present = sprite != null && slots[7 + index] != null;
            material.SetFloat("_ExtraPresent" + suffix, present ? 1 : 0);
            material.SetTexture("_Extra" + suffix, present ? sprite.texture : Texture2D.blackTexture);
            if (!present) return;
            Rect box = new Rect(index == 0 ? 3 : 1190, 205, 87, 310);
            float scale = Mathf.Min(box.width / sprite.rect.width, box.height / sprite.rect.height);
            var image = new Rect(box.center.x - sprite.rect.width * scale * .5f, box.center.y - sprite.rect.height * scale * .5f, sprite.rect.width * scale, sprite.rect.height * scale);
            var uv = new Rect(sprite.rect.x / sprite.texture.width, sprite.rect.y / sprite.texture.height, sprite.rect.width / sprite.texture.width, sprite.rect.height / sprite.texture.height);
            material.SetVector("_ExtraUv" + suffix, new Vector4(uv.x, uv.y, uv.width, uv.height));
            material.SetVector("_ExtraScreen" + suffix, new Vector4(image.x / 1280, image.y / 720, image.width / 1280, image.height / 720));
        }
        void OnDisable() => Clear();
        void OnDestroy()
        {
            Clear(); if (material != null) Destroy(material); if (sound != null) Destroy(sound);
        }
    }
}
