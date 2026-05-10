using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using AlNeda.Admin.Services;
using AlNeda.Core.Models;
using AlNeda.Data.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlNeda.Admin.ViewModels;

public partial class CategoryViewModel : ObservableObject
{
    private readonly CategoryRepository _repository;
    private readonly IDialogService _dialog;
    private readonly DispatcherTimer _searchTimer;
    private System.Collections.Generic.List<CategoryDto> _allCategories = [];

    [ObservableProperty] private ObservableCollection<CategoryDto> _categories = [];
    [ObservableProperty] private CategoryDto? _selectedCategory;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private int _totalActive;
    [ObservableProperty] private int _totalProducts;
    [ObservableProperty] private string _topCategory = "-";

    // Edit state
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private CategoryDto _editCategory = new();
    [ObservableProperty] private string _dialogTitle = "إضافة تصنيف";

    public CategoryViewModel(CategoryRepository repository, IDialogService dialog)
    {
        _repository = repository;
        _dialog = dialog;

        _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _searchTimer.Tick += (s, e) => { _searchTimer.Stop(); ApplyFilters(); };
    }

    partial void OnSearchTextChanged(string value) 
    { 
        _searchTimer.Stop(); 
        _searchTimer.Start(); 
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            _allCategories = await _repository.GetAllCategoriesAsync();
            CalculateStats();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            _dialog.ShowError(ex.Message, "خطأ");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void CalculateStats()
    {
        TotalActive = _allCategories.Count(c => c.IsActive);
        TotalProducts = _allCategories.Sum(c => c.ProductCount);
        TopCategory = _allCategories.OrderByDescending(c => c.ProductCount).FirstOrDefault()?.Name ?? "-";
    }

    private void ApplyFilters()
    {
        var filtered = _allCategories.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.Trim();
            filtered = filtered.Where(c => c.Name.Contains(s, StringComparison.OrdinalIgnoreCase) || 
                                         (c.Description != null && c.Description.Contains(s, StringComparison.OrdinalIgnoreCase)));
        }

        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher != null)
        {
            dispatcher.Invoke(() =>
            {
                Categories = new ObservableCollection<CategoryDto>(filtered);
            });
        }
        else
        {
            Categories = new ObservableCollection<CategoryDto>(filtered);
        }
    }

    [RelayCommand]
    private void AddNew()
    {
        EditCategory = new CategoryDto { IsActive = true, Icon = "📦", ColorCode = "#FF10B981" };
        DialogTitle = "إضافة تصنيف جديد";
        IsEditing = true;
    }

    [RelayCommand]
    private void EditSelected(CategoryDto? cat)
    {
        var target = cat ?? SelectedCategory;
        if (target == null) return;

        EditCategory = new CategoryDto
        {
            Id = target.Id,
            Name = target.Name,
            Description = target.Description,
            Icon = target.Icon,
            ColorCode = target.ColorCode,
            IsActive = target.IsActive,
            DisplayOrder = target.DisplayOrder
        };
        DialogTitle = "تعديل التصنيف";
        IsEditing = true;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
    }

    [RelayCommand]
    private async Task SaveCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(EditCategory.Name))
        {
            _dialog.ShowError("الرجاء إدخال اسم التصنيف", "خطأ");
            return;
        }

        IsLoading = true;
        try
        {
            if (EditCategory.Id == 0)
            {
                await _repository.AddCategoryAsync(EditCategory);
                _dialog.ShowMessage("تمت الإضافة بنجاح", "نجاح");
            }
            else
            {
                await _repository.UpdateCategoryAsync(EditCategory);
                _dialog.ShowMessage("تم التعديل بنجاح", "نجاح");
            }

            IsEditing = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _dialog.ShowError(ex.Message, "خطأ");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(CategoryDto? cat)
    {
        var target = cat ?? SelectedCategory;
        if (target == null) return;

        string msg = target.ProductCount > 0 
            ? $"هذا التصنيف يحتوي على {target.ProductCount} منتج. لا يمكن حذفه." 
            : $"هل أنت متأكد من حذف التصنيف '{target.Name}'؟";

        if (target.ProductCount > 0)
        {
            _dialog.ShowError(msg, "رفض الحذف");
            return;
        }

        if (!_dialog.Confirm(msg)) return;

        IsLoading = true;
        try
        {
            await _repository.DeleteCategoryAsync(target.Id);
            _dialog.ShowMessage("تم حذف التصنيف بنجاح", "نجاح");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _dialog.ShowError(ex.Message, "خطأ");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
