@echo off
set "sarkazTrialExe=%~dp0YesterdayConvoy.exe"
if not exist "%sarkazTrialExe%" set "sarkazTrialExe=%~dp0..\Builds\Windows\YesterdayConvoy.exe"
if not exist "%sarkazTrialExe%" (
  echo Build the Windows player before starting this trial.
  pause
  exit /b 1
)
start "" "%sarkazTrialExe%" -sarkazTrial -screen-fullscreen 0 -screen-width 1600 -screen-height 900
