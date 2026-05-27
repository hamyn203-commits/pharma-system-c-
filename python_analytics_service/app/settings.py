from __future__ import annotations

import os
from dataclasses import dataclass
from pathlib import Path


SERVICE_ROOT = Path(__file__).resolve().parents[1]
WORKSPACE_ROOT = SERVICE_ROOT.parent


def _resolve_database_path() -> Path:
    configured = os.getenv("ALNEDA_DB_PATH")
    if configured:
        path = Path(configured).expanduser()
        return path if path.is_absolute() else (SERVICE_ROOT / path).resolve()

    candidates = [
        WORKSPACE_ROOT / "pharmacy.db",
        WORKSPACE_ROOT / "src" / "AlNeda.API" / "bin" / "Debug" / "net9.0" / "pharmacy.db",
        WORKSPACE_ROOT / "src" / "AlNeda.API" / "bin" / "Release" / "net9.0" / "pharmacy.db",
    ]
    for candidate in candidates:
        if candidate.exists():
            return candidate.resolve()

    return candidates[0].resolve()


def _int_from_env(name: str, default: int) -> int:
    raw = os.getenv(name)
    if raw is None:
        return default
    try:
        return int(raw)
    except ValueError:
        return default


def _cors_origins() -> list[str]:
    raw = os.getenv(
        "ALNEDA_CORS_ORIGINS",
        "http://localhost:5200,http://127.0.0.1:5200,http://localhost:5000",
    )
    return [origin.strip() for origin in raw.split(",") if origin.strip()]


@dataclass(frozen=True)
class Settings:
    app_name: str = "Al-Neda Python Analytics"
    database_path: Path = _resolve_database_path()
    low_stock_threshold: int = _int_from_env("ALNEDA_LOW_STOCK_THRESHOLD", 10)
    expiry_warning_days: int = _int_from_env("ALNEDA_EXPIRY_WARNING_DAYS", 30)
    cors_origins: tuple[str, ...] = tuple(_cors_origins())


settings = Settings()
