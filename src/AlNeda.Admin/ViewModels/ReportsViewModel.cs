using AlNeda.Admin.Services.ApiClient;
using AlNeda.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Xps;
using PrintDialog = System.Windows.Controls.PrintDialog;

namespace AlNeda.Admin.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    private readonly ReportService _reportService;
    private readonly IAlNedaApiClient _apiClient;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _reportContent = "اختر تقريراً من القائمة";
    [ObservableProperty] private DataTable? _reportData;
    [ObservableProperty] private bool _isGridView = true;
    [ObservableProperty] private string _currentReportName = "";
    [ObservableProperty] private string _statusMessage = "";

    public ReportsViewModel(ReportService reportService, IAlNedaApiClient apiClient)
    {
        _reportService = reportService;
        _apiClient = apiClient;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = false;
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void ToggleViewMode()
    {
        IsGridView = !IsGridView;
    }

    [RelayCommand]
    private async Task RunReportAsync(string? reportName)
    {
        if (string.IsNullOrEmpty(reportName)) return;
        IsLoading = true;
        CurrentReportName = reportName;
        ReportContent = "جاري تحميل التقرير...";

        try
        {
            ReportContent = reportName switch
            {
                "daily_sales" => await _reportService.GetDailySalesReportAsync(),
                "monthly_sales" => await _reportService.GetMonthlySalesReportAsync(),
                "top_products" => await _reportService.GetTopProductsReportAsync(),
                "top_pharmacies" => await _reportService.GetTopPharmaciesReportAsync(),
                "expiry" => await _reportService.GetExpiryReportAsync(),
                "debts" => await _reportService.GetDebtsReportAsync(),
                "stock" => await _reportService.GetStockReportAsync(),
                _ => $"تقرير غير معروف: {reportName}"
            };

            ReportData = reportName switch
            {
                "daily_sales" => await _reportService.GetDailySalesDataTableAsync(),
                "monthly_sales" => await _reportService.GetMonthlySalesDataTableAsync(),
                "top_products" => await _reportService.GetTopProductsDataTableAsync(),
                "top_pharmacies" => await _reportService.GetTopPharmaciesDataTableAsync(),
                "expiry" => await _reportService.GetExpiryDataTableAsync(),
                "debts" => await _reportService.GetDebtsDataTableAsync(),
                "stock" => await _reportService.GetStockDataTableAsync(),
                _ => null
            };

            StatusMessage = "تم تحميل التقرير بنجاح";
        }
        catch (Exception ex)
        {
            ReportContent = $"خطأ في تحميل التقرير: {ex.Message}";
            StatusMessage = $"خطأ: {ex.Message}";
        }

        IsLoading = false;
    }

    [RelayCommand]
    private async Task ExportToCsvAsync()
    {
        if (string.IsNullOrEmpty(CurrentReportName))
        {
            StatusMessage = "يرجى تشغيل التقرير أولاً";
            return;
        }

        var apiPath = CurrentReportName switch
        {
            "daily_sales" => "daily-sales",
            "monthly_sales" => "monthly-sales",
            "top_products" => "top-products",
            "top_pharmacies" => "top-pharmacies",
            "expiry" => "expiry",
            "debts" => "debts",
            "stock" => "stock",
            _ => null
        };

        if (apiPath == null)
        {
            StatusMessage = "نوع التقرير غير مدعوم للتصدير";
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "تصدير إلى CSV",
            Filter = "CSV Files (*.csv)|*.csv",
            FileName = $"{CurrentReportName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var data = await _apiClient.DownloadReportAsync(apiPath);
                if (data != null)
                {
                    await File.WriteAllBytesAsync(dialog.FileName, data);
                    StatusMessage = $"تم التصدير بنجاح: {Path.GetFileName(dialog.FileName)}";
                }
                else
                {
                    StatusMessage = "فشل التصدير من الخادم";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ في التصدير: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private async Task ExportToExcelAsync()
    {
        if (string.IsNullOrEmpty(CurrentReportName))
        {
            StatusMessage = "يرجى تشغيل التقرير أولاً";
            return;
        }

        var apiPath = CurrentReportName switch
        {
            "daily_sales" => "daily-sales/excel",
            "monthly_sales" => "monthly-sales/excel",
            "top_products" => "top-products/excel",
            "top_pharmacies" => "top-pharmacies/excel",
            "expiry" => "expiry/excel",
            "debts" => "debts/excel",
            "stock" => "stock/excel",
            _ => null
        };

        if (apiPath == null)
        {
            StatusMessage = "نوع التقرير غير مدعوم للتصدير";
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "تصدير إلى Excel",
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = $"{CurrentReportName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var data = await _apiClient.DownloadReportAsync(apiPath);
                if (data != null)
                {
                    await File.WriteAllBytesAsync(dialog.FileName, data);
                    StatusMessage = $"تم التصدير بنجاح: {Path.GetFileName(dialog.FileName)}";
                }
                else
                {
                    StatusMessage = "فشل التصدير من الخادم";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ في التصدير: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private void ExportToPdf()
    {
        if (ReportData == null || ReportData.Rows.Count == 0)
        {
            StatusMessage = "لا توجد بيانات للتصدير";
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "تصدير إلى PDF",
            Filter = "PDF Files (*.pdf)|*.pdf",
            FileName = $"{CurrentReportName}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var doc = CreateReportFlowDocument();
                var xpsPath = dialog.FileName.Replace(".pdf", ".xps");

                using var xpsDoc = new System.Windows.Xps.Packaging.XpsDocument(xpsPath, FileAccess.Write);
                var writer = System.Windows.Xps.Packaging.XpsDocument.CreateXpsDocumentWriter(xpsDoc);
                writer.Write(((IDocumentPaginatorSource)doc).DocumentPaginator);

                StatusMessage = $"تم التصدير بنجاح: {Path.GetFileName(xpsPath)}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ في التصدير: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private void Print()
    {
        if (ReportData == null || ReportData.Rows.Count == 0)
        {
            StatusMessage = "لا توجد بيانات للطباعة";
            return;
        }

        try
        {
            var doc = CreateReportFlowDocument();
            var printDialog = new PrintDialog();

            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, $"تقرير - {CurrentReportName}");
                StatusMessage = "تم إرسال التقرير إلى الطابعة";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ في الطباعة: {ex.Message}";
        }
    }

    private FlowDocument CreateReportFlowDocument()
    {
        var doc = new FlowDocument();
        doc.PagePadding = new Thickness(40);
        doc.FontFamily = new System.Windows.Media.FontFamily("Segoe UI");

        doc.Blocks.Add(new System.Windows.Documents.Paragraph(
            new System.Windows.Documents.Run($"تقرير: {GetReportTitle()}"))
        {
            FontSize = 20,
            FontWeight = System.Windows.FontWeights.Bold,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(85, 214, 107)),
            TextAlignment = System.Windows.TextAlignment.Center,
            Margin = new System.Windows.Thickness(0, 0, 0, 20)
        });

        doc.Blocks.Add(new System.Windows.Documents.Paragraph(
            new System.Windows.Documents.Run($"تاريخ الطباعة: {DateTime.Now:yyyy-MM-dd HH:mm}"))
        {
            FontSize = 10,
            Foreground = System.Windows.Media.Brushes.Gray,
            TextAlignment = System.Windows.TextAlignment.Center,
            Margin = new System.Windows.Thickness(0, 0, 0, 20)
        });

        if (ReportData != null)
        {
            var table = new System.Windows.Documents.Table();
            var totalColumns = ReportData.Columns.Count;
            for (int i = 0; i < totalColumns; i++)
            {
                table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            }

            var headerRowGroup = new System.Windows.Documents.TableRowGroup();
            var headerRow = new System.Windows.Documents.TableRow
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(85, 214, 107))
            };

            foreach (DataColumn col in ReportData.Columns)
            {
                headerRow.Cells.Add(new System.Windows.Documents.TableCell(
                    new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(col.ColumnName))
                    {
                        Foreground = System.Windows.Media.Brushes.Black,
                        FontWeight = System.Windows.FontWeights.Bold,
                        TextAlignment = System.Windows.TextAlignment.Right
                    }));
            }
            headerRowGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerRowGroup);

            var dataRowGroup = new System.Windows.Documents.TableRowGroup();
            foreach (DataRow row in ReportData.Rows)
            {
                var dataRow = new System.Windows.Documents.TableRow();
                foreach (var item in row.ItemArray)
                {
                    dataRow.Cells.Add(new System.Windows.Documents.TableCell(
                        new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(item?.ToString() ?? ""))
                        {
                            Foreground = System.Windows.Media.Brushes.White,
                            TextAlignment = System.Windows.TextAlignment.Right
                        }));
                }
                dataRowGroup.Rows.Add(dataRow);
            }
            table.RowGroups.Add(dataRowGroup);

            doc.Blocks.Add(table);
        }
        else
        {
            doc.Blocks.Add(new System.Windows.Documents.Paragraph(
                new System.Windows.Documents.Run(ReportContent))
            {
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 12,
                Foreground = System.Windows.Media.Brushes.LightGray
            });
        }

        doc.Blocks.Add(new System.Windows.Documents.Paragraph(
            new System.Windows.Documents.Run("مخزن الندا للأدوية"))
        {
            FontSize = 10,
            Foreground = System.Windows.Media.Brushes.Gray,
            TextAlignment = System.Windows.TextAlignment.Center,
            Margin = new System.Windows.Thickness(0, 30, 0, 0)
        });

        return doc;
    }

    private string GetReportTitle() => CurrentReportName switch
    {
        "daily_sales" => "المبيعات اليومية",
        "monthly_sales" => "المبيعات الشهرية",
        "top_products" => "المنتجات الأكثر مبيعاً",
        "top_pharmacies" => "الصيدليات الأعلى مبيعات",
        "expiry" => "تقرير انتهاء الصلاحية",
        "debts" => "الصيدليات المدينة",
        "stock" => "تقرير المخزون",
        _ => CurrentReportName
    };
}
