using System.Collections.ObjectModel;
using AlNeda.Admin.Services;
using AlNeda.Admin.Services.ApiClient;
using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlNeda.Admin.ViewModels;

public partial class ProductsViewModel : ObservableObject
{
    private readonly IAlNedaApiClient _apiClient;
    private readonly IDialogService _dialog;

    [ObservableProperty] private ObservableCollection<ProductDto> _products = [];
    [ObservableProperty] private ObservableCollection<CategoryDto> _categories = [];
    [ObservableProperty] private ProductDto? _selectedProduct;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _barcodeSearch = "";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _showEditor;
    [ObservableProperty] private Product _editProduct = new();
    [ObservableProperty] private string _validationMessage = "";

    public ProductsViewModel(IAlNedaApiClient apiClient, IDialogService dialog)
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
            Products = new ObservableCollection<ProductDto>(await _apiClient.GetProductsAsync(SearchText));
            var cats = await _apiClient.GetCategoriesAsync();
            Categories = new ObservableCollection<CategoryDto>(cats);
        }
        catch (ApiException ex)
        {
            _dialog.ShowError(ex.Message, "خطأ");
        }
        IsLoading = false;
    }

    [RelayCommand]
    private async Task SearchAsync() => await LoadAsync();

    [RelayCommand]
    private async Task SearchByBarcodeAsync()
    {
        if (string.IsNullOrWhiteSpace(BarcodeSearch)) return;
        IsLoading = true;
        var allProducts = await _apiClient.GetProductsAsync();
        var found = allProducts.Where(p => p.Barcode == BarcodeSearch.Trim()).ToList();
        Products = new ObservableCollection<ProductDto>(found);
        IsLoading = false;

        if (found.Count == 1)
        {
            SelectedProduct = found[0];
            EditSelected();
        }
    }

    partial void OnBarcodeSearchChanged(string value)
    {
        if (value?.Length >= 8)
            SearchByBarcodeCommand.Execute(null);
    }

    [RelayCommand]
    private void NewProduct()
    {
        EditProduct = new Product { IsActive = 1 };
        ShowEditor = true;
    }

    [RelayCommand]
    private void EditSelected()
    {
        if (SelectedProduct == null) return;
        EditProduct = new Product
        {
            Id = SelectedProduct.Id,
            Name = SelectedProduct.Name,
            Barcode = SelectedProduct.Barcode,
            CategoryId = SelectedProduct.CategoryId,
            Category = SelectedProduct.CategoryName ?? SelectedProduct.Category,
            Company = SelectedProduct.Company,
            Quantity = SelectedProduct.Quantity,
            UnitPrice = SelectedProduct.UnitPrice,
            ExpiryDate = SelectedProduct.ExpiryDate,
            ImagePath = SelectedProduct.ImagePath,
            ImageUrl = SelectedProduct.ImageUrl,
            IsActive = SelectedProduct.IsActive,
            Description = SelectedProduct.Description,
        };
        ShowEditor = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            ValidationMessage = "";

            if (EditProduct == null || string.IsNullOrWhiteSpace(EditProduct.Name))
            {
                ValidationMessage = "اسم المنتج مطلوب";
                return;
            }
            if (EditProduct.Quantity < 0) { ValidationMessage = "الكمية لا يمكن أن تكون سالبة"; return; }
            if (EditProduct.UnitPrice < 0) { ValidationMessage = "السعر لا يمكن أن يكون سالباً"; return; }

            if (EditProduct.Id == 0)
            {
                var request = new CreateProductRequest
                {
                    Name = EditProduct.Name.Trim(),
                    Barcode = EditProduct.Barcode,
                    CategoryId = EditProduct.CategoryId,
                    Company = EditProduct.Company,
                    Quantity = EditProduct.Quantity,
                    UnitPrice = EditProduct.UnitPrice,
                    ExpiryDate = EditProduct.ExpiryDate,
                    ImagePath = EditProduct.ImagePath,
                    ImageUrl = EditProduct.ImageUrl,
                    Description = EditProduct.Description ?? "",
                    IsActive = EditProduct.IsActive,
                    ProductImagesJson = EditProduct.ProductImagesJson
                };
                await _apiClient.CreateProductAsync(request);
                _dialog.ShowMessage("تم إضافة المنتج بنجاح", "نجاح");
            }
            else
            {
                var request = new UpdateProductRequest
                {
                    Id = EditProduct.Id,
                    Name = EditProduct.Name.Trim(),
                    Barcode = EditProduct.Barcode,
                    CategoryId = EditProduct.CategoryId,
                    Company = EditProduct.Company,
                    Quantity = EditProduct.Quantity,
                    UnitPrice = EditProduct.UnitPrice,
                    ExpiryDate = EditProduct.ExpiryDate,
                    ImagePath = EditProduct.ImagePath,
                    ImageUrl = EditProduct.ImageUrl,
                    Description = EditProduct.Description ?? "",
                    IsActive = EditProduct.IsActive,
                    ProductImagesJson = EditProduct.ProductImagesJson
                };
                await _apiClient.UpdateProductAsync(request);
                _dialog.ShowMessage("تم تحديث المنتج بنجاح", "نجاح");
            }

            ShowEditor = false;
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            ValidationMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ValidationMessage = $"خطأ: {ex.Message}";
            Serilog.Log.Error(ex, "Failed to save product");
        }
    }

    [RelayCommand]
    private void CancelEdit() => ShowEditor = false;

    [RelayCommand]
    private async Task DeleteAsync(ProductDto? product)
    {
        if (product == null) return;
        if (!_dialog.Confirm($"هل أنت متأكد من حذف المنتج '{product.Name}'؟")) return;
        try
        {
            await _apiClient.DeleteProductAsync(product.Id);
            _dialog.ShowMessage("تم حذف المنتج بنجاح", "نجاح");
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            _dialog.ShowError(ex.Message, "خطأ");
        }
    }
}