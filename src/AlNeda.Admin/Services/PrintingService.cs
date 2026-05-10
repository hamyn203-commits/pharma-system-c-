using AlNeda.Core.Entities;
using AlNeda.Data;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace AlNeda.Admin.Services;

public class PrintingService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public PrintingService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Order?> GetOrderWithDetailsAsync(int orderId)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        return await db.Orders
            .Where(o => o.Id == orderId)
            .Select(o => new Order
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                PharmacyId = o.PharmacyId,
                Pharmacy = o.Pharmacy,
                TotalAmount = o.TotalAmount,
                Discount = o.Discount,
                DiscountType = o.DiscountType,
                FinalTotal = o.FinalTotal,
                AmountPaid = o.AmountPaid,
                BalanceBefore = o.BalanceBefore,
                BalanceAfter = o.BalanceAfter,
                Status = o.Status,
                DeliveryPerson = o.DeliveryPerson,
                Notes = o.Notes,
                LastStatusUpdate = o.LastStatusUpdate,
                ExpectedDeliveryNote = o.ExpectedDeliveryNote,
                PaymentStatus = o.PaymentStatus,
                PaymentType = o.PaymentType,
                RemainingAmount = o.RemainingAmount,
                PaymentNotes = o.PaymentNotes,
                CreatedAt = o.CreatedAt,
                Items = o.Items.Select(i => new OrderItem
                {
                    Id = i.Id,
                    OrderId = i.OrderId,
                    ProductId = i.ProductId,
                    Product = i.Product,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Pharmacy?> GetPharmacyWithOrdersAsync(int pharmacyId)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        return await db.Pharmacies
            .Where(p => p.Id == pharmacyId)
            .Select(p => new Pharmacy
            {
                Id = p.Id,
                Name = p.Name,
                Address = p.Address,
                Phone = p.Phone,
                Balance = p.Balance,
                CreatedAt = p.CreatedAt,
                AccountStatus = p.AccountStatus,
                ApprovedAt = p.ApprovedAt,
                BlockedAt = p.BlockedAt,
                LastLoginAt = p.LastLoginAt,
                DeviceId = p.DeviceId,
                Orders = p.Orders.Where(o => o.Status != "cancelled")
                    .Select(o => new Order
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        PharmacyId = o.PharmacyId,
                        Pharmacy = o.Pharmacy,
                        TotalAmount = o.TotalAmount,
                        Discount = o.Discount,
                        DiscountType = o.DiscountType,
                        FinalTotal = o.FinalTotal,
                        AmountPaid = o.AmountPaid,
                        BalanceBefore = o.BalanceBefore,
                        BalanceAfter = o.BalanceAfter,
                        Status = o.Status,
                        CreatedAt = o.CreatedAt,
                        Items = o.Items.Select(i => new OrderItem
                        {
                            Id = i.Id,
                            OrderId = i.OrderId,
                            ProductId = i.ProductId,
                            Product = i.Product,
                            Quantity = i.Quantity,
                            UnitPrice = i.UnitPrice,
                            TotalPrice = i.TotalPrice
                        }).ToList()
                    }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public FlowDocument CreateOrderInvoiceDocument(Order order)
    {
        var doc = new FlowDocument();
        doc.PagePadding = new Thickness(50);
        doc.FontFamily = new FontFamily("Segoe UI");

        var companyPara = new Paragraph(new Run("مخزن الندا للأدوية"))
        {
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(85, 214, 107)),
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        };
        doc.Blocks.Add(companyPara);

        doc.Blocks.Add(new Paragraph(new Run($"فاتورة ضريبية - #{order.OrderNumber}"))
        {
            TextAlignment = TextAlignment.Center,
            Foreground = Brushes.LightGray,
            Margin = new Thickness(0, 0, 0, 20)
        });

        var infoTable = new Table();
        infoTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        infoTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        var infoGroup = new TableRowGroup();

        var infoRow = new TableRow();
        infoRow.Cells.Add(new TableCell(new Paragraph(new Run($"الصيدلية: {order.Pharmacy?.Name ?? "غير محدد"}")) { Foreground = Brushes.LightGray }) { Padding = new Thickness(5) });
        infoRow.Cells.Add(new TableCell(new Paragraph(new Run($"التاريخ: {order.CreatedAt:yyyy-MM-dd HH:mm}")) { Foreground = Brushes.LightGray }) { Padding = new Thickness(5) });
        infoGroup.Rows.Add(infoRow);

        var statusRow = new TableRow();
        statusRow.Cells.Add(new TableCell(new Paragraph(new Run($"الحالة: {GetStatusName(order.Status)}")) { Foreground = Brushes.LightGray }) { Padding = new Thickness(5) });
        statusRow.Cells.Add(new TableCell(new Paragraph(new Run($"رقم الفاتورة: {order.Id}")) { Foreground = Brushes.LightGray }) { Padding = new Thickness(5) });
        infoGroup.Rows.Add(statusRow);

        infoTable.RowGroups.Add(infoGroup);
        doc.Blocks.Add(infoTable);

        doc.Blocks.Add(new Paragraph(new Run(" ")) { FontSize = 12 });

        if (order.Items != null && order.Items.Any())
        {
            var itemsTable = new Table();
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(50) });
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(2, GridUnitType.Star) });
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

            var headerRowGroup = new TableRowGroup();
            var headerRow = new TableRow { Background = new SolidColorBrush(Color.FromRgb(85, 214, 107)) };
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("#")) { Foreground = Brushes.Black, FontWeight = FontWeights.Bold }));
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("المنتج")) { Foreground = Brushes.Black, FontWeight = FontWeights.Bold }));
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("الكمية")) { Foreground = Brushes.Black, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center }));
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("السعر")) { Foreground = Brushes.Black, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Right }));
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("الإجمالي")) { Foreground = Brushes.Black, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Right }));
            headerRowGroup.Rows.Add(headerRow);
            itemsTable.RowGroups.Add(headerRowGroup);

            var itemsRowGroup = new TableRowGroup();
            int itemIndex = 1;
            foreach (var item in order.Items)
            {
                var row = new TableRow();
                row.Cells.Add(new TableCell(new Paragraph(new Run(itemIndex.ToString())) { Foreground = Brushes.LightGray }));
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.Product?.Name ?? "غير محدد")) { Foreground = Brushes.White }));
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.Quantity.ToString())) { Foreground = Brushes.White, TextAlignment = TextAlignment.Center }));
                row.Cells.Add(new TableCell(new Paragraph(new Run($"{item.UnitPrice:N2}")) { Foreground = Brushes.White, TextAlignment = TextAlignment.Right }));
                row.Cells.Add(new TableCell(new Paragraph(new Run($"{item.TotalPrice:N2}")) { Foreground = Brushes.White, TextAlignment = TextAlignment.Right }));
                itemsRowGroup.Rows.Add(row);
                itemIndex++;
            }
            itemsTable.RowGroups.Add(itemsRowGroup);
            doc.Blocks.Add(itemsTable);
        }

        doc.Blocks.Add(new Paragraph(new Run(" ")) { FontSize = 12 });

        var summaryTable = new Table();
        summaryTable.Columns.Add(new TableColumn { Width = new GridLength(3, GridUnitType.Star) });
        summaryTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        var summaryGroup = new TableRowGroup();

        var totalRow = new TableRow();
        totalRow.Cells.Add(new TableCell(new Paragraph(new Run("الإجمالي")) { Foreground = Brushes.LightGray, TextAlignment = TextAlignment.Left }));
        totalRow.Cells.Add(new TableCell(new Paragraph(new Run($"{order.TotalAmount:N2}")) { Foreground = Brushes.White, TextAlignment = TextAlignment.Right }));
        summaryGroup.Rows.Add(totalRow);

        var discountRow = new TableRow();
        discountRow.Cells.Add(new TableCell(new Paragraph(new Run($"الخصم ({(order.DiscountType == "percent" ? "%" : "ر.س")})")) { Foreground = Brushes.LightGray, TextAlignment = TextAlignment.Left }));
        discountRow.Cells.Add(new TableCell(new Paragraph(new Run($"-{order.Discount:N2}")) { Foreground = new SolidColorBrush(Color.FromRgb(219, 78, 78)), TextAlignment = TextAlignment.Right }));
        summaryGroup.Rows.Add(discountRow);

        var paidRow = new TableRow();
        paidRow.Cells.Add(new TableCell(new Paragraph(new Run("المبلغ المدفوع")) { Foreground = Brushes.LightGray, TextAlignment = TextAlignment.Left }));
        paidRow.Cells.Add(new TableCell(new Paragraph(new Run($"{order.AmountPaid:N2}")) { Foreground = new SolidColorBrush(Color.FromRgb(85, 214, 107)), TextAlignment = TextAlignment.Right }));
        summaryGroup.Rows.Add(paidRow);

        var remainingRow = new TableRow { Background = new SolidColorBrush(Color.FromRgb(85, 214, 107)) };
        remainingRow.Cells.Add(new TableCell(new Paragraph(new Run("المتبقي")) { Foreground = Brushes.Black, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Left }));
        remainingRow.Cells.Add(new TableCell(new Paragraph(new Run($"{order.RemainingAmount:N2}")) { Foreground = Brushes.Black, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Right }));
        summaryGroup.Rows.Add(remainingRow);

        summaryTable.RowGroups.Add(summaryGroup);
        doc.Blocks.Add(summaryTable);

        doc.Blocks.Add(new Paragraph(new Run($"شكراً لتعاملكم معنا - مخزن الندا"))
        {
            TextAlignment = TextAlignment.Center,
            Foreground = Brushes.Gray,
            FontSize = 10,
            Margin = new Thickness(0, 30, 0, 0)
        });

        return doc;
    }

    public FlowDocument CreateAccountStatementDocument(Pharmacy pharmacy, List<TransactionEntry> transactions)
    {
        var doc = new FlowDocument();
        doc.PagePadding = new Thickness(50);
        doc.FontFamily = new FontFamily("Segoe UI");

        doc.Blocks.Add(new Paragraph(new Run("كشف حساب"))
        {
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(85, 214, 107)),
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        });

        doc.Blocks.Add(new Paragraph(new Run($"صيدلية: {pharmacy.Name}"))
        {
            TextAlignment = TextAlignment.Center,
            Foreground = Brushes.LightGray,
            Margin = new Thickness(0, 0, 0, 20)
        });

        var infoTable = new Table();
        infoTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        infoTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        var infoGroup = new TableRowGroup();

        var row1 = new TableRow();
        row1.Cells.Add(new TableCell(new Paragraph(new Run($"الرصيد الحالي: {pharmacy.Balance:N2}")) { Foreground = Brushes.LightGray }) { Padding = new Thickness(5) });
        row1.Cells.Add(new TableCell(new Paragraph(new Run($"عدد المعاملات: {transactions.Count}")) { Foreground = Brushes.LightGray }) { Padding = new Thickness(5) });
        infoGroup.Rows.Add(row1);

        infoTable.RowGroups.Add(infoGroup);
        doc.Blocks.Add(infoTable);

        doc.Blocks.Add(new Paragraph(new Run(" ")) { FontSize = 12 });

        var transTable = new Table();
        transTable.Columns.Add(new TableColumn { Width = new GridLength(80) });
        transTable.Columns.Add(new TableColumn { Width = new GridLength(100) });
        transTable.Columns.Add(new TableColumn { Width = new GridLength(2, GridUnitType.Star) });
        transTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        transTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        transTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

        var headerRowGroup = new TableRowGroup();
        var headerRow = new TableRow { Background = new SolidColorBrush(Color.FromRgb(85, 214, 107)) };
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("التاريخ")) { Foreground = Brushes.Black, FontWeight = FontWeights.Bold }));
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("الرقم")) { Foreground = Brushes.Black, FontWeight = FontWeights.Bold }));
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("الوصف")) { Foreground = Brushes.Black, FontWeight = FontWeights.Bold }));
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("مدين")) { Foreground = Brushes.Black, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Right }));
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("دائن")) { Foreground = Brushes.Black, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Right }));
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("الرصيد")) { Foreground = Brushes.Black, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Right }));
        headerRowGroup.Rows.Add(headerRow);
        transTable.RowGroups.Add(headerRowGroup);

        var dataGroup = new TableRowGroup();
        foreach (var t in transactions)
        {
            var row = new TableRow();
            row.Cells.Add(new TableCell(new Paragraph(new Run(t.Date.ToString("yyyy-MM-dd"))) { Foreground = Brushes.White }));
            row.Cells.Add(new TableCell(new Paragraph(new Run(t.Reference)) { Foreground = Brushes.White }));
            row.Cells.Add(new TableCell(new Paragraph(new Run(t.Description)) { Foreground = Brushes.White }));
            row.Cells.Add(new TableCell(new Paragraph(new Run(t.Debit > 0 ? $"{t.Debit:N2}" : "-")) { Foreground = Brushes.White, TextAlignment = TextAlignment.Right }));
            row.Cells.Add(new TableCell(new Paragraph(new Run(t.Credit > 0 ? $"{t.Credit:N2}" : "-")) { Foreground = Brushes.White, TextAlignment = TextAlignment.Right }));
            row.Cells.Add(new TableCell(new Paragraph(new Run($"{t.RunningBalance:N2}")) { Foreground = t.RunningBalance >= 0 ? new SolidColorBrush(Color.FromRgb(85, 214, 107)) : new SolidColorBrush(Color.FromRgb(219, 78, 78)), TextAlignment = TextAlignment.Right }));
            dataGroup.Rows.Add(row);
        }
        transTable.RowGroups.Add(dataGroup);
        doc.Blocks.Add(transTable);

        doc.Blocks.Add(new Paragraph(new Run($"مخزن الندا للأدوية - كشف حساب رقم {pharmacy.Id}"))
        {
            TextAlignment = TextAlignment.Center,
            Foreground = Brushes.Gray,
            FontSize = 10,
            Margin = new Thickness(0, 30, 0, 0)
        });

        return doc;
    }

    public bool PrintDocument(FlowDocument document, string description = "تقرير")
    {
        var printDialog = new System.Windows.Controls.PrintDialog();
        if (printDialog.ShowDialog() == true)
        {
            document.PagePadding = new Thickness(50);
            var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
            printDialog.PrintDocument(paginator, description);
            return true;
        }
        return false;
    }

    public bool PrintOrder(Order order)
    {
        var doc = CreateOrderInvoiceDocument(order);
        return PrintDocument(doc, $"فاتورة #{order.OrderNumber}");
    }

    public bool PrintAccountStatement(Pharmacy pharmacy, List<TransactionEntry> transactions)
    {
        var doc = CreateAccountStatementDocument(pharmacy, transactions);
        return PrintDocument(doc, $"كشف حساب - {pharmacy.Name}");
    }

    public string ExportOrderToHtml(Order order)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html dir='rtl'>");
        sb.AppendLine("<head><meta charset='utf-8'><title>فاتورة</title>");
        sb.AppendLine("<style>body{font-family:Arial;background:#1a1a2e;color:#fff;padding:20px}");
        sb.AppendLine("h1{color:#55d66b;text-align:center}.header{text-align:center;margin-bottom:20px}");
        sb.AppendLine("table{width:100%;border-collapse:collapse;margin:20px 0}");
        sb.AppendLine("th,td{padding:10px;border:1px solid #333;text-align:right}");
        sb.AppendLine("th{background:#55d66b;color:#000}.total{font-size:18px;font-weight:bold}");
        sb.AppendLine(".remaining{background:#55d66b;color:#000;padding:10px;text-align:center;font-size:20px}");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine($"<h1>مخزن الندا للأدوية</h1>");
        sb.AppendLine($"<div class='header'><h2>فاتورة ضريبية - #{order.OrderNumber}</h2></div>");
        sb.AppendLine($"<p><strong>الصيدلية:</strong> {order.Pharmacy?.Name ?? "غير محدد"}</p>");
        sb.AppendLine($"<p><strong>التاريخ:</strong> {order.CreatedAt:yyyy-MM-dd HH:mm}</p>");
        sb.AppendLine($"<p><strong>الحالة:</strong> {GetStatusName(order.Status)}</p>");

        if (order.Items != null && order.Items.Any())
        {
            sb.AppendLine("<table><thead><tr><th>#</th><th>المنتج</th><th>الكمية</th><th>السعر</th><th>الإجمالي</th></tr></thead><tbody>");
            int i = 1;
            foreach (var item in order.Items)
            {
                sb.AppendLine($"<tr><td>{i}</td><td>{item.Product?.Name ?? "غير محدد"}</td><td>{item.Quantity}</td><td>{item.UnitPrice:N2}</td><td>{item.TotalPrice:N2}</td></tr>");
                i++;
            }
            sb.AppendLine("</tbody></table>");
        }

        sb.AppendLine($"<p class='total'>الإجمالي: {order.TotalAmount:N2} ر.س</p>");
        sb.AppendLine($"<p class='total'>الخصم: -{order.Discount:N2}</p>");
        sb.AppendLine($"<p class='total'>المدفوع: {order.AmountPaid:N2} ر.س</p>");
        sb.AppendLine($"<div class='remaining'>المتبقي: {order.RemainingAmount:N2} ر.س</div>");
        sb.AppendLine("<p style='text-align:center;color:#888;margin-top:30px'>شكراً لتعاملكم معنا</p>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    public string ExportStatementToHtml(Pharmacy pharmacy, List<TransactionEntry> transactions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html dir='rtl'>");
        sb.AppendLine("<head><meta charset='utf-8'><title>كشف حساب</title>");
        sb.AppendLine("<style>body{font-family:Arial;background:#1a1a2e;color:#fff;padding:20px}");
        sb.AppendLine("h1{color:#55d66b;text-align:center}table{width:100%;border-collapse:collapse;margin:20px 0}");
        sb.AppendLine("th,td{padding:10px;border:1px solid #333;text-align:right}th{background:#55d66b;color:#000}");
        sb.AppendLine(".balance{background:#333;padding:15px;text-align:center;font-size:20px;margin:20px 0}");
        sb.AppendLine(".debit{color:#ff6b6b}.credit{color:#55d66b}.positive{color:#55d66b}.negative{color:#ff6b6b}");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine($"<h1>كشف حساب</h1>");
        sb.AppendLine($"<div class='balance'><strong>صيدلية:</strong> {pharmacy.Name} | <strong>الرصيد:</strong> <span class='{(pharmacy.Balance >= 0 ? "positive" : "negative")}'>{pharmacy.Balance:N2} ر.س</span></div>");

        sb.AppendLine("<table><thead><tr><th>التاريخ</th><th>الرقم</th><th>الوصف</th><th>مدين</th><th>دائن</th><th>الرصيد</th></tr></thead><tbody>");
        foreach (var t in transactions)
        {
            sb.AppendLine($"<tr><td>{t.Date:yyyy-MM-dd}</td><td>{t.Reference}</td><td>{t.Description}</td>");
            sb.AppendLine($"<td class='debit'>{t.Debit:N2}</td><td class='credit'>{t.Credit:N2}</td>");
            sb.AppendLine($"<td class='{(t.RunningBalance >= 0 ? "positive" : "negative")}'>{t.RunningBalance:N2}</td></tr>");
        }
        sb.AppendLine("</tbody></table>");
        sb.AppendLine("<p style='text-align:center;color:#888;margin-top:30px'>مخزن الندا للأدوية</p>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private string GetStatusName(string status) => status switch
    {
        "pending" => "قيد الانتظار",
        "reviewed" => "تم المراجعة",
        "in_store" => "في المخزن",
        "with_driver" => "مع المندوب",
        "on_the_way" => "في الطريق",
        "delivered" => "تم التسليم",
        "postponed" => "مؤجل",
        "cancelled" => "ملغي",
        _ => status
    };
}

public class TransactionEntry
{
    public DateTime Date { get; set; }
    public string Reference { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
}