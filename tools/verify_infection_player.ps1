param([string]$EvidenceRoot='')
$infectionProjectRoot=Split-Path -Parent $PSScriptRoot
if([string]::IsNullOrWhiteSpace($EvidenceRoot)){$EvidenceRoot=Join-Path $infectionProjectRoot ('artifacts/infection-player-'+[DateTime]::Now.ToString('yyyyMMdd-HHmmss'))}
if(Test-Path -LiteralPath (Join-Path $EvidenceRoot 'result.json')){throw 'Evidence directory already has result.json; choose a fresh directory.'}
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Path $EvidenceRoot -Force | Out-Null
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class InfectionMouse {
 [StructLayout(LayoutKind.Sequential)] public struct Point { public int X,Y; }
 [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left,Top,Right,Bottom; }
 [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr w,out Rect r);
 [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr w,ref Point p);
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr w);
 [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);
 [DllImport("user32.dll")] public static extern void mouse_event(uint f,uint x,uint y,uint data,UIntPtr extra);
 [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
}
"@
[InfectionMouse]::SetProcessDPIAware() | Out-Null
$playerExe=Join-Path $infectionProjectRoot 'Builds/Windows/YesterdayConvoy.exe'
$playerArgs=@('-infectionSmokeTest','-demoEvidenceDir',('"'+$EvidenceRoot+'"'),'-force-d3d11','-screen-width','1600','-screen-height','900','-screen-fullscreen','0','-logFile',('"'+$EvidenceRoot+'.log"'))
$player=Start-Process -FilePath $playerExe -ArgumentList $playerArgs -PassThru
Write-Output ('INFECTION_PLAYER_PID='+$player.Id)
$deadline=[DateTime]::UtcNow.AddSeconds(180);$lastStep=-1
try {
 while([DateTime]::UtcNow -lt $deadline){
  $player.Refresh()
  $resultPath=Join-Path $EvidenceRoot 'result.json'
  if(Test-Path -LiteralPath $resultPath){$result=Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json; if($result.passed){Write-Output ('INFECTION_PLAYER_PASS pixels='+$result.changedPixels);exit 0}else{Write-Output (Get-Content -LiteralPath $resultPath -Raw);exit 1}}
  if($player.HasExited){throw 'Player exited without result.json'}
  $stepPath=Join-Path $EvidenceRoot 'gui-step.json'
  if(Test-Path -LiteralPath $stepPath){
   $step=$null;try{$step=Get-Content -LiteralPath $stepPath -Raw | ConvertFrom-Json}catch{}
   if($null -ne $step -and $step.id -ne $lastStep){
    $handle=$player.MainWindowHandle
    if($handle -ne [IntPtr]::Zero){
     [InfectionMouse]::SetForegroundWindow($handle) | Out-Null; Start-Sleep -Milliseconds 160
     if([InfectionMouse]::GetForegroundWindow() -ne $handle){throw 'Refusing mouse input: own Player window is not foreground'}
     $client=New-Object InfectionMouse+Rect
     if(-not [InfectionMouse]::GetClientRect($handle,[ref]$client)){throw 'Cannot read own Player client bounds'}
     $point=New-Object InfectionMouse+Point
     $point.X=[int]([double]$step.x*($client.Right-$client.Left)/[double]$step.width)
     $point.Y=[int]([double]$step.y*($client.Bottom-$client.Top)/[double]$step.height)
     if(-not [InfectionMouse]::ClientToScreen($handle,[ref]$point)){throw 'Cannot resolve Player point'}
     [InfectionMouse]::SetCursorPos($point.X,$point.Y) | Out-Null;Start-Sleep -Milliseconds 80
     [InfectionMouse]::mouse_event(2,0,0,0,[UIntPtr]::Zero);Start-Sleep -Milliseconds 90
     [InfectionMouse]::mouse_event(4,0,0,0,[UIntPtr]::Zero);Start-Sleep -Milliseconds 90
     Set-Content -LiteralPath (Join-Path $EvidenceRoot 'gui-ack.txt') -Value $step.id -NoNewline
     Write-Output ('GUI_CLICK '+$step.id+' '+$step.name);$lastStep=$step.id
    }
   }
  }
  Start-Sleep -Milliseconds 100
 }
 throw 'Player GUI evidence timeout'
} catch {
 Write-Output $_.Exception.Message
 $player.Refresh();if(-not $player.HasExited){$player.Kill()}
 exit 2
}
