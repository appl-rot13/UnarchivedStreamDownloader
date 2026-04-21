@echo off

where deno >nul 2>&1
if not ERRORLEVEL 1 (
  deno upgrade
)

yt-dlp.exe --update
pause
