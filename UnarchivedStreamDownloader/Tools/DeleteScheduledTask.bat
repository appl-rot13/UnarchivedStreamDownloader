@echo off

schtasks /delete /f /tn "UnarchivedStreamDownloaderHourly"
schtasks /delete /f /tn "UnarchivedStreamDownloaderOnLogon"
pause
