-- =============================================
-- AlNeda - Reporting Views for pharmacy.db
-- These views simplify complex report queries
-- =============================================

-- View: Pharmacy account summary (running balance + totals)
CREATE VIEW IF NOT EXISTS vw_pharmacy_accounts AS
SELECT
    p.id AS PharmacyId,
    p.name AS PharmacyName,
    p.balance AS CurrentBalance,
    COALESCE(o.TotalOrderAmount, 0) AS TotalOrders,
    COALESCE(py.TotalPayments, 0) AS TotalPayments,
    COALESCE(r.TotalReturns, 0) AS TotalReturns,
    GREATEST(
        COALESCE(o.LastOrder, '1970-01-01'),
        COALESCE(py.LastPayment, '1970-01-01'),
        COALESCE(r.LastReturn, '1970-01-01')
    ) AS LastActivityDate
FROM pharmacies p
LEFT JOIN (
    SELECT pharmacy_id,
           SUM(final_total) AS TotalOrderAmount,
           MAX(created_at) AS LastOrder
    FROM orders
    WHERE status != 'cancelled'
    GROUP BY pharmacy_id
) o ON p.id = o.pharmacy_id
LEFT JOIN (
    SELECT pharmacy_id,
           SUM(amount) AS TotalPayments,
           MAX(date) AS LastPayment
    FROM payments
    GROUP BY pharmacy_id
) py ON p.id = py.pharmacy_id
LEFT JOIN (
    SELECT pharmacy_id,
           SUM(total_amount) AS TotalReturns,
           MAX(created_at) AS LastReturn
    FROM returns
    WHERE status != 'cancelled'
    GROUP BY pharmacy_id
) r ON p.id = r.pharmacy_id;

-- View: Daily sales summary
CREATE VIEW IF NOT EXISTS vw_daily_sales AS
SELECT
    DATE(created_at) AS SaleDate,
    COUNT(*) AS OrderCount,
    SUM(final_total) AS TotalSales,
    SUM(discount) AS TotalDiscounts,
    SUM(amount_paid) AS TotalCollected
FROM orders
WHERE status != 'cancelled'
GROUP BY DATE(created_at)
ORDER BY SaleDate DESC;

-- View: Top selling products
CREATE VIEW IF NOT EXISTS vw_top_products AS
SELECT
    p.id AS ProductId,
    p.name AS ProductName,
    SUM(oi.quantity) AS TotalQuantity,
    SUM(oi.total_price) AS TotalRevenue,
    p.quantity AS CurrentStock
FROM order_items oi
JOIN orders o ON oi.order_id = o.id
JOIN products p ON oi.product_id = p.id
WHERE o.status != 'cancelled'
GROUP BY p.id
ORDER BY TotalRevenue DESC;

-- View: Monthly sales trend
CREATE VIEW IF NOT EXISTS vw_monthly_sales AS
SELECT
    strftime('%Y-%m', created_at) AS Month,
    COUNT(*) AS OrderCount,
    SUM(final_total) AS TotalSales,
    SUM(discount) AS TotalDiscounts,
    SUM(amount_paid) AS TotalCollected,
    COUNT(DISTINCT pharmacy_id) AS ActivePharmacies
FROM orders
WHERE status != 'cancelled'
GROUP BY strftime('%Y-%m', created_at)
ORDER BY Month DESC;
