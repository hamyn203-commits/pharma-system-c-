using System.Collections.ObjectModel;
using System.Windows.Threading;
using AlNeda.Admin.Services;
using AlNeda.Admin.Services.ApiClient;
using AlNeda.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlNeda.Admin.ViewModels;

public partial class CategoriesViewModel : ObservableObject
{
    private readonly IAlNedaApiClient _apiClient;
    private readonly IDialogService _dialog;
    private readonly DispatcherTimer _searchTimer;
    private List<CategoryDto> _allCategories = [];

    [ObservableProperty] private ObservableCollection<CategoryDto> _categories = [];
    [ObservableProperty] private CategoryDto? _selectedCategory;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _filterCount = "الكل";
    [ObservableProperty] private string _sortBy = "الاسم";
    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private int _totalActive;
    [ObservableProperty] private int _totalProducts;
    [ObservableProperty] private string _topCategory = "-";

    public CategoriesViewModel(IAlNedaApiClient apiClient, IDialogService dialog)
    {
        _apiClient = apiClient;
        _dialog = dialog;

        _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _searchTimer.Tick += async (s, e) => { _searchTimer.Stop(); await ApplyFiltersAsync(); };
    }

    partial void OnSearchTextChanged(string value) 
    { 
        _searchTimer.Stop(); 
        _searchTimer.Start(); 
    }

    partial void OnFilterCountChanged(string value) => _ = ApplyFiltersAsync();
    partial void OnSortByChanged(string value) => _ = ApplyFiltersAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            _allCategories = await _apiClient.GetCategoriesAsync();
            CalculateStats();
            await ApplyFiltersAsync();
        }
        catch (ApiException ex)
        {
            _dialog.ShowError(ex.Message, "خطأ");
        }
        IsLoading = false;
    }

    private void CalculateStats()
    {
        TotalActive = _allCategories.Count(c => c.IsActive);
        TotalProducts = _allCategories.Sum(c => c.ProductCount);
        TopCategory = _allCategories.OrderByDescending(c => c.ProductCount).FirstOrDefault()?.Name ?? "-";
    }

    private async Task ApplyFiltersAsync()
    {
        var filtered = _allCategories.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.Trim();
            filtered = filtered.Where(c => c.Name.Contains(s, StringComparison.OrdinalIgnoreCase) || 
                                         (c.Description != null && c.Description.Contains(s, StringComparison.OrdinalIgnoreCase)));
        }

        filtered = FilterCount switch
        {
            "> 10" => filtered.Where(c => c.ProductCount > 10),
            "> 50" => filtered.Where(c => c.ProductCount > 50),
            "< 10" => filtered.Where(c => c.ProductCount < 10),
            _ => filtered
        };

        var result = SortBy switch
        {
            "العدد" => filtered.OrderByDescending(c => c.ProductCount).ToList(),
            "ID" => filtered.OrderBy(c => c.Id).ToList(),
            _ => filtered.OrderBy(c => c.Name).ToList()
        };

        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher != null)
        {
            await dispatcher.InvokeAsync(() =>
            {
                Categories = new ObservableCollection<CategoryDto>(result);
            });
        }
    }

    [RelayCommand]
    private async Task AddNewAsync()
    {
        if (_dialog.ShowCategoryDialog())
        {
            await LoadAsync();
        }
    }

    [RelayCommand]
    private async Task EditSelectedAsync(CategoryDto? cat)
    {
        var target = cat ?? SelectedCategory;
        if (target == null) return;

        if (_dialog.ShowCategoryDialog(new AlNeda.Core.Models.CategoryDto
        {
            Id = target.Id, Name = target.Name, Description = target.Description,
            Icon = target.Icon, ColorCode = target.ColorCode, IsActive = target.IsActive,
            DisplayOrder = target.DisplayOrder
        }))
        {
            await LoadAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(CategoryDto? cat)
    {
        if (cat == null) return;
        
        string msg = cat.ProductCount > 0 
            ? $"هذا التصنيف يحتوي على {cat.ProductCount} منتج. هل تريد حذفه نهائياً؟" 
            : $"هل أنت متأكد من حذف '{cat.Name}'؟";

        if (!_dialog.Confirm(msg)) return;

        try
        {
            await _apiClient.DeleteCategoryAsync(cat.Id);
            _dialog.ShowMessage("تم حذف التصنيف بنجاح", "نجاح");
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            _dialog.ShowError(ex.Message, "خطأ");
        }
    }

    [RelayCommand]
    private void ExportExcel() => _dialog.ShowMessage("جاري تحضير ملف Excel...", "تصدير");
}