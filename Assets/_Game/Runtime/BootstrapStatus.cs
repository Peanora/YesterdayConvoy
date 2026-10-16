using System;
using UnityEngine;

namespace YesterdayConvoy
{
    /// <summary>Visible entry marker and five-frame startup check, separate from the full movement verification.</summary>
    public sealed class BootstrapStatus : MonoBehaviour
    {
        private int observedFrames;
        private bool smokeTest;

        private void Awake()
        {
            smokeTest = Array.IndexOf(Environment.GetCommandLineArgs(), "-baselineSmokeTest") >= 0;
            Debug.Log("BASELINE_ENTRY_AWAKE: Bootstrap scene loaded; T02 movement demo, combat not implemented.");
        }

        private void Update()
        {
            observedFrames++;
            if (!smokeTest || observedFrames != 5)
                return;

            Camera entryCamera = Camera.main;
            bool valid = gameObject.scene.name == "Bootstrap" && entryCamera != null && entryCamera.orthographic;
            Debug.Log(valid ? "BASELINE_SMOKE_PASS: scene, component, camera and five runtime frames verified."
                            : "BASELINE_SMOKE_FAIL: entry scene or orthographic camera is missing.");
            Application.Quit(valid ? 0 : 1);
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(24, 24, 520, 140), "Yesterday Convoy / T02 Prototype");
            GUI.Label(new Rect(44, 64, 480, 70),
                "One configured unit follows the fixed route.\nGreen = start | Amber = unit | Blue = endpoint\nCombat, deployment and shop are not implemented yet.");
        }
    }
}
