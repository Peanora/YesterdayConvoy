@echo off
cd /d "%~dp0..\Builds\Windows"
start "" "YesterdayConvoy.exe" -blackForest -force-d3d11 -screen-fullscreen 0 -screen-width 1600 -screen-height 900
