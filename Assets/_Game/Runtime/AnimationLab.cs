using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YesterdayConvoy
{
    /// <summary>Isolated presentation playground. It owns no combat HP, cards or deployment rules.</summary>
    public sealed class AnimationLab : MonoBehaviour
    {
        [SerializeField] private DemoSpriteVisual biped;
        [SerializeField] private Font interfaceFont;
        private RouteMotion motion;
        private float clock;
        private bool paused, moving = true;
        private GUIStyle label, button;
        public DemoSpriteVisual Biped => biped;
        public RouteMotion Motion => motion;
        public float SimulationTime => clock;
        public bool Paused => paused;
        public static readonly float[] KeyPhases = { 0, 0.125f, 0.25f, 0.375f };
        private static readonly string[] KeyNames = { "接触", "压低", "经过", "抬起" };
        private static readonly Vector3[] Route = {
            new Vector3(-4,0.04f,-1),new Vector3(-2,0.04f,-1),new Vector3(-2,0.04f,1),
            new Vector3(2,0.04f,1),new Vector3(2,0.04f,-1),new Vector3(4,0.04f,-1) };

        private void Start()
        {
            Restart();
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-animationLabSmokeTest") >= 0)
            {
                int at = Array.IndexOf(args,"-demoEvidenceDir");
                StartCoroutine(Verify(at >= 0 && at+1 < args.Length ? args[at+1] : null));
            }
        }
        public void Restart()
        {
            motion = new RouteMotion(Route,0.75f); clock = 0; paused = false; moving = true;
            biped.Presentation.ResetVisual(); Render();
        }
        public void SetPaused(bool value) => paused = value;
        public void SetMoving(bool value) { moving = value; paused = false; }
        public void Simulate(float delta)
        {
            if (paused) return;
            clock += delta;
            if (moving) motion.Advance(delta);
            if (motion.HasArrived) { Restart(); return; }
            Render();
        }
        private void Update() => Simulate(Time.deltaTime);
        private void Render()
        {
            biped.transform.parent.position = motion.Position;
            biped.PresentMotion((float)motion.DistanceTravelled,clock,motion.Direction,moving,Color.white);
        }
        public void ShowKeyPose(int index)
        {
            if (index < 0 || index >= KeyPhases.Length) throw new ArgumentOutOfRangeException(nameof(index));
            paused = true;
            biped.transform.parent.position = new Vector3(-0.5f,0.04f,0);
            biped.Presentation.ResetVisual();
            biped.PresentMotion(DemoWalkPuppet.Stride*KeyPhases[index],10,Vector3.right,true,Color.white);
        }
        private void OnGUI()
        {
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { font=interfaceFont,fontSize=21 };
                button = new GUIStyle(GUI.skin.button) { font=interfaceFont,fontSize=19 };
            }
            Matrix4x4 before = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/720f,1));
            GUI.Box(new Rect(20,18,1240,132),GUIContent.none);
            GUI.Label(new Rect(40,28,1100,33),"动画样板 · 独立关节与关键姿势预览",label);
            if (GUI.Button(new Rect(40,79,145,44),paused?"恢复播放":"暂停画面",button)) paused=!paused;
            if (GUI.Button(new Rect(200,79,145,44),moving?"停止行走":"继续行走",button)) SetMoving(!moving);
            if (GUI.Button(new Rect(360,79,145,44),"重新开始",button)) Restart();
            if (GUI.Button(new Rect(1040,79,185,44),"返回战斗 Demo",button)) SceneManager.LoadScene("InitialDemo");
            GUI.Box(new Rect(20,595,1240,105),GUIContent.none);
            GUI.Label(new Rect(40,604,1160,30),"关键姿势（右向保持原正面） · 点击后暂停，可检查关节重叠与鞋底",label);
            for(int i=0;i<4;i++) if(GUI.Button(new Rect(40+i*185,645,170,40),KeyNames[i],button)) ShowKeyPose(i);
            GUI.matrix = before;
        }
        private IEnumerator Verify(string directory)
        {
            Application.runInBackground = true; Time.timeScale = 0;
            for(int pose=0;pose<4;pose++)
            {
                ShowKeyPose(pose); yield return new WaitForEndOfFrame();
                if(directory!=null) InitialDemo.CaptureCamera(directory,$"keypose-{pose}.png");
            }
            Restart(); Simulate(0.8f/0.75f);
            for(int frame=0;frame<32;frame++)
            {
                yield return new WaitForEndOfFrame();
                if(directory!=null) InitialDemo.CaptureCamera(directory,$"layered-walk-{frame:00}.png");
                Simulate(DemoWalkPuppet.Stride/(32*0.75f));
            }
            SetMoving(false); Simulate(0.25f);
            bool stopped = biped.Puppet.MovementWeight == 0;
            SetPaused(true); Vector3 foot=biped.Puppet.LeftFoot;float time=clock;Simulate(1);
            bool frozen = clock==time && biped.Puppet.LeftFoot==foot;
            Restart();bool reset=clock==0 && motion.DistanceTravelled==0;
            Debug.Log($"ANIMATION_LAB_CHECK: stop={stopped}; pause={frozen}; restart={reset}.");
            Application.Quit(stopped && frozen && reset ? 0 : 1);
        }
    }
}
