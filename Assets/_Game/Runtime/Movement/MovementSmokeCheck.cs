using System;
using System.IO;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Opt-in Player verification, disabled during normal play. Timeout is a test failure, never game defeat.</summary>
    public sealed class MovementSmokeCheck : MonoBehaviour
    {
        private MovementPrototype prototype;
        private string evidenceDirectory;
        private double lastDistance;
        private float startedAt;
        private bool capturedStart;
        private bool capturedMiddle;
        private int finishedFrames;

        private void Awake()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-movementSmokeTest") < 0)
            {
                enabled = false;
                return;
            }
            prototype = GetComponent<MovementPrototype>();
            Application.runInBackground = true;
            int pathIndex = Array.IndexOf(args, "-movementEvidenceDir");
            if (pathIndex >= 0 && pathIndex + 1 < args.Length)
            {
                evidenceDirectory = args[pathIndex + 1];
                Directory.CreateDirectory(evidenceDirectory);
            }
            startedAt = Time.realtimeSinceStartup;
        }

        private void Update()
        {
            if (Time.realtimeSinceStartup - startedAt > 30f)
            {
                Fail("Movement/evidence verification did not finish within Player test watchdog; not a battle defeat rule.");
                return;
            }
            if (prototype == null || prototype.Unit == null || prototype.Unit.Motion == null)
                return;
            RouteMotion motion = prototype.Unit.Motion;
            if (!capturedStart)
            {
                if (FindObjectsByType<PrototypeUnit>().Length != 1)
                {
                    Fail("Expected exactly one prototype unit.");
                    return;
                }
                Debug.Log($"T02_MOVEMENT_START: {prototype.Unit.UnitId}; speed={motion.Speed}; length={motion.TotalLength}.");
                if (!Capture("start.png"))
                    return;
                capturedStart = true;
            }
            if (motion.DistanceTravelled < lastDistance)
            {
                Fail("Path progress moved backwards.");
                return;
            }
            lastDistance = motion.DistanceTravelled;
            if (!capturedMiddle && motion.DistanceTravelled >= motion.TotalLength * 0.5)
            {
                if (!Capture("middle.png"))
                    return;
                capturedMiddle = true;
            }
            if (!motion.HasArrived)
                return;
            Vector3[] route = prototype.CopyWorldWaypoints();
            if (Vector3.Distance(prototype.Unit.transform.position, route[route.Length - 1]) > 0.0001f)
            {
                Fail("Unit did not stop at the authored endpoint.");
                return;
            }
            if (finishedFrames++ == 0)
            {
                if (!Capture("end.png"))
                    return;
                Debug.Log($"T02_MOVEMENT_PASS: {prototype.Unit.UnitId}; arrived at {motion.Position}; distance={motion.DistanceTravelled}.");
            }
            if (finishedFrames >= 5 && (evidenceDirectory == null || File.Exists(Path.Combine(evidenceDirectory, "end.png"))))
                Application.Quit(0);
        }

        private bool Capture(string file)
        {
            if (evidenceDirectory == null)
                return true;
            Camera camera = Camera.main;
            if (camera == null)
            {
                Fail("Cannot capture evidence without a main camera.");
                return false;
            }
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            var target = new RenderTexture(1280, 720, 24);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(Path.Combine(evidenceDirectory, file), pixels.EncodeToPNG());
                return true;
            }
            catch (Exception error)
            {
                Fail("Camera evidence capture failed: " + error.Message);
                return false;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                target.Release();
                Destroy(target);
                Destroy(pixels);
            }
        }

        private void Fail(string message)
        {
            Debug.LogError("T02_MOVEMENT_FAIL: " + message);
            enabled = false;
            Application.Quit(1);
        }
    }
}
