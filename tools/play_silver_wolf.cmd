@echo off
setlocal
set "PLAYER=%~dp0..\Builds\Windows\YesterdayConvoy.exe"
if exist "%~dp0YesterdayConvoy.exe" set "PLAYER=%~dp0YesterdayConvoy.exe"
if not exist "%PLAYER%" exit /b 1
start "" "%PLAYER%" -silverWolfTrial
