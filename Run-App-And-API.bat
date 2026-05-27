@echo off
chcp 65001 >nul
setlocal

set "API_URL=http://localhost:5000"
set "ANALYTICS_URL=http://localhost:8010"

pushd "%~dp0"

echo ========================================================
echo Al-Neda startup
echo API: %API_URL%
echo App: .NET Admin by default
echo ========================================================
echo.

if not exist "src\AlNeda.API\AlNeda.API.csproj" goto ApiMissing

echo [1/2] Starting API...
netstat -ano | findstr ":5000" | findstr "LISTENING" >nul
if errorlevel 1 goto StartApi
echo API is already running on %API_URL%.
goto WaitForApi

:StartApi
start "AlNeda API" /D "%~dp0" cmd /k dotnet run --project src\AlNeda.API\AlNeda.API.csproj --urls %API_URL%

:WaitForApi
echo Waiting for API startup...
timeout /t 5 /nobreak >nul

echo [2/2] Starting app...
if /I "%~1"=="flutter" if exist "windows\CMakeLists.txt" goto StartFlutter
if exist "src\AlNeda.Admin\AlNeda.Admin.csproj" goto StartDotnetAdmin
goto AppMissing

:StartFlutter
start "AlNeda Flutter App" /D "%~dp0" cmd /k flutter run -d windows --dart-define=ALNEDA_API_BASE_URL=%API_URL% --dart-define=ALNEDA_ANALYTICS_API_BASE_URL=%ANALYTICS_URL%
goto Done

:StartDotnetAdmin
start "AlNeda Admin App" /D "%~dp0" cmd /k dotnet run --project src\AlNeda.Admin\AlNeda.Admin.csproj
goto Done

:ApiMissing
echo ERROR: API project was not found.
echo Expected: src\AlNeda.API\AlNeda.API.csproj
pause
popd
exit /b 1

:AppMissing
echo ERROR: No app project was found.
echo Expected src\AlNeda.Admin\AlNeda.Admin.csproj
pause
popd
exit /b 1

:Done
echo.
echo Started. Keep the opened windows running.
timeout /t 3 >nul
popd
endlocal
