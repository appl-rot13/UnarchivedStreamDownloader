@echo off

set URL=https://github.com/yt-dlp/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip
set ZIP=ffmpeg-master-latest-win64-gpl.zip

echo Downloading %URL%
curl -L -sS -o "%ZIP%" %URL%
if errorlevel 1 (
  echo ERROR: Download failed
  goto error
)

if not exist "%ZIP%" (
  echo ERROR: Downloaded file not found
  goto error
)

echo Extracting %ZIP%
tar -xf "%ZIP%" --strip-components=2 */ffmpeg.exe */ffprobe.exe
if errorlevel 1 (
  echo ERROR: Extract failed
  goto error
)

del "%ZIP%"

pause
exit /b 0

:error
pause
exit /b 1
