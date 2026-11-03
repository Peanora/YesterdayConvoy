using System;
using System.Collections;
using UnityEngine;
namespace YesterdayConvoy
{
    public sealed partial class ManualBattleController
    {
        private IEnumerator TacticalFrame(string directory,string name)
        {
            System.IO.Directory.CreateDirectory(directory);
            yield return new WaitForEndOfFrame();
            var frame=ScreenCapture.CaptureScreenshotAsTexture();
            if(frame==null){Debug.LogError("TACTICAL_CAPTURE_FAILED: a visible graphics window is required.");Time.timeScale=1;Application.Quit(1);yield break;}
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory,name+".png"),frame.EncodeToPNG());Destroy(frame);
            yield return null;yield return null;
        }
    }
}
