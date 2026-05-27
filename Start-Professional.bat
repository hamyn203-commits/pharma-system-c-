@echo off
chcp 65001 >nul
setlocal

set ROOT=%~dp0
set API_URL=http://localhost:5000
set ANALYTICS_URL=http://localhost:8010

echo ========================================================
echo Starting Al-Neda professional stack
echo .NET API:        %API_URL%
echo Python insights: %ANALYTICS_URL%
echo Flutter admin:   Windows desktop
echo ========================================================

echo [1/3] Starting .NET API...
start "AlNeda API" cmd /k "cd /d ""%ROOT%"" && dotnet run --project src\AlNeda.API\AlNeda.API.csproj --urls %API_URL%"

echo [2/3] Starting Python analytics service...
if not exist "%ROOT%python_analytics_service\.venv\Scripts\python.exe" (
  echo Creating Python virtual environment...
  py -3 -m venv "%ROOT%python_analytics_service\.venv"
)

start "AlNeda Python Analytics" cmd /k "cd /d ""%ROOT%python_analytics_service"" && .venv\Scripts\python.exe -m pip install -r requirements.txt && set ""ALNEDA_DB_PATH=%ROOT%pharmacy.db"" && .venv\Scripts\python.exe -m uvicorn app.main:app --reload --host 127.0.0.1 --port 8010"

echo [3/3] Starting Flutter admin app...
timeout /t 6 /nobreak >nul
start "AlNeda Flutter Admin" cmd /k "cd /d ""%ROOT%"" && flutter run -d windows --dart-define=ALNEDA_API_BASE_URL=%API_URL% --dart-define=ALNEDA_ANALYTICS_API_BASE_URL=%ANALYTICS_URL%"

echo Done. Keep the opened windows running while you use the app.
timeout /t 3 >nul
endlocal
