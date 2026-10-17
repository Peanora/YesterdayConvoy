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
