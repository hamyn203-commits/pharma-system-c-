from __future__ import annotations

import sqlite3
from dataclasses import dataclass
from datetime import date, datetime, timedelta
from decimal import Decimal
from typing import Any

from .database import table_exists
from .settings import Settings


ACTIVE_ORDER_STATUSES = {"pending", "reviewed", "in_store", "with_driver", "on_the_way"}


@dataclass(frozen=True)
class InsightContext:
    connection: sqlite3.Connection
    settings: Settings


def build_dashboard_insights(context: InsightContext) -> dict[str, Any]:
    products = _rows(context.connection, "select * from Products where coalesce(IsActive, 1) = 1")
    orders = _rows(context.connection, "select * from Orders")
    pharmacies = _rows(context.connection, "select * from Pharmacies")

    today = date.today()
    month_start = today.replace(day=1)
    expiring_cutoff = today + timedelta(days=context.settings.expiry_warning_days)

    expiring_products = [
        product
        for product in products
        if _date_between(_parse_date(product["ExpiryDate"]), today, expiring_cutoff)
    ]
    expired_products = [
        product
        for product in products
        if (_parse_date(product["ExpiryDate"]) or date.max) < today
    ]
    low_stock_products = [
        product for product in products if _int(product["Quantity"]) <= context.settings.low_stock_threshold
    ]

    return {
        "databasePath": str(context.settings.database_path),
        "generatedAt": datetime.now().isoformat(timespec="seconds"),
        "kpis": {
            "products": len(products),
            "pharmacies": len(pharmacies),
            "activeOrders": _active_order_count(orders),
            "dailySales": _sales_total(orders, today, today),
            "monthlySales": _sales_total(orders, month_start, today),
            "totalReceivables": _positive_balance_total(pharmacies),
            "lowStock": len(low_stock_products),
            "expiringSoon": len(expiring_products),
            "expired": len(expired_products),
        },
        "alerts": {
            "lowStock": [_product_alert(product) for product in low_stock_products[:10]],
            "expiringSoon": [_product_alert(product) for product in expiring_products[:10]],
            "expired": [_product_alert(product) for product in expired_products[:10]],
        },
        "recommendations": _reorder_recommendations(context.connection, products),
        "salesForecast": _simple_sales_forecast(orders),
    }


def build_inventory_insights(context: InsightContext) -> dict[str, Any]:
    products = _rows(context.connection, "select * from Products where coalesce(IsActive, 1) = 1")
    recommendations = _reorder_recommendations(context.connection, products)
    return {
        "generatedAt": datetime.now().isoformat(timespec="seconds"),
        "lowStockThreshold": context.settings.low_stock_threshold,
        "recommendations": recommendations,
    }


def _rows(connection: sqlite3.Connection, query: str, parameters: tuple[Any, ...] = ()) -> list[sqlite3.Row]:
    return list(connection.execute(query, parameters).fetchall())


def _active_order_count(orders: list[sqlite3.Row]) -> int:
    return sum(1 for order in orders if str(order["Status"]).lower() in ACTIVE_ORDER_STATUSES)


def _sales_total(orders: list[sqlite3.Row], start: date, end: date) -> float:
    total = Decimal("0")
    for order in orders:
        created_at = _parse_date(order["CreatedAt"])
        if created_at and start <= created_at <= end:
            total += _decimal(order["FinalTotal"])
    return float(total)


def _positive_balance_total(pharmacies: list[sqlite3.Row]) -> float:
    total = Decimal("0")
    for pharmacy in pharmacies:
        balance = _decimal(pharmacy["Balance"])
        if balance > 0:
            total += balance
    return float(total)


def _reorder_recommendations(
    connection: sqlite3.Connection,
    products: list[sqlite3.Row],
) -> list[dict[str, Any]]:
    sales_by_product = _sales_by_product(connection)
    recommendations: list[dict[str, Any]] = []

    for product in products:
        product_id = _int(product["Id"])
        quantity = _int(product["Quantity"])
        recent_sales = sales_by_product.get(product_id, 0)
        daily_velocity = recent_sales / 30
        suggested_quantity = max(0, round((daily_velocity * 21) - quantity))
        urgency_score = (recent_sales * 2) - quantity

        if quantity <= 10 or suggested_quantity > 0:
            recommendations.append(
                {
                    "productId": product_id,
                    "name": product["Name"],
                    "quantity": quantity,
                    "salesLast30Days": recent_sales,
                    "dailyVelocity": round(daily_velocity, 2),
                    "suggestedQuantity": suggested_quantity,
                    "urgencyScore": round(urgency_score, 2),
                }
            )

    recommendations.sort(key=lambda item: item["urgencyScore"], reverse=True)
    return recommendations[:12]


def _sales_by_product(connection: sqlite3.Connection) -> dict[int, int]:
    if not table_exists(connection, "OrderItems"):
        return {}

    thirty_days_ago = datetime.now() - timedelta(days=30)
    rows = connection.execute(
        """
        select oi.ProductId, sum(oi.Quantity) as QuantitySold
        from OrderItems oi
        inner join Orders o on o.Id = oi.OrderId
        where o.CreatedAt >= ?
        group by oi.ProductId
        """,
        (thirty_days_ago.isoformat(),),
    ).fetchall()
    return {_int(row["ProductId"]): _int(row["QuantitySold"]) for row in rows}


def _simple_sales_forecast(orders: list[sqlite3.Row]) -> dict[str, Any]:
    daily_totals: dict[date, Decimal] = {}
    for order in orders:
        created_at = _parse_date(order["CreatedAt"])
        if created_at is None:
            continue
        daily_totals[created_at] = daily_totals.get(created_at, Decimal("0")) + _decimal(order["FinalTotal"])

    if not daily_totals:
        return {"method": "moving_average_7d", "next7DaysTotal": 0.0, "dailyAverage": 0.0}

    last_day = max(daily_totals)
    window = [daily_totals.get(last_day - timedelta(days=offset), Decimal("0")) for offset in range(7)]
    average = sum(window, Decimal("0")) / Decimal(len(window))
    return {
        "method": "moving_average_7d",
        "next7DaysTotal": float(average * Decimal("7")),
        "dailyAverage": float(average),
    }


def _product_alert(product: sqlite3.Row) -> dict[str, Any]:
    return {
        "id": _int(product["Id"]),
        "name": product["Name"],
        "quantity": _int(product["Quantity"]),
        "expiryDate": product["ExpiryDate"],
        "unitPrice": float(_decimal(product["UnitPrice"])),
    }


def _date_between(value: date | None, start: date, end: date) -> bool:
    return value is not None and start <= value <= end


def _parse_date(value: Any) -> date | None:
    if value in (None, ""):
        return None
    text = str(value).strip()
    if not text:
        return None

    normalized = text.replace("Z", "+00:00")
    try:
        return datetime.fromisoformat(normalized).date()
    except ValueError:
        pass

    for pattern in ("%Y-%m-%d", "%d/%m/%Y", "%m/%d/%Y"):
        try:
            return datetime.strptime(text, pattern).date()
        except ValueError:
            continue
    return None


def _decimal(value: Any) -> Decimal:
    if value in (None, ""):
        return Decimal("0")
    return Decimal(str(value))


def _int(value: Any) -> int:
    if value in (None, ""):
        return 0
    return int(value)
