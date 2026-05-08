using AlNeda.Admin.Services.ApiClient;
using AlNeda.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace AlNeda.Admin.ViewModels;

public partial class AddEditCategoryDialogViewModel : ObservableObject
{
    private readonly IAlNedaApiClient _apiClient;
    private readonly Window _window;

    [ObservableProperty] private CategoryDto _category;
    [ObservableProperty] private string _title;
    [ObservableProperty] private string _validationMessage = "";

    public AddEditCategoryDialogViewModel(IAlNedaApiClient apiClient, Window window, CategoryDto? category = null)
    {
        _apiClient = apiClient;
        _window = window;
        
        if (category == null)
        {
            _category = new CategoryDto { IsActive = true, Icon = "📦", ColorCode = "#FF10B981" };
            _title = "إضافة تصنيف جديد";
        }
        else
        {
            // Clone to avoid direct editing before save
            _category = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Icon = category.Icon,
                ColorCode = category.ColorCode,
                IsActive = category.IsActive,
                DisplayOrder = category.DisplayOrder
            };
            _title = "تعديل التصنيف";
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Category.Name))
        {
            ValidationMessage = "يرجى إدخال اسم التصنيف";
            return;
        }

        try
        {
            if (Category.Id == 0)
            {
                await _apiClient.CreateCategoryAsync(new CreateCategoryRequest
                {
                    Name = Category.Name.Trim(),
                    Description = Category.Description,
                    Icon = Category.Icon,
                    ColorCode = Category.ColorCode,
                    IsActive = Category.IsActive,
                    DisplayOrder = Category.DisplayOrder
                });
            }
            else
            {
                await _apiClient.UpdateCategoryAsync(new UpdateCategoryRequest
                {
                    Id = Category.Id,
                    Name = Category.Name.Trim(),
                    Description = Category.Description,
                    Icon = Category.Icon,
                    ColorCode = Category.ColorCode,
                    IsActive = Category.IsActive,
                    DisplayOrder = Category.DisplayOrder
                });
            }

            _window.DialogResult = true;
            _window.Close();
        }
        catch (ApiException ex)
        {
            ValidationMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ValidationMessage = $"خطأ: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _window.DialogResult = false;
        _window.Close();
    }
}
