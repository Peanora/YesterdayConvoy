using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    public enum DemoOutcome { Ready, Running, Breached, Defeated }

    /// <summary>One playable prototype: red gate -> moving monster -> tower fire -> blue gate or death.</summary>
    public sealed class InitialDemo : MonoBehaviour
    {
        [SerializeField] private UnitDefinition definition;
        [SerializeField] private Transform monster;
        [SerializeField] private Transform tower;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Transform redGate;
        [SerializeField] private Transform blueGate;
        [SerializeField] private LineRenderer shotBeam;
        [SerializeField] private Vector3[] waypoints;
        [SerializeField] private float towerRange = 2.8f;
        [SerializeField] private float towerDamage = 16f;
        [SerializeField] private float shotInterval = 1f;
        [SerializeField] private int initialDefense = 8;
        [SerializeField] private CardDefinition card;
        [SerializeField] private Font interfaceFont;

        private RouteMotion motion;
        private float elapsed, nextShotAt, beamRemaining, selectedHealth = 100f, runMaxHealth;
        private Font font;
        private bool ownsFont;
        private GUIStyle titleStyle, textStyle, smallStyle, buttonStyle;

        public DemoOutcome Outcome { get; private set; }
        public float Health { get; private set; }
        public int GateDefense { get; private set; }
        public int ShotsFired { get; private set; }
        public Transform Monster => monster;
        public RouteMotion Motion => motion;
        public float SimulationTime => elapsed;
        public CardDefinition Card => card;

        private void Start()
        {
            ResetRun(selectedHealth, DemoOutcome.Ready);
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-demoSmokeTest") >= 0)
            {
                Application.runInBackground = true;
                int index = Array.IndexOf(args, "-demoEvidenceDir");
                string directory = index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
                StartCoroutine(VerifyDemo(directory));
            }
        }

        public void BeginAttack(float maxHealth) => ResetRun(maxHealth, DemoOutcome.Running);

        private void ResetRun(float maxHealth, DemoOutcome state)
        {
            if (float.IsNaN(maxHealth) || float.IsInfinity(maxHealth) || maxHealth <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            if (definition == null || monster == null || tower == null || muzzle == null ||
                redGate == null || blueGate == null || shotBeam == null)
                throw new InvalidOperationException("Initial Demo: missing monster, tower, gate or configuration reference.");
            motion = new RouteMotion(waypoints, definition.MoveSpeed);
            runMaxHealth = Health = maxHealth;
            selectedHealth = maxHealth;
            GateDefense = initialDefense;
            ShotsFired = 0;
            elapsed = nextShotAt = beamRemaining = 0;
            monster.gameObject.SetActive(true);
            monster.position = motion.Position;
            shotBeam.enabled = false;
            Outcome = state;
        }

        private void Update() => Simulate(Time.deltaTime);

        public void Simulate(float delta)
        {
            if (float.IsNaN(delta) || float.IsInfinity(delta) || delta < 0)
                throw new ArgumentOutOfRangeException(nameof(delta));
            beamRemaining = Mathf.Max(0, beamRemaining - delta);
            if (shotBeam != null)
                shotBeam.enabled = beamRemaining > 0;
            if (Outcome != DemoOutcome.Running || delta == 0)
                return;
            elapsed += delta;
            motion.Advance(delta);
            monster.position = motion.Position;

            Vector3 offset = monster.position - tower.position;
            offset.y = 0;
            if (offset.sqrMagnitude > 0.001f)
                muzzle.parent.rotation = Quaternion.LookRotation(offset);
            if (offset.sqrMagnitude <= towerRange * towerRange && elapsed >= nextShotAt)
            {
                nextShotAt = elapsed + shotInterval;
                ShotsFired++;
                Health = Mathf.Max(0, Health - towerDamage);
                shotBeam.SetPosition(0, muzzle.position);
                shotBeam.SetPosition(1, monster.position + Vector3.up * 0.65f);
                shotBeam.enabled = true;
                beamRemaining = 0.18f;
                if (Health <= 0)
                {
                    monster.gameObject.SetActive(false);
                    Outcome = DemoOutcome.Defeated;
                    Debug.Log($"DEMO_DEFEAT: shots={ShotsFired}; gate={GateDefense}; health=0.");
                    return;
                }
            }
            if (motion.HasArrived)
            {
                GateDefense = Mathf.Max(0, GateDefense - definition.GateDamage);
                monster.gameObject.SetActive(false);
                Outcome = DemoOutcome.Breached;
                Debug.Log($"DEMO_BREACH: shots={ShotsFired}; gate={GateDefense}; health={Health}.");
            }
        }

        private void OnGUI()
        {
            if (motion == null)
                return;
            EnsureStyles();
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
            Panel(new Rect(22, 20, 1236, 88), new Color(0.06f, 0.09f, 0.12f, 0.96f));
            GUI.Label(new Rect(42, 30, 750, 38), "昨日圆车  /  就这？进攻小队", titleStyle);
            GUI.Label(new Rect(44, 73, 740, 26), "红门出发 → 穿过防御塔射程 → 突破蓝门", smallStyle);
            GUI.Label(new Rect(950, 40, 280, 32), $"蓝门防御  {GateDefense} / {initialDefense}", textStyle);

            if (monster.gameObject.activeSelf)
            {
                Vector3 screen = Camera.main.WorldToScreenPoint(monster.position + Vector3.up * 1.85f);
                if (screen.z > 0)
                {
                    float x = screen.x * 1280f / Screen.width, y = 720 - screen.y * 720f / Screen.height;
                    Panel(new Rect(x - 43, y, 86, 9), new Color(0.05f, 0.07f, 0.08f, 0.95f));
                    Panel(new Rect(x - 41, y + 2, 82 * Health / runMaxHealth, 5), new Color(0.45f, 0.91f, 0.61f));
                    GUI.Label(new Rect(x - 60, y - 25, 140, 25), $"{Health:0} / {runMaxHealth:0}", smallStyle);
                }
            }
            WorldLabel(redGate.position + Vector3.up * 2.2f, "红门 · 出发");
            WorldLabel(blueGate.position + Vector3.up * 2.2f, "蓝门 · 突破目标");
            WorldLabel(tower.position + Vector3.up * 2.4f, "臭脸防御炮");

            Panel(new Rect(22, 605, 1236, 94), new Color(0.06f, 0.09f, 0.12f, 0.96f));
            string result = Outcome == DemoOutcome.Ready ? "准备进攻" : Outcome == DemoOutcome.Running ? "进攻进行中" :
                Outcome == DemoOutcome.Breached ? $"突破蓝门！本次扣除 {definition.GateDamage} 点防御" : "进攻失败：怪物被防御塔击败";
            GUI.Label(new Rect(42, 613, 760, 30), result, textStyle);
            GUI.Label(new Rect(42, 654, 200, 24), $"怪物生命  {selectedHealth:0}", smallStyle);
            bool previousEnabled = GUI.enabled;
            GUI.enabled = Outcome != DemoOutcome.Running;
            selectedHealth = Mathf.Round(GUI.HorizontalSlider(new Rect(220, 660, 210, 20), selectedHealth, 30, 140));
            if (Outcome == DemoOutcome.Ready) runMaxHealth = Health = selectedHealth;
            GUI.Label(new Rect(452, 651, 510, 25), $"塔已攻击 {ShotsFired} 次  ·  降低生命可观察失败", smallStyle);
            Color background = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1, 0.75f, 0.28f);
            if (GUI.Button(new Rect(1030, 622, 205, 58), Outcome == DemoOutcome.Ready ? "开始进攻" : Outcome == DemoOutcome.Running ? "进攻中…" : "重新开始", buttonStyle))
                BeginAttack(selectedHealth);
            GUI.backgroundColor = background;
            GUI.enabled = previousEnabled;
            GUI.matrix = previousMatrix;
        }

        private void EnsureStyles()
        {
            if (textStyle != null)
                return;
            ownsFont = interfaceFont == null;
            font = interfaceFont != null ? interfaceFont : Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 18);
            textStyle = new GUIStyle(GUI.skin.label) { font = font, fontSize = 21 };
            titleStyle = new GUIStyle(textStyle) { fontSize = 27, fontStyle = FontStyle.Bold };
            smallStyle = new GUIStyle(textStyle) { fontSize = 16 };
            buttonStyle = new GUIStyle(GUI.skin.button) { font = font, fontSize = 22, fontStyle = FontStyle.Bold };
            buttonStyle.normal.textColor = Color.black;
        }

        private static void Panel(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void WorldLabel(Vector3 position, string label)
        {
            Vector3 point = Camera.main.WorldToScreenPoint(position);
            if (point.z > 0)
                GUI.Label(new Rect(point.x * 1280f / Screen.width - 70, 720 - point.y * 720f / Screen.height - 15, 160, 28), label, smallStyle);
        }

        private IEnumerator VerifyDemo(string directory)
        {
            float start = Time.realtimeSinceStartup;
            bool captured = false;
            Time.timeScale = 3;
            BeginAttack(100);
            while (Outcome == DemoOutcome.Running && Time.realtimeSinceStartup - start < 20)
            {
                if (!captured && ShotsFired > 0)
                {
                    if (directory != null) CaptureCamera(directory, "demo-action.png");
                    captured = true;
                }
                yield return null;
            }
            bool breached = Outcome == DemoOutcome.Breached && GateDefense == initialDefense - definition.GateDamage && ShotsFired > 0;
            int settled = GateDefense;
            Simulate(10);
            bool once = GateDefense == settled;
            BeginAttack(30);
            while (Outcome == DemoOutcome.Running && Time.realtimeSinceStartup - start < 25)
                yield return null;
            bool killed = Outcome == DemoOutcome.Defeated && GateDefense == initialDefense;
            BeginAttack(100);
            bool reset = Health == 100 && GateDefense == initialDefense && ShotsFired == 0 && monster.gameObject.activeSelf;
            Time.timeScale = 1;
            Debug.Log($"INITIAL_DEMO_CHECK: breach={breached}; gateOnce={once}; killed={killed}; restart={reset}.");
            Application.Quit(breached && once && killed && reset ? 0 : 1);
        }

        private static void CaptureCamera(string directory, string name)
        {
            Directory.CreateDirectory(directory);
            Camera camera = Camera.main;
            RenderTexture oldTarget = camera.targetTexture, oldActive = RenderTexture.active;
            var target = new RenderTexture(1280, 720, 24);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(Path.Combine(directory, name), pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = oldTarget;
                RenderTexture.active = oldActive;
                target.Release();
                Destroy(target);
                Destroy(pixels);
            }
        }

        private void OnDestroy()
        {
            if (ownsFont && font != null) Destroy(font);
        }
    }
}
