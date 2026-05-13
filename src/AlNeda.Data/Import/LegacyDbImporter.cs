using Microsoft.Data.Sqlite;
using AlNeda.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlNeda.Data.Import;

public class LegacyDbImporter
{
    private readonly string _legacyDbPath;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private const int BatchSize = 100;

    public LegacyDbImporter(string legacyDbPath, IDbContextFactory<AppDbContext> contextFactory)
    {
        _legacyDbPath = legacyDbPath;
        _contextFactory = contextFactory;
    }

    public record ImportResult(int RowsImported, int RowsSkipped, List<string> Errors);

    public async Task<ImportResult> ImportAsync()
    {
        var errors = new List<string>();
        int imported = 0;

        try
        {
            using var legacyConn = new SqliteConnection($"Data Source={_legacyDbPath}");
            await legacyConn.OpenAsync();

            using var target = await _contextFactory.CreateDbContextAsync();
            await target.Database.EnsureCreatedAsync();

            // Start an atomic transaction for the whole process
            using var transaction = await target.Database.BeginTransactionAsync();
            try
            {
                imported += await ImportUsersAsync(legacyConn, target, errors);
                imported += await ImportCategoriesAsync(legacyConn, target, errors);
                imported += await ImportSuppliersAsync(legacyConn, target, errors);
                imported += await ImportProductsAsync(legacyConn, target, errors);
                imported += await ImportPharmaciesAsync(legacyConn, target, errors);
                imported += await ImportPurchasesAsync(legacyConn, target, errors);
                imported += await ImportPurchaseItemsAsync(legacyConn, target, errors);
                imported += await ImportOrdersAsync(legacyConn, target, errors);
                imported += await ImportOrderItemsAsync(legacyConn, target, errors);
                imported += await ImportOrderStatusHistoryAsync(legacyConn, target, errors);
                imported += await ImportPaymentsAsync(legacyConn, target, errors);
                imported += await ImportReturnsAsync(legacyConn, target, errors);
                imported += await ImportReturnItemsAsync(legacyConn, target, errors);
                imported += await ImportAuditLogsAsync(legacyConn, target, errors);

                await target.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                errors.Add($"Global Import Failure: {ex.Message}");
                return new ImportResult(0, 0, errors);
            }

            return new ImportResult(imported, 0, errors);
        }
        catch (Exception ex)
        {
            errors.Add($"Connection Failure: {ex.Message}");
            return new ImportResult(0, 0, errors);
        }
    }

    private static decimal? ToDecimal(object? val) => val is DBNull or null ? null : Convert.ToDecimal(val);
    private static decimal ToDecimalDef(object? val, decimal def = 0) => ToDecimal(val) ?? def;
    private static int ToInt(object? val) => val is DBNull or null ? 0 : Convert.ToInt32(val);
    private static string? ToStr(object? val) => val is DBNull or null ? null : Convert.ToString(val);
    private static string ToStrDef(object? val, string def = "") => ToStr(val) ?? def;
    private static DateTime ParseDate(object? val) => val is DBNull or null ? DateTime.Now : DateTime.TryParse(Convert.ToString(val), out var d) ? d : DateTime.Now;
    private static DateTime? ParseDateN(object? val) => val is DBNull or null ? null : DateTime.TryParse(Convert.ToString(val), out var d) ? d : null;
    private static bool ToBool(object? val) => val is not DBNull and not null && (Convert.ToInt32(val) != 0);

    private async Task<int> BulkInsertAsync<T>(SqliteConnection legacy, AppDbContext target, string table, Func<System.Data.Common.DbDataReader, T> factory, List<string> errors) where T : class
    {
        int count = 0;
        try
        {
            if (!await TableExistsAsync(legacy, table))
            {
                errors.Add($"Skipped missing legacy table {table}");
                return 0;
            }

            using var cmd = legacy.CreateCommand();
            cmd.CommandText = $"SELECT * FROM [{table}]";
            using var reader = await cmd.ExecuteReaderAsync();
            var batch = new List<T>();
            while (await reader.ReadAsync())
            {
                try
                {
                    var item = factory(reader);
                    if (item != null)
                    {
                        batch.Add(item);
                    }
                    
                    if (batch.Count >= BatchSize)
                    {
                        target.Set<T>().AddRange(batch);
                        await target.SaveChangesAsync();
                        count += batch.Count;
                        batch.Clear();
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"{table} row error: {ex.Message}");
                }
            }
            if (batch.Count > 0)
            {
                target.Set<T>().AddRange(batch);
                await target.SaveChangesAsync();
                count += batch.Count;
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to read table {table}: {ex.Message}");
            throw; // Re-throw to trigger global rollback in ImportAsync
        }
        return count;
    }

    private static async Task<bool> TableExistsAsync(SqliteConnection legacy, string table)
    {
        using var cmd = legacy.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name";
        cmd.Parameters.AddWithValue("$name", table);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }

    private async Task<int> ImportUsersAsync(SqliteConnection legacy, AppDbContext target, List<string> errors)
    {
        return await BulkInsertAsync(legacy, target, "users", r => new User
        {
            Id = ToInt(r["id"]),
            Username = ToStrDef(r["username"], "unknown"),
            Password = ToStrDef(r["password"], ""),
            Role = ToStrDef(r["role"], "admin"),
            CreatedAt = ParseDate(r["created_at"])
        }, errors);
    }

    private async Task<int> ImportCategoriesAsync(SqliteConnection legacy, AppDbContext target, List<string> errors)
    {
        return await BulkInsertAsync(legacy, target, "categories", r => new Category
        {
            Id = ToInt(r["id"]),
            Name = ToStrDef(r["name"], "غير مصنف"),
            CreatedAt = ParseDate(r["created_at"])
        }, errors);
    }

    private async Task<int> ImportSuppliersAsync(SqliteConnection legacy, AppDbContext target, List<string> errors)
    {
        return await BulkInsertAsync(legacy, target, "suppliers", r => new Supplier
        {
            Id = ToInt(r["id"]),
            Name = ToStrDef(r["name"], "مورد مجهول"),
            Phone = ToStrDef(r["phone"]),
            Address = ToStrDef(r["address"]),
            Company = ToStrDef(r["company"]),
            Balance = ToDecimalDef(r["balance"]),
            Notes = ToStrDef(r["notes"]),
            CreatedAt = ParseDate(r["created_at"])
        }, errors);
    }

    private async Task<int> ImportProductsAsync(SqliteConnection legacy, AppDbContext target, List<string> errors)
    {
        return await BulkInsertAsync(legacy, target, "products", r => new Product
        {
            Id = ToInt(r["id"]),
            Name = ToStrDef(r["name"], "منتج بدون اسم"),
            Barcode = ToStr(r["barcode"]),
            Category = ToStrDef(r["category"], "عام"),
            CategoryId = r["category_id"] is DBNull or null ? null : ToInt(r["category_id"]),
            Company = ToStrDef(r["company"], "غير محدد"),
            Quantity = ToInt(r["quantity"]),
            UnitPrice = ToDecimalDef(r["unit_price"]),
            ExpiryDate = ToStr(r["expiry_date"]),
            ImagePath = ToStr(r["image_path"]),
            ImageUrl = ToStr(r["image_url"]),
            IsActive = ToBool(r["is_active"]) ? 1 : 0,
            ProductImagesJson = ToStrDef(r["product_images_json"], "[]"),
            Description = ToStrDef(r["description"]),
            CreatedAt = ParseDate(r["created_at"]),
            UpdatedAt = ParseDate(r["updated_at"])
        }, errors);
    }

    private async Task<int> ImportPharmaciesAsync(SqliteConnection legacy, AppDbContext target, List<string> errors)
    {
        return await BulkInsertAsync(legacy, target, "pharmacies", r => new Pharmacy
        {
            Id = ToInt(r["id"]),
            Name = ToStrDef(r["name"], "صيدلية غير معروفة"),
            Address = ToStrDef(r["address"]),
            Phone = ToStrDef(r["phone"]),
            Balance = ToDecimalDef(r["balance"]),
            CreatedAt = ParseDate(r["created_at"]),
            AccountStatus = ToStrDef(r["account_status"], "active"),
            ApprovedAt = ParseDateN(r["approved_at"]),
            BlockedAt = ParseDateN(r["blocked_at"]),
            LastLoginAt = ParseDateN(r["last_login_at"]),
            DeviceId = ToStr(r["device_id"])
        }, errors);
    }

    private async Task<int> ImportPurchasesAsync(SqliteConnection legacy, AppDbContext target, List<string> errors)
    {
        return await BulkInsertAsync(legacy, target, "purchases", r => new Purchase
        {
            Id = ToInt(r["id"]),
            InvoiceNumber = ToStrDef(r["invoice_number"], "INV-000"),
            SupplierId = ToInt(r["supplier_id"]),
            TotalAmount = ToDecimalDef(r["total_amount"]),
            AmountPaid = ToDecimalDef(r["amount_paid"]),
            RemainingAmount = ToDecimalDef(r["remaining_amount"]),
            Status = ToStrDef(r["status"], "unpaid"),
            Notes = ToStrDef(r["notes"]),
            CreatedAt = ParseDate(r["created_at"])
        }, errors);
    }

    private async Task<int> ImportPurchaseItemsAsync(SqliteConnection legacy, AppDbContext target, List<string> errors)
    {
        return await BulkInsertAsync(legacy, target, "purchase_items", r => new PurchaseItem
        {
            Id = ToInt(r["id"]),
            PurchaseId = ToInt(r["purchase_id"]),
            ProductId = ToInt(r["product_id"]),
            Quantity = ToInt(r["quantity"]),
            UnitCost = ToDecimalDef(r["unit_cost"]),
            TotalCost = ToDecimalDef(r["total_cost"])
        }, errors);
    }

    private async Task<int> ImportOrdersAsync(SqliteConnection legacy, AppDbContext target, List<string> errors)
    {
        return await BulkInsertAsync(legacy, target, "orders", r => new Order
        {
            Id = ToInt(r["id"]),
            OrderNumber = ToStrDef(r["order_number"], "ORD-000"),
            PharmacyId = ToInt(r["pharmacy_id"]),
            TotalAmount = ToDecimalDef(r["total_amount"]),
            Discount = ToDecimalDef(r["discount"]),
            DiscountType = ToStrDef(r["discount_type"], "value"),
            FinalTotal = ToDecimalDef(r["final_total"]),
            BalanceBefore = ToDecimalDef(r["balance_before"]),
            BalanceAfter = ToDecimalDef(r["balance_after"]),
            Status = ToStrDef(r["status"], "pending"),
            DeliveryPerson = ToStrDef(r["delivery_person"]),
            Notes = ToStrDef(r["notes"]),
            LastStatusUpdate = ParseDateN(r["last_status_update"]),
            ExpectedDeliveryNote = ToStrDef(r["expected_delivery_note"]),
            PaymentStatus = ToStrDef(r["payment_status"], "unpaid"),
            PaymentType = ToStrDef(r["payment_type"]),
            AmountPaid = ToDecimalDef(r["amount_paid"]),
            RemainingAmount = ToDecimalDef(r["remaining_amount"]),
            PaymentNotes = ToStrDef(r["payment_notes"]),
            CreatedAt = ParseDate(r["created_at"])
        }, errors);
    }

    private async Task<int> ImportOrderItemsAsync(SqliteConnection legacy, AppDbContext target, List<string> errors)
    {
        return await BulkInsertAsync(legacy, target, "order_items", r => new OrderItem
        {
            Id = ToInt(r["id"]),
            OrderId = ToInt(r["order_id"]),
            ProductId = ToInt(r["product_id"]),
            Quantity = ToInt(r["quantity"]),
            UnitPrice = ToDecimalDef(r["unit_price"]),
            TotalPrice = ToDecimalDef(r["total_price"])
        }, errors);
    }

    private async Task<int> ImportOrderStatusHistoryAsync(SqliteConnection legacy, AppDbContext target, List<string> errors)
    {
        return await BulkInsertAsync(legacy, target, "order_status_history", r => new OrderStatusHistory
        {
            Id = ToInt(r["id"]),
            OrderId = ToInt(r["order_id"]),
            OldStatus = ToStrDef(r["old_status"], "unknown"),
            NewStatus = ToStrDef(r["new_status"], "unknown"),
            Note = ToStrDef(r["note"]),
            CreatedAt = ParseDate(r["created_at"])
        }, errors);
    }

    private async Task<int> ImportPaymentsAsync(SqliteConnection legacy, AppDbContext target, List<string> errors)
    {
        return await BulkInsertAsync(legacy, target, "payments", r => new Payment
        {
            Id = ToInt(r["id"]),
            PharmacyId = ToInt(r["pharmacy_id"]),
            OrderId = r["order_id"] is DBNull or null ? null : ToInt(r["order_id"]),
            Amount = ToDecimalDef(r["amount"]),
            PaymentStatus = ToStrDef(r["payment_status"], "partial"),
            PaymentType = ToStrDef(r["payment_type"], "cash"),
            AmountPaid = ToDecimalDef(r["amount_paid"]),
            RemainingAmount = ToDecimalDef(r["remaining_amount"]),
            PaymentNotes = ToStrDef(r["payment_notes"]),
            Date = ParseDate(r["date"])
        }, errors);
    }

    private async Task<int> ImportReturnsAsync(SqliteConnection legacy, AppDbContext target, List<string> errors)
    {
        return await BulkInsertAsync(legacy, target, "returns", r => new Return
        {
            Id = ToInt(r["id"]),
            ReturnNumber = ToStrDef(r["return_number"], "RET-000"),
            PharmacyId = ToInt(r["pharmacy_id"]),
            OrderId = r["order_id"] is DBNull or null ? null : ToInt(r["order_id"]),
            TotalAmount = ToDecimalDef(r["total_amount"]),
            Status = ToStrDef(r["status"], "pending"),
            ReturnType = ToStr(r["return_type"]),
            Reason = ToStr(r["reason"]),
            Notes = ToStrDef(r["notes"]),
            StockAdjusted = ToBool(r["stock_adjusted"]),
            BalanceAdjusted = ToBool(r["balance_adjusted"]),
            BalanceBefore = ToDecimalDef(r["balance_before"]),
            BalanceAfter = ToDecimalDef(r["balance_after"]),
            CreatedAt = ParseDate(r["created_at"])
        }, errors);
    }

    private async Task<int> ImportReturnItemsAsync(SqliteConnection legacy, AppDbContext target, List<string> errors)
    {
        return await BulkInsertAsync(legacy, target, "return_items", r => new ReturnItem
        {
            Id = ToInt(r["id"]),
            ReturnId = ToInt(r["return_id"]),
            ProductId = ToInt(r["product_id"]),
            Quantity = ToInt(r["quantity"]),
            UnitPrice = ToDecimalDef(r["unit_price"]),
            TotalPrice = ToDecimalDef(r["total_price"])
        }, errors);
    }

    private async Task<int> ImportAuditLogsAsync(SqliteConnection legacy, AppDbContext target, List<string> errors)
    {
        return await BulkInsertAsync(legacy, target, "audit_logs", r => new AuditLog
        {
            Id = ToInt(r["id"]),
            Username = ToStrDef(r["username"], "system"),
            Action = ToStrDef(r["action"], "unknown"),
            Entity = ToStrDef(r["entity"], "unknown"),
            EntityId = ToStrDef(r["entity_id"], "0"),
            Details = ToStrDef(r["details"]),
            CreatedAt = ParseDate(r["created_at"])
        }, errors);
    }
}
