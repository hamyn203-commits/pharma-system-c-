using System.Collections.ObjectModel;
using AlNeda.Admin.Services;
using AlNeda.Admin.Services.ApiClient;
using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlNeda.Admin.ViewModels;

public partial class ReturnsViewModel : ObservableObject
{
    private readonly IAlNedaApiClient _apiClient;
    private readonly IDialogService _dialog;

    [ObservableProperty] private ObservableCollection<ReturnDto> _returns = [];
    [ObservableProperty] private ObservableCollection<PharmacyDto> _pharmacies = [];
    [ObservableProperty] private ObservableCollection<ProductDto> _products = [];
    [ObservableProperty] private ObservableCollection<ReturnItem> _newItems = [];
    [ObservableProperty] private ReturnDto? _selectedReturn;
    [ObservableProperty] private bool _showEditor;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private PharmacyDto? _selectedPharmacy;
    [ObservableProperty] private decimal _totalAmount;
    [ObservableProperty] private string _returnType = "expired";
    [ObservableProperty] private string _reason = "";
    [ObservableProperty] private bool _adjustStock = true;
    [ObservableProperty] private bool _adjustBalance = true;
    [ObservableProperty] private string _validationMessage = "";

    public string[] ReturnTypes { get; } = ["expired", "damaged", "wrong_item", "extra_quantity", "customer_return", "other"];

    public ReturnsViewModel(IAlNedaApiClient apiClient, IDialogService dialog)
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
            Returns = new ObservableCollection<ReturnDto>(await _apiClient.GetReturnsAsync());
            var phs = await _apiClient.GetPharmaciesAsync();
            Pharmacies = new ObservableCollection<PharmacyDto>(phs);
            var prods = await _apiClient.GetProductsAsync();
            Products = new ObservableCollection<ProductDto>(prods);
        }
        catch (ApiException ex)
        {
            _dialog.ShowError(ex.Message, "خطأ");
        }
        IsLoading = false;
    }

    [RelayCommand]
    private void NewReturn()
    {
        NewItems = [];
        TotalAmount = 0;
        ReturnType = "expired";
        Reason = "";
        AdjustStock = true;
        AdjustBalance = true;
        SelectedPharmacy = null;
        ShowEditor = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidationMessage = "";
        if (SelectedPharmacy == null) { ValidationMessage = "اختر الصيدلية"; return; }
        if (NewItems.Count == 0) { ValidationMessage = "أضف منتجات على الأقل"; return; }

        var request = new CreateReturnRequest
        {
            PharmacyId = SelectedPharmacy.Id,
            TotalAmount = TotalAmount,
            ReturnType = ReturnType,
            Reason = Reason,
            StockAdjusted = AdjustStock,
            BalanceAdjusted = AdjustBalance,
            Items = NewItems.Select(i => new CreateReturnItemRequest
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        try
        {
            var result = await _apiClient.CreateReturnAsync(request);
            if (result != null)
                _dialog.ShowMessage($"تم إنشاء المرتجع #{result.ReturnNumber} بنجاح", "نجاح");
            ShowEditor = false;
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            ValidationMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void CancelEdit() => ShowEditor = false;

    [RelayCommand]
    private void AddItem()
    {
        NewItems.Add(new ReturnItem());
    }

    [RelayCommand]
    private void RemoveItem(ReturnItem? item)
    {
        if (item != null) NewItems.Remove(item);
    }

    partial void OnNewItemsChanged(ObservableCollection<ReturnItem> value)
    {
        TotalAmount = value.Sum(i => i.Quantity * i.UnitPrice);
    }
}