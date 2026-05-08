using System.Collections.ObjectModel;
using AlNeda.Admin.Services.ApiClient;
using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlNeda.Admin.ViewModels;

public partial class PaymentsViewModel : ObservableObject
{
    private readonly IAlNedaApiClient _apiClient;
    private readonly Services.IDialogService _dialog;

    [ObservableProperty] private ObservableCollection<Payment> _payments = [];
    [ObservableProperty] private ObservableCollection<PharmacyDto> _pharmacies = [];
    [ObservableProperty] private Payment? _selectedPayment;
    [ObservableProperty] private bool _showEditor;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private int _selectedPharmacyId;
    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private string _paymentType = "cash";
    [ObservableProperty] private string _paymentNotes = "";
    [ObservableProperty] private string _validationMessage = "";

    public string[] PaymentTypes { get; } = ["cash", "partial", "full", "deferred", "collect_on_delivery"];

    public PaymentsViewModel(IAlNedaApiClient apiClient, Services.IDialogService dialog)
    {
        _apiClient = apiClient;
        _dialog = dialog;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            Payments = new ObservableCollection<Payment>([]); // placeholder — no GET payments endpoint yet
            var phs = await _apiClient.GetPharmaciesAsync();
            Pharmacies = new ObservableCollection<PharmacyDto>(phs);
        }
        catch (ApiException ex)
        {
            _dialog.ShowError(ex.Message, "خطأ");
        }
        IsLoading = false;
    }

    [RelayCommand]
    private void NewPayment()
    {
        Amount = 0;
        PaymentType = "cash";
        PaymentNotes = "";
        SelectedPharmacyId = 0;
        ShowEditor = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidationMessage = "";
        if (SelectedPharmacyId == 0) { ValidationMessage = "اختر الصيدلية"; return; }
        if (Amount <= 0) { ValidationMessage = "المبلغ يجب أن يكون أكبر من صفر"; return; }
        // Payment creation via API not yet implemented — kept as placeholder
        ShowEditor = false;
        await LoadAsync();
    }

    [RelayCommand]
    private void CancelEdit() => ShowEditor = false;
}