using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
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
    private List<ProductDto> _allProducts = [];

    [ObservableProperty] private ObservableCollection<ProductDto> _products = [];
    [ObservableProperty] private ObservableCollection<CategoryDto> _categories = [];
    [ObservableProperty] private ProductDto? _selectedProduct;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _barcodeSearch = "";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _showEditor;
    [ObservableProperty] private Product _editProduct = new();
    [ObservableProperty] private string _validationMessage = "";
    [ObservableProperty] private int _totalProducts;
    [ObservableProperty] private int _activeProducts;
    [ObservableProperty] private int _lowStockProducts;
    [ObservableProperty] private decimal _inventoryValue;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private string _categoryFilter = "";

    public string[] CategoryFilters { get; set; } = [];

    public ProductsViewModel(IAlNedaApiClient apiClient, IDialogService dialog)
    {
        _apiClient = apiClient;
        _dialog = dialog;
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnCategoryFilterChanged(string value) => ApplyFilter();

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = "جاري تحميل المنتجات...";
        try
        {
            _allProducts = await _apiClient.GetProductsAsync();
            var cats = await _apiClient.GetCategoriesAsync();
            Categories = new ObservableCollection<CategoryDto>(cats);
            var names = cats.Select(c => c.Name).Where(n => !string.IsNullOrEmpty(n)).Cast<string>().Distinct().ToList();
            CategoryFilters = ["الكل", .. names];
            OnPropertyChanged(nameof(CategoryFilters));
            ApplyFilter();
        }
        catch (ApiException ex)
        {
            StatusMessage = "خطأ في تحميل المنتجات";
            _dialog.ShowError(ex.Message, "خطأ");
        }
        IsLoading = false;
    }

    [RelayCommand]
    private async Task SearchAsync() => await LoadAsync();

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    private void ApplyFilter()
    {
        if (_allProducts.Count == 0) return;
        var filtered = _allProducts.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var q = SearchText.Trim();
            filtered = filtered.Where(p =>
                (p.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) == true) ||
                (p.Barcode?.Contains(q, StringComparison.OrdinalIgnoreCase) == true) ||
                (p.Company?.Contains(q, StringComparison.OrdinalIgnoreCase) == true));
        }

        if (!string.IsNullOrEmpty(CategoryFilter) && CategoryFilter != "الكل")
        {
            filtered = filtered.Where(p =>
                p.CategoryName?.Equals(CategoryFilter, StringComparison.OrdinalIgnoreCase) == true ||
                p.Category?.Equals(CategoryFilter, StringComparison.OrdinalIgnoreCase) == true);
        }

        Products = new ObservableCollection<ProductDto>(filtered.ToList());
        UpdateStats();
        StatusMessage = $"تم عرض {Products.Count} من أصل {_allProducts.Count} منتج";
    }

    private void UpdateStats()
    {
        TotalProducts = Products.Count;
        ActiveProducts = Products.Count(p => p.IsActive == 1);
        LowStockProducts = Products.Count(p => p.Quantity <= 10);
        InventoryValue = Products.Sum(p => p.Quantity * p.UnitPrice);
    }

    [RelayCommand]
    private async Task SearchByBarcodeAsync()
    {
        if (string.IsNullOrWhiteSpace(BarcodeSearch)) return;
        StatusMessage = "جاري البحث بالباركود...";
        if (_allProducts.Count == 0)
        {
            IsLoading = true;
            _allProducts = await _apiClient.GetProductsAsync();
            IsLoading = false;
        }
        var found = _allProducts.Where(p => p.Barcode == BarcodeSearch.Trim()).ToList();
        Products = new ObservableCollection<ProductDto>(found);
        UpdateStats();
        StatusMessage = found.Count > 0 ? $"تم العثور على {found.Count} منتج" : "لم يتم العثور على منتج";

        if (found.Count == 1)
        {
            SelectedProduct = found[0];
            EditSelected(found[0]);
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
    private void EditSelected(ProductDto? product = null)
    {
        product ??= SelectedProduct;
        if (product == null) return;

        SelectedProduct = product;
        EditProduct = new Product
        {
            Id = product.Id,
            Name = product.Name ?? string.Empty,
            Barcode = product.Barcode,
            CategoryId = product.CategoryId,
            Category = product.CategoryName ?? product.Category ?? "عام",
            Company = product.Company ?? "غير محدد",
            Quantity = product.Quantity,
            UnitPrice = product.UnitPrice,
            ExpiryDate = product.ExpiryDate,
            ImagePath = product.ImagePath,
            ImageUrl = product.ImageUrl,
            IsActive = product.IsActive,
            Description = product.Description ?? string.Empty,
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
        catch (ApiException apiEx)
        {
            ValidationMessage = apiEx.ErrorResponse != null ? $"خطأ من الخادم: {apiEx.ErrorResponse.Message}" : $"خطأ في الاتصال (رمز: {apiEx.StatusCode})";
            Serilog.Log.Warning(apiEx, "API Save error");
        }
        catch (HttpRequestException)
        {
            ValidationMessage = "تعذر الاتصال بالخادم. تأكد من تشغيل الخدمة";
        }
        catch (Exception ex)
        {
            ValidationMessage = $"خطأ غير متوقع: {ex.Message}";
            Serilog.Log.Error(ex, "Failed to save product");
        }
        finally
        {
            IsLoading = false;
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

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "تصدير المنتجات CSV",
                Filter = "CSV Files (*.csv)|*.csv",
                FileName = $"Products_{DateTime.Today:yyyy-MM-dd}.csv"
            };
            if (dialog.ShowDialog() != true) return;
            using var writer = new StreamWriter(dialog.FileName, false, System.Text.Encoding.UTF8);
            await writer.WriteLineAsync("ID,اسم المنتج,الباركود,التصنيف,الشركة,الكمية,السعر,تاريخ الصلاحية");
            foreach (var p in Products)
            {
                await writer.WriteLineAsync($"{p.Id},\"{p.Name}\",\"{p.Barcode}\",\"{p.CategoryName ?? p.Category}\",\"{p.Company}\",{p.Quantity},{p.UnitPrice},\"{p.ExpiryDate}\"");
            }
            StatusMessage = $"تم تصدير {Products.Count} منتج إلى {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex) { StatusMessage = $"خطأ: {ex.Message}"; }
    }
}
