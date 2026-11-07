@echo off
cd /d "%~dp0.."
start "" "Builds\Windows\YesterdayConvoy.exe" -marineTide -force-d3d11
