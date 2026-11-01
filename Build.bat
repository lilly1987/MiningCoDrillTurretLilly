@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo .NET SDK was not found. Install the SDK required by the project.
    pause
    exit /b 1
)

echo Building MiningCo. DrillTurret for RimWorld 1.7...
echo Note: the project build closes any running RimWorld.exe process.
dotnet build "Source\MiningCo. DrillTurret\MiningCo. DrillTurret.csproj" --configuration Release
set "BUILD_EXIT_CODE=%ERRORLEVEL%"

if not "%BUILD_EXIT_CODE%"=="0" (
    echo Build failed with exit code %BUILD_EXIT_CODE%.
    pause
    exit /b %BUILD_EXIT_CODE%
)

echo Build succeeded. Output: 1.7\Assemblies
pause
exit /b 0
