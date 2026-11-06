@echo off
set "mudrockTrialExe=%~dp0YesterdayConvoy.exe"
if not exist "%mudrockTrialExe%" set "mudrockTrialExe=%~dp0..\Builds\Windows\YesterdayConvoy.exe"
if not exist "%mudrockTrialExe%" (
  echo Build the Windows player before starting this trial.
  pause
  exit /b 1
)
start "" "%mudrockTrialExe%" -mudrockCultivation -screen-fullscreen 0 -screen-width 1600 -screen-height 900
