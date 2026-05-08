using AlNeda.Core.Entities;
using AlNeda.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;

namespace AlNeda.Admin.ViewModels;

public partial class CategoryProductsDialogViewModel : ObservableObject
{
    private readonly ProductService _productService;
    private readonly Window _window;
    private readonly int _categoryId;

    [ObservableProperty] private string _categoryName = string.Empty;
    [ObservableProperty] private string _categoryIcon = "📦";
    [ObservableProperty] private ObservableCollection<Product> _products = [];
    [ObservableProperty] private string _productCountText = "جاري التحميل...";

    public CategoryProductsDialogViewModel(ProductService productService, Window window, Category category)
    {
        _productService = productService;
        _window = window;
        _categoryId = category.Id;
        _categoryName = category.Name ?? "تصنيف بدون اسم";
        _categoryIcon = category.Icon ?? "📦";

        _ = LoadProductsAsync();
    }

    private async Task LoadProductsAsync()
    {
        var allProducts = await _productService.GetAllAsync();
        var filtered = allProducts.Where(p => p.CategoryId == _categoryId).ToList();
        
        Products = new ObservableCollection<Product>(filtered);
        ProductCountText = $"{filtered.Count} منتج متوفر";
    }

    [RelayCommand]
    private void Close() => _window.Close();
}
