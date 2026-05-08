using System.Collections.ObjectModel;
using AlNeda.Core.Entities;
using AlNeda.Services;
using AlNeda.Admin.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TransactionEntry = AlNeda.Admin.Services.TransactionEntry;
using PrintingService = AlNeda.Admin.Services.PrintingService;

namespace AlNeda.Admin.ViewModels;

public partial class AccountStatementViewModel : ObservableObject
{
    private readonly PharmacyService _pharmacyService;
    private readonly ReportService _reportService;
    private readonly AlNeda.Admin.Services.PrintingService _printingService;

    [ObservableProperty] private ObservableCollection<Pharmacy> _pharmacies = [];
    [ObservableProperty] private Pharmacy? _selectedPharmacy;
    [ObservableProperty] private string _ledgerText = "اختر صيدليةواضغط 'عرض الكشف'";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool _hasStatement;

    public AccountStatementViewModel(PharmacyService ps, ReportService rs, AlNeda.Admin.Services.PrintingService printingService)
    {
        _pharmacyService = ps;
        _reportService = rs;
        _printingService = printingService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        Pharmacies = new ObservableCollection<Pharmacy>(await _pharmacyService.GetAllAsync());
        IsLoading = false;
    }

    [RelayCommand]
    private async Task ShowStatementAsync()
    {
        if (SelectedPharmacy == null) return;
        IsLoading = true;
        LedgerText = "جاري تحميل الكشف...";
        LedgerText = await _reportService.GetAccountStatementAsync(SelectedPharmacy.Id);
        HasStatement = true;
        StatusMessage = $"تم تحميل كشف حساب: {SelectedPharmacy.Name}";
        IsLoading = false;
    }

    [RelayCommand]
    private async Task PrintStatementAsync()
    {
        if (SelectedPharmacy == null)
        {
            StatusMessage = "اختر صيدلية أولاً";
            return;
        }

        try
        {
            var pharmacy = await _pharmacyService.GetByIdAsync(SelectedPharmacy.Id);
            if (pharmacy == null)
            {
                StatusMessage = "خطأ في تحميل بيانات الصيدلية";
                return;
            }

            var transactions = BuildTransactionsList(pharmacy);
            if (_printingService.PrintAccountStatement(pharmacy, transactions))
            {
                StatusMessage = "تم إرسال الكشف إلى الطابعة";
            }
            else
            {
                StatusMessage = "تم إلغاء الطباعة";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportStatementHtmlAsync()
    {
        if (SelectedPharmacy == null)
        {
            StatusMessage = "اختر صيدلية أولاً";
            return;
        }

        try
        {
            var pharmacy = await _pharmacyService.GetByIdAsync(SelectedPharmacy.Id);
            if (pharmacy == null)
            {
                StatusMessage = "خطأ في تحميل بيانات الصيدلية";
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "تصدير كشف حساب HTML",
                Filter = "HTML Files (*.html)|*.html",
                FileName = $"Statement_{pharmacy.Name}_{DateTime.Now:yyyyMMdd}.html"
            };

            if (dialog.ShowDialog() == true)
            {
                var transactions = BuildTransactionsList(pharmacy);
                var html = _printingService.ExportStatementToHtml(pharmacy, transactions);
                System.IO.File.WriteAllText(dialog.FileName, html, System.Text.Encoding.UTF8);
                StatusMessage = $"تم التصدير: {System.IO.Path.GetFileName(dialog.FileName)}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ: {ex.Message}";
        }
    }

    private List<TransactionEntry> BuildTransactionsList(Pharmacy pharmacy)
    {
        var transactions = new List<TransactionEntry>();
        decimal balance = 0;

        if (pharmacy.Orders != null)
        {
            foreach (var order in pharmacy.Orders.Where(o => o.Status != "cancelled").OrderBy(o => o.CreatedAt))
            {
                balance += order.FinalTotal;
                transactions.Add(new TransactionEntry
                {
                    Date = order.CreatedAt,
                    Reference = $"#{order.OrderNumber}",
                    Description = "طلب جديد",
                    Debit = order.FinalTotal,
                    Credit = 0,
                    RunningBalance = balance
                });
            }
        }

        return transactions.OrderBy(t => t.Date).ToList();
    }
}
