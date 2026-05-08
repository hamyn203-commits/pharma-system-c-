using System.Collections.ObjectModel;
using AlNeda.Admin.Services;
using AlNeda.Admin.Services.ApiClient;
using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlNeda.Admin.ViewModels;

public partial class PurchasesViewModel : ObservableObject
{
    private readonly IAlNedaApiClient _apiClient;
    private readonly IDialogService _dialog;

    [ObservableProperty] private ObservableCollection<PurchaseDto> _purchases = [];
    [ObservableProperty] private ObservableCollection<SupplierDto> _suppliers = [];
    [ObservableProperty] private ObservableCollection<ProductDto> _products = [];
    [ObservableProperty] private ObservableCollection<PurchaseItem> _newItems = [];
    [ObservableProperty] private PurchaseDto? _selectedPurchase;
    [ObservableProperty] private bool _showEditor;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private SupplierDto? _selectedSupplier;
    [ObservableProperty] private string _invoiceNumber = "";
    [ObservableProperty] private decimal _totalAmount;
    [ObservableProperty] private string _validationMessage = "";

    public PurchasesViewModel(IAlNedaApiClient apiClient, IDialogService dialog)
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
            Purchases = new ObservableCollection<PurchaseDto>(await _apiClient.GetPurchasesAsync());
            var supps = await _apiClient.GetSuppliersAsync();
            Suppliers = new ObservableCollection<SupplierDto>(supps);
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
    private void NewPurchase()
    {
        NewItems = [];
        TotalAmount = 0;
        InvoiceNumber = "";
        SelectedSupplier = null;
        ShowEditor = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidationMessage = "";
        if (SelectedSupplier == null) { ValidationMessage = "اختر المورد"; return; }
        if (string.IsNullOrWhiteSpace(InvoiceNumber)) { ValidationMessage = "رقم الفاتورة مطلوب"; return; }
        if (NewItems.Count == 0) { ValidationMessage = "أضف منتجات على الأقل"; return; }

        var request = new CreatePurchaseRequest
        {
            InvoiceNumber = InvoiceNumber.Trim(),
            SupplierId = SelectedSupplier.Id,
            TotalAmount = TotalAmount,
            Items = NewItems.Select(i => new CreatePurchaseItemRequest
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitCost
            }).ToList()
        };

        try
        {
            var result = await _apiClient.CreatePurchaseAsync(request);
            if (result != null)
                _dialog.ShowMessage($"تم حفظ فاتورة المشتريات #{result.InvoiceNumber} بنجاح", "نجاح");
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
        NewItems.Add(new PurchaseItem());
    }

    [RelayCommand]
    private void RemoveItem(PurchaseItem? item)
    {
        if (item != null) NewItems.Remove(item);
    }

    partial void OnNewItemsChanged(ObservableCollection<PurchaseItem> value)
    {
        TotalAmount = value.Sum(i => i.Quantity * i.UnitCost);
    }
}