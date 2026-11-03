using UnityEngine;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Collections;
using System.Runtime.InteropServices;
#endif

namespace YesterdayConvoy
{
    // Only the game's own Windows Player is subclassed. Editor/docked Game views are untouched.
    internal sealed class ProportionalPlayerWindow : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if(FindAnyObjectByType<ProportionalPlayerWindow>()==null)
            {var go=new GameObject("Proportional player window");DontDestroyOnLoad(go);go.AddComponent<ProportionalPlayerWindow>();}
#endif
        }
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        const uint Sizing=0x214,GetMinMax=0x24,EnterSize=0x231,ExitSize=0x232,NcDestroy=0x82;
        static readonly UIntPtr HookId=new UIntPtr(0x5943);
        IntPtr window;
        Subclass callback;
        bool inSizeMove,normalizing;
        int attempts;
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]delegate IntPtr Subclass(IntPtr h,uint message,IntPtr w,IntPtr l,UIntPtr id,UIntPtr data);
        delegate bool EnumWindow(IntPtr h,IntPtr data);
        [StructLayout(LayoutKind.Sequential)]struct NativeRect
        {
            public int Left,Top,Right,Bottom;
            public RectInt Rect=>new RectInt(Left,Top,Right-Left,Bottom-Top);
            public static NativeRect From(RectInt r)=>new NativeRect{Left=r.x,Top=r.y,Right=r.xMax,Bottom=r.yMax};
        }
        [StructLayout(LayoutKind.Sequential)]struct Point{public int X,Y;public Point(int x,int y){X=x;Y=y;}}
        [StructLayout(LayoutKind.Sequential)]struct MinMax{public Point Reserved,MaxSize,MaxPosition,MinTrack,MaxTrack;}
        [StructLayout(LayoutKind.Sequential)]struct MonitorInfo{public uint Size;public NativeRect Monitor,Work;public uint Flags;}
        [DllImport("user32.dll")]static extern bool EnumWindows(EnumWindow callback,IntPtr data);
        [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr h,out uint process);
        [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll")]static extern bool IsIconic(IntPtr h);
        [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr h,out NativeRect rect);
        [DllImport("user32.dll")]static extern bool GetClientRect(IntPtr h,out NativeRect rect);
        [DllImport("user32.dll")]static extern IntPtr MonitorFromWindow(IntPtr h,uint flags);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern bool GetMonitorInfo(IntPtr h,ref MonitorInfo info);
        [DllImport("user32.dll")]static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int width,int height,uint flags);
        [DllImport("comctl32.dll",SetLastError=true)]static extern bool SetWindowSubclass(IntPtr h,Subclass callback,UIntPtr id,UIntPtr data);
        [DllImport("comctl32.dll")]static extern bool RemoveWindowSubclass(IntPtr h,Subclass callback,UIntPtr id);
        [DllImport("comctl32.dll")]static extern IntPtr DefSubclassProc(IntPtr h,uint message,IntPtr w,IntPtr l);
        void Awake(){callback=ProcessMessage;TryInstall();}
        void TryInstall()
        {
            uint own=(uint)System.Diagnostics.Process.GetCurrentProcess().Id;
            EnumWindows((h,unused)=>{GetWindowThreadProcessId(h,out uint pid);if(pid!=own||!IsWindowVisible(h))return true;window=h;return false;},IntPtr.Zero);
            if(window==IntPtr.Zero)return;
            if(!SetWindowSubclass(window,callback,HookId,UIntPtr.Zero)){Debug.LogError("WINDOW_ASPECT_HOOK_FAILED: "+Marshal.GetLastWin32Error());window=IntPtr.Zero;return;}
            Debug.Log("WINDOW_ASPECT_LOCKED: native client 16:9; sizing and maximize preserve aspect.");
        }
        void Update()
        {
            if(window==IntPtr.Zero){if(++attempts<120)TryInstall();return;}
            if(inSizeMove||normalizing||Screen.fullScreen||IsIconic(window))return;
            GetClientRect(window,out var client);int width=client.Right,height=client.Bottom;
            if(width<=0||height<=0||Math.Abs(width*9L-height*16L)<=16)return;
            // Catch startup resolutions, OS snapping and DPI changes once, rather than reallocating every frame.
            var size=WindowAspectGeometry.Fit(width,height);var frame=Frame();GetWindowRect(window,out var outer);
            normalizing=true;SetWindowPos(window,IntPtr.Zero,outer.Left,outer.Top,size.x+frame.x,size.y+frame.y,0x14);normalizing=false;
        }
        Vector2Int Frame()
        {
            GetWindowRect(window,out var outer);GetClientRect(window,out var client);
            return new Vector2Int(outer.Right-outer.Left-client.Right,outer.Bottom-outer.Top-client.Bottom);
        }
        IntPtr ProcessMessage(IntPtr h,uint message,IntPtr w,IntPtr l,UIntPtr id,UIntPtr data)
        {
            if(message==NcDestroy){RemoveWindowSubclass(h,callback,HookId);window=IntPtr.Zero;return DefSubclassProc(h,message,w,l);}
            if(message==EnterSize)inSizeMove=true;
            if(message==ExitSize)inSizeMove=false;
            if(message==Sizing)
            {
                GetClientRect(h,out var client);var proposed=Marshal.PtrToStructure<NativeRect>(l);
                var resized=WindowAspectGeometry.Resize(proposed.Rect,Frame(),w.ToInt32(),new Vector2Int(client.Right,client.Bottom));
                Marshal.StructureToPtr(NativeRect.From(resized),l,false);return new IntPtr(1);
            }
            var result=DefSubclassProc(h,message,w,l);
            if(message==GetMinMax)
            {
                var monitor=new MonitorInfo{Size=(uint)Marshal.SizeOf<MonitorInfo>()};
                if(GetMonitorInfo(MonitorFromWindow(h,2),ref monitor))
                {
                    var frame=Frame();var work=monitor.Work.Rect;var size=WindowAspectGeometry.Fit(work.width-frame.x,work.height-frame.y);
                    var limits=Marshal.PtrToStructure<MinMax>(l);
                    limits.MinTrack=new Point(640+frame.x,360+frame.y);
                    limits.MaxSize=limits.MaxTrack=new Point(size.x+frame.x,size.y+frame.y);
                    limits.MaxPosition=new Point(work.x-monitor.Monitor.Left+(work.width-limits.MaxSize.X)/2,work.y-monitor.Monitor.Top+(work.height-limits.MaxSize.Y)/2);
                    Marshal.StructureToPtr(limits,l,false);
                }
            }
            return result;
        }
        void OnDestroy(){if(window!=IntPtr.Zero)RemoveWindowSubclass(window,callback,HookId);window=IntPtr.Zero;}
        IEnumerator Start()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-windowAspectSmokeTest")<0)yield break;
            Application.runInBackground=true;
            for(int i=0;i<180;i++){var c=FindAnyObjectByType<ManualBattleController>();if(window!=IntPtr.Zero&&c!=null&&c.ShopTrialActive)break;yield return null;}
            yield return new WaitForSecondsRealtime(.5f);
            var controller=FindAnyObjectByType<ManualBattleController>();
            if(window==IntPtr.Zero||controller==null||!controller.ShopTrialActive){Debug.LogError("WINDOW_ASPECT_CHECK: bootstrap failed");Application.Quit(1);yield break;}
            controller.StartBattle();controller.SelectCard(true);controller.TryDeployAt(controller.Battle.Entry);controller.Battle.SetPaused(true);
            int dp=controller.Battle.DP;double clock=controller.Battle.Clock;bool aspect=true,coupled=true;
            string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-demoEvidenceDir");string directory=at<0?null:args[at+1];
            foreach(int edge in new[]{2,6,4})
            {
                GetWindowRect(window,out var outer);GetClientRect(window,out var before);var r=outer.Rect;
                if(edge==6)r.height+=80;else r.width+=128;
                IntPtr block=Marshal.AllocHGlobal(Marshal.SizeOf<NativeRect>());
                try{Marshal.StructureToPtr(NativeRect.From(r),block,false);SendMessage(window,Sizing,new IntPtr(edge),block);r=Marshal.PtrToStructure<NativeRect>(block).Rect;}
                finally{Marshal.FreeHGlobal(block);}
                SetWindowPos(window,IntPtr.Zero,r.x,r.y,r.width,r.height,0x14);yield return new WaitForSecondsRealtime(.35f);
                GetClientRect(window,out var after);aspect&=Math.Abs(after.Right*9L-after.Bottom*16L)<=16;
                coupled&=before.Right!=after.Right&&before.Bottom!=after.Bottom;
                if(directory!=null){System.IO.Directory.CreateDirectory(directory);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory,"window-edge-"+edge+".png"));yield return null;}
            }
            ShowWindow(window,3);yield return new WaitForSecondsRealtime(.5f);GetClientRect(window,out var max);aspect&=Math.Abs(max.Right*9L-max.Bottom*16L)<=16;
            if(directory!=null){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(directory,"window-maximized.png"));yield return null;}
            ShowWindow(window,9);yield return new WaitForSecondsRealtime(.3f);
            bool state=controller.Battle.IsPaused&&controller.Battle.Clock==clock&&controller.Battle.DP==dp;
            bool canvas=TacticalViewport.Current.Offset.sqrMagnitude<1;
            Debug.Log($"WINDOW_ASPECT_CHECK: aspect={aspect}; axesCoupled={coupled}; pausedState={state}; noBars={canvas}; client={Screen.width}x{Screen.height}");
            Application.Quit(aspect&&coupled&&state&&canvas?0:1);
        }
        [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,uint message,IntPtr w,IntPtr l);
        [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr h,int command);
#endif
    }
}
