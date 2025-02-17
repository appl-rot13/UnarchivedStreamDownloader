@echo off

set ScriptDir=%~dp0
set ScriptPath=%ScriptDir%UnarchivedStreamDownloader.vbs

schtasks /create /tn "UnarchivedStreamDownloader" /tr "%ScriptPath%" /sc HOURLY
pause
