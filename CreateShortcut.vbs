' CreateShortcut.vbs - Creates a desktop shortcut for TextExtractor
' Run this script after building the application
' Usage: Double-click or run: cscript CreateShortcut.vbs

Option Explicit

Dim WScriptShell, fso, scriptDir, exePath, iconPath, workingDir
Dim desktopPath, startMenuPath, shortcut
Dim possiblePaths, i

Set WScriptShell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")

' Get script directory
scriptDir = fso.GetParentFolderName(WScript.ScriptFullName)

' Use dist folder directly
exePath = "G:\Projects\Git\TextExtractorWin\dist\TextExtractorWin.exe"

If Not fso.FileExists(exePath) Then
    MsgBox "Could not find TextExtractorWin.exe at:" & vbCrLf & vbCrLf & _
           exePath & vbCrLf & vbCrLf & _
           "Please run CreateDist.ps1 first.", vbExclamation, "TextExtractor Shortcut"
    WScript.Quit 1
End If

' Get the full absolute path
exePath = fso.GetAbsolutePathName(exePath)
workingDir = fso.GetParentFolderName(exePath)

' Use the exe's embedded icon (set via ApplicationIcon in .csproj)
iconPath = exePath

' Create desktop shortcut
desktopPath = WScriptShell.SpecialFolders("Desktop")
Set shortcut = WScriptShell.CreateShortcut(desktopPath & "\TextExtractor.lnk")
shortcut.TargetPath = exePath
shortcut.WorkingDirectory = workingDir
shortcut.IconLocation = iconPath & ",0"
shortcut.Description = "TextExtractor - OCR text extraction for Windows 11"
shortcut.WindowStyle = 1  ' Normal window
shortcut.Save

' Create Start Menu shortcut
startMenuPath = WScriptShell.SpecialFolders("StartMenu") & "\Programs"
Set shortcut = WScriptShell.CreateShortcut(startMenuPath & "\TextExtractor.lnk")
shortcut.TargetPath = exePath
shortcut.WorkingDirectory = workingDir
shortcut.IconLocation = iconPath & ",0"
shortcut.Description = "TextExtractor - OCR text extraction for Windows 11"
shortcut.WindowStyle = 1  ' Normal window
shortcut.Save

MsgBox "Shortcuts created successfully!" & vbCrLf & vbCrLf & _
       "Desktop: " & desktopPath & "\TextExtractor.lnk" & vbCrLf & _
       "Start Menu: " & startMenuPath & "\TextExtractor.lnk" & vbCrLf & vbCrLf & _
       "Target: " & exePath & vbCrLf & _
       "Icon: " & iconPath, vbInformation, "TextExtractor Shortcut"

Set shortcut = Nothing
Set fso = Nothing
Set WScriptShell = Nothing
