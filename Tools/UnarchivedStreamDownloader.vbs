Option Explicit

Dim exeDir
Dim exePath
Dim objShell

exeDir = Left(WScript.ScriptFullName, Len(WScript.ScriptFullName) - Len(WScript.ScriptName))
exePath = exeDir & "UnarchivedStreamDownloader.exe"

Set objShell = CreateObject("WScript.Shell")

objShell.CurrentDirectory = exeDir
objShell.Run """" & exePath & """", 7, False

Set objShell = Nothing
