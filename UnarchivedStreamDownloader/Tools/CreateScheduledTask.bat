@echo off

set ScriptDir=%~dp0
set ScriptPath=%ScriptDir%UnarchivedStreamDownloader.vbs

schtasks /create /tn "UnarchivedStreamDownloaderHourly"  /tr "%ScriptPath%" /sc HOURLY
schtasks /create /tn "UnarchivedStreamDownloaderOnLogon" /tr "%ScriptPath%" /sc ONLOGON
pause
