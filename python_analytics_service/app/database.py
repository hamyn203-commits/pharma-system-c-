from __future__ import annotations

import sqlite3
from collections.abc import Iterator
from contextlib import contextmanager
from pathlib import Path


@contextmanager
def connect(database_path: Path) -> Iterator[sqlite3.Connection]:
    if not database_path.exists():
        raise FileNotFoundError(f"Database file was not found: {database_path}")

    connection = sqlite3.connect(database_path)
    connection.row_factory = sqlite3.Row
    try:
        yield connection
    finally:
        connection.close()


def table_exists(connection: sqlite3.Connection, table_name: str) -> bool:
    row = connection.execute(
        "select name from sqlite_master where type = 'table' and name = ?",
        (table_name,),
    ).fetchone()
    return row is not None
