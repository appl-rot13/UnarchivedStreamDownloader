@echo off

schtasks /delete /f /tn "UnarchivedStreamDownloaderHourly"
schtasks /delete /f /tn "UnarchivedStreamDownloaderOnStart"
pause
