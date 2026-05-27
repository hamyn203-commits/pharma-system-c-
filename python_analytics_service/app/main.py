from __future__ import annotations

from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware

from .database import connect
from .insights import InsightContext, build_dashboard_insights, build_inventory_insights
from .settings import settings


app = FastAPI(
    title=settings.app_name,
    version="0.1.0",
    description="Read-only Python analytics layer for Al-Neda Pharmacy Warehouse.",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=list(settings.cors_origins),
    allow_credentials=True,
    allow_methods=["GET"],
    allow_headers=["*"],
)


@app.get("/health")
def health() -> dict[str, object]:
    return {
        "status": "ok",
        "service": settings.app_name,
        "databasePath": str(settings.database_path),
        "databaseExists": settings.database_path.exists(),
    }


@app.get("/insights/dashboard")
def dashboard_insights() -> dict[str, object]:
    try:
        with connect(settings.database_path) as connection:
            return build_dashboard_insights(InsightContext(connection, settings))
    except FileNotFoundError as exc:
        raise HTTPException(status_code=503, detail=str(exc)) from exc


@app.get("/insights/inventory")
def inventory_insights() -> dict[str, object]:
    try:
        with connect(settings.database_path) as connection:
            return build_inventory_insights(InsightContext(connection, settings))
    except FileNotFoundError as exc:
        raise HTTPException(status_code=503, detail=str(exc)) from exc
