# Al-Neda Python Analytics Service

FastAPI read-only service for analytics and operational recommendations.

## Run

```powershell
cd python_analytics_service
py -3 -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
$env:ALNEDA_DB_PATH = "..\pharmacy.db"
.\.venv\Scripts\python.exe -m uvicorn app.main:app --reload --host 127.0.0.1 --port 8010
```

## Endpoints

- `GET /health`
- `GET /insights/dashboard`
- `GET /insights/inventory`
