@echo off
SET xOS=x86
SET VER_VS=15.0
IF Defined PROCESSOR_ARCHITEW6432 (SET xOS=x64) ELSE IF "%PROCESSOR_ARCHITECTURE%"=="AMD64" SET xOS=x64

set NET="C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\"
IF NOT EXIST %NET% (set NET="C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\")
rem Если установлено только VSCode
rem IF NOT EXIST %NET% (
rem   set path=%path%;"C:\Documents and Settings\ssersn\.vscode\extensions\ms-vscode.csharp-1.14.0\.omnisharp\"
rem   set NET="C:\Documents and Settings\ssersn\.vscode\extensions\ms-vscode.csharp-1.14.0\.omnisharp\msbuild\15.0\Bin\"
rem )
IF NOT EXIST %NET% (echo "Путь до msbuild не найден...")

set path=%path%;%NET%