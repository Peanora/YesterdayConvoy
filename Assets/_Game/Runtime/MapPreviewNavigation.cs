using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.IO;

namespace YesterdayConvoy
{
    /// <summary>Navigation for standalone visual review scenes only.</summary>
    public sealed class MapPreviewNavigation : MonoBehaviour
    {
        public int mapIndex;
        public Font interfaceFont;
        static readonly string[] Titles={"01  工业转运站", "02  雪原中继站", "03  芦苇水道"};
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();
            foreach(string arg in args)
                if(arg.StartsWith("--preview-map=")&&int.TryParse(arg.Substring(14),out int index)&&index>=0&&index<3&&index!=mapIndex)
                {SceneManager.LoadScene(index);yield break;}
            Debug.Log("MAP_PREVIEW_READY: "+Titles[mapIndex]);
            string capture=Array.Find(args,a=>a.StartsWith("--preview-capture="));
            if(capture==null)yield break;
            string path=capture.Substring(18);
            Directory.CreateDirectory(path);
            for(int i=0;i<30;i++)yield return null;
            yield return new WaitForEndOfFrame();
            var captureTexture=ScreenCapture.CaptureScreenshotAsTexture();
            if(captureTexture==null){Debug.LogError("Map screenshot unavailable");Application.Quit(1);yield break;}
            File.WriteAllBytes(Path.Combine(path,"player-"+mapIndex+".png"),captureTexture.EncodeToPNG());
            Destroy(captureTexture);
            Application.Quit(0);
        }
        void OnGUI()
        {
            if(interfaceFont!=null)GUI.skin.font=interfaceFont;
            float scale=Mathf.Max(.6f,Screen.width/1920f);
            GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            GUI.Box(new Rect(25,22,480,82),"");
            var title=new GUIStyle(GUI.skin.label){fontSize=24};title.normal.textColor=Color.white;
            GUI.Label(new Rect(42,32,450,36),Titles[mapIndex],title);
            GUI.Label(new Rect(42,70,450,25),"地图布局预览 · 设施尚未启用 · 红门进 / 蓝门出");
            for(int i=0;i<3;i++)
                if(GUI.Button(new Rect(25+i*165,Screen.height/scale-64,155,40),Titles[i]))
                    SceneManager.LoadScene(i);
        }
    }
}
