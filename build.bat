@echo off
REM Build SweepDrive.exe from source using the .NET Framework compiler shipped with Windows.
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
"%CSC%" /nologo /target:winexe /out:SweepDrive.exe /win32icon:assets\sweepdrive.ico /win32manifest:app.manifest ^
  /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:Microsoft.VisualBasic.dll ^
  src\SweepDrive.cs
echo Build complete: SweepDrive.exe
