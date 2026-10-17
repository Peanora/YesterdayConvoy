using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace YesterdayConvoy.Tests
{
    public sealed class AnimationLabTests
    {
        private float originalScale;
        [SetUp] public void SetUp() => originalScale=Time.timeScale;
        [TearDown] public void TearDown() => Time.timeScale=originalScale;

        [UnityTest]
        public IEnumerator ReloadedHeadCannotHideTheWalkingLegArtwork()
        {
            yield return SceneManager.LoadSceneAsync("AnimationLab",LoadSceneMode.Single);
            Time.timeScale=0;yield return null;
            var lab=Object.FindAnyObjectByType<AnimationLab>();
            lab.ShowKeyPose(0);
            var rig=lab.Biped.GetComponent<BipedVisualRig>();
            foreach (var view in rig.Views)
            {
                Sprite head=view.Head.Renderer.sprite;
                Assert.That(head.vertices.Length,Is.EqualTo(view.HeadVertices.Length));
                foreach (Vector2 vertex in head.vertices)
                    Assert.That(vertex.y,Is.GreaterThan(-0.2f),"Head artwork must end above the legs, even after asset reload.");
            }
            Vector3 shoe=rig.Front.ShoeL.Renderer.transform.position;
            lab.Biped.PresentMotion(DemoWalkPuppet.Stride*0.75f,11,Vector3.right,true,Color.white);
            Assert.That(rig.Front.ShoeL.Renderer.enabled,Is.True);
            Assert.That(Vector3.Distance(shoe,rig.Front.ShoeL.Renderer.transform.position),Is.GreaterThan(0.1f));
            // During a turn the head can lead the torso. Serialized Part wrappers are not
            // reference-identical, so visibility must use their actual SpriteRenderer identity.
            lab.Biped.PresentMotion(0.5f,12,Vector3.forward,true,Color.white);
            lab.Biped.PresentMotion(0.55f,12.19f,Vector3.forward,true,Color.white);
            int heads=0;
            foreach (var view in rig.Views)
                if (view.Root.gameObject.activeSelf && view.Head.Renderer.enabled) heads++;
            Assert.That(heads,Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator KeyPosesStopPauseAndRestartUseAuthoredPrefab()
        {
            yield return SceneManager.LoadSceneAsync("AnimationLab",LoadSceneMode.Single);
            Time.timeScale=0;yield return null;
            var lab=Object.FindAnyObjectByType<AnimationLab>();
            Assert.That(lab.Biped.GetComponent<BipedVisualRig>().IsComplete,Is.True);
            Assert.That(lab.Biped.GetComponentsInChildren<MeshFilter>(true).Length,Is.Zero);
            var rig=lab.Biped.GetComponent<BipedVisualRig>();
            Quaternion contact=Quaternion.identity;
            for(int i=0;i<4;i++)
            {
                lab.ShowKeyPose(i);
                if(i==0)contact=rig.Front.BootL.Bone.localRotation;
                else Assert.That(Quaternion.Angle(contact,rig.Front.BootL.Bone.localRotation),Is.GreaterThan(0.1f));
                Assert.That(lab.Paused,Is.True);
            }
            lab.Restart();lab.Simulate(0.2f);lab.SetMoving(false);
            Vector3 before=lab.Biped.transform.parent.position;
            lab.Simulate(0.25f);
            Assert.That(lab.Biped.Puppet.MovementWeight,Is.Zero);
            Assert.That(lab.Biped.transform.parent.position,Is.EqualTo(before));
            lab.SetPaused(true);float time=lab.SimulationTime;Vector3 foot=lab.Biped.Puppet.LeftFoot;
            lab.Simulate(1);
            Assert.That(lab.SimulationTime,Is.EqualTo(time));
            Assert.That(lab.Biped.Puppet.LeftFoot,Is.EqualTo(foot));
            lab.Restart();Assert.That(lab.Motion.DistanceTravelled,Is.Zero);Assert.That(lab.SimulationTime,Is.Zero);
        }

        [UnityTest]
        public IEnumerator DifferentBodiesShareMotionButKeepTheirOwnAnimationAndFreezeTogether()
        {
            yield return SceneManager.LoadSceneAsync("AnimationLab",LoadSceneMode.Single);
            Time.timeScale=0;yield return null;
            var lab=Object.FindAnyObjectByType<AnimationLab>();
            Assert.That(lab.Floating,Is.Not.Null);
            Assert.That(lab.Floating.GetComponent<BipedVisualRig>(),Is.Null,"A legless body must not be given a biped rig.");
            var floating=(FloatingVisualPresentation)lab.Floating.Presentation;
            Quaternion rootRotation=lab.Floating.transform.parent.rotation;
            Vector3 rootScale=lab.Floating.transform.parent.localScale;
            for(int step=0;step<120;step++)
            {
                lab.Simulate(0.05f);
                UnitVisualFrame a=lab.Biped.Presentation.LastFrame,b=lab.Floating.Presentation.LastFrame;
                Assert.That(b.Time,Is.EqualTo(a.Time));Assert.That(b.Distance,Is.EqualTo(a.Distance));
                Assert.That(b.Direction,Is.EqualTo(a.Direction));Assert.That(b.Moving,Is.EqualTo(a.Moving));
                Assert.That(Vector3.Distance(lab.Floating.transform.parent.position-lab.Biped.transform.parent.position,new Vector3(2.8f,0,0)),Is.LessThan(0.0001f));
            }
            Assert.That(lab.Floating.transform.parent.rotation,Is.EqualTo(rootRotation));
            Assert.That(lab.Floating.transform.parent.localScale,Is.EqualTo(rootScale));
            lab.SetMoving(false);lab.Simulate(0.25f);
            Assert.That(lab.Biped.Presentation.MotionWeight,Is.Zero);Assert.That(floating.MotionWeight,Is.Zero);
            Vector3 root=lab.Floating.transform.parent.position,hover=floating.Body.localPosition;
            lab.Simulate(0.1f);
            Assert.That(lab.Floating.transform.parent.position,Is.EqualTo(root));
            Assert.That(floating.Body.localPosition,Is.Not.EqualTo(hover),"Idle hovering continues while the simulation runs.");
            lab.SetPaused(true);hover=floating.Body.localPosition;Quaternion rotation=floating.Body.localRotation;float clock=lab.SimulationTime;
            lab.Simulate(1);
            Assert.That(lab.SimulationTime,Is.EqualTo(clock));Assert.That(floating.Body.localPosition,Is.EqualTo(hover));Assert.That(floating.Body.localRotation,Is.EqualTo(rotation));
            lab.Restart();Assert.That(lab.SimulationTime,Is.Zero);Assert.That(floating.Body.localPosition.y,Is.EqualTo(0.28f).Within(0.0001f));
        }

        [UnityTest]
        public IEnumerator TwoPrefabInstancesShareArtButKeepIndependentPoseState()
        {
            yield return SceneManager.LoadSceneAsync("AnimationLab",LoadSceneMode.Single);
            Time.timeScale=0;yield return null;
            var lab=Object.FindAnyObjectByType<AnimationLab>();
            var clone=Object.Instantiate(lab.Biped.gameObject).GetComponent<DemoSpriteVisual>();
            try
            {
                lab.ShowKeyPose(1);
                var first=lab.Biped.GetComponent<BipedVisualRig>();var second=clone.GetComponent<BipedVisualRig>();
                Quaternion saved=first.Front.BootL.Bone.localRotation;
                clone.PresentMotion(DemoWalkPuppet.Stride*0.375f,10,Vector3.right,true,Color.white);
                Assert.That(first.Front.BootL.Bone.localRotation,Is.EqualTo(saved));
                Assert.That(Quaternion.Angle(saved,second.Front.BootL.Bone.localRotation),Is.GreaterThan(0.1f));
                Assert.That(second.Front.Body.Renderer.sprite,Is.SameAs(first.Front.Body.Renderer.sprite));
                Assert.That(second.Front.Body.Renderer.sharedMaterial,Is.SameAs(first.Front.Body.Renderer.sharedMaterial));
            }
            finally {Object.Destroy(clone.gameObject);}
        }
    }
}
