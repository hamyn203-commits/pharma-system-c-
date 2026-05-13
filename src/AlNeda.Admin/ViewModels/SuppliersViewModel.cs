using System.Collections.ObjectModel;
using AlNeda.Admin.Services;
using AlNeda.Admin.Services.ApiClient;
using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlNeda.Admin.ViewModels;

public partial class SuppliersViewModel : ObservableObject
{
    private readonly IAlNedaApiClient _apiClient;
    private readonly IDialogService _dialog;

    [ObservableProperty] private ObservableCollection<SupplierDto> _suppliers = [];
    [ObservableProperty] private SupplierDto? _selectedSupplier;
    [ObservableProperty] private bool _showEditor;
    [ObservableProperty] private Supplier _editSupplier = new();
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _validationMessage = "";
    [ObservableProperty] private int _totalSuppliers;
    [ObservableProperty] private int _companiesCount;
    [ObservableProperty] private decimal _totalSupplierBalance;

    public SuppliersViewModel(IAlNedaApiClient apiClient, IDialogService dialog)
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
            Suppliers = new ObservableCollection<SupplierDto>(await _apiClient.GetSuppliersAsync(SearchText));
            UpdateStats();
        }
        catch (ApiException ex)
        {
            _dialog.ShowError(ex.Message, "خطأ");
        }
        IsLoading = false;
    }

    [RelayCommand]
    private void NewSupplier()
    {
        EditSupplier = new Supplier();
        ShowEditor = true;
    }

    private void UpdateStats()
    {
        TotalSuppliers = Suppliers.Count;
        CompaniesCount = Suppliers.Select(s => s.Company).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().Count();
        TotalSupplierBalance = Suppliers.Sum(s => s.Balance);
    }

    [RelayCommand]
    private void EditSelected()
    {
        if (SelectedSupplier == null) return;
        EditSupplier = new Supplier
        {
            Id = SelectedSupplier.Id,
            Name = SelectedSupplier.Name ?? string.Empty,
            Phone = SelectedSupplier.Phone ?? string.Empty,
            Address = SelectedSupplier.Address ?? string.Empty,
            Company = SelectedSupplier.Company ?? "غير محدد",
            Balance = SelectedSupplier.Balance,
            Notes = SelectedSupplier.Notes ?? string.Empty
        };
        ShowEditor = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidationMessage = "";
        if (string.IsNullOrWhiteSpace(EditSupplier.Name))
        {
            ValidationMessage = "اسم المورد مطلوب";
            return;
        }
        if (EditSupplier.Balance < 0)
        {
            ValidationMessage = "الرصيد لا يمكن أن يكون سالباً";
            return;
        }

        try
        {
            if (EditSupplier.Id == 0)
            {
                var request = new CreateSupplierRequest
                {
                    Name = EditSupplier.Name.Trim(),
                    Phone = EditSupplier.Phone,
                    Address = EditSupplier.Address,
                    Company = EditSupplier.Company,
                    Balance = EditSupplier.Balance,
                    Notes = EditSupplier.Notes
                };
                await _apiClient.CreateSupplierAsync(request);
                _dialog.ShowMessage("تم إضافة المورد بنجاح", "نجاح");
            }
            else
            {
                var request = new UpdateSupplierRequest
                {
                    Id = EditSupplier.Id,
                    Name = EditSupplier.Name.Trim(),
                    Phone = EditSupplier.Phone,
                    Address = EditSupplier.Address,
                    Company = EditSupplier.Company,
                    Balance = EditSupplier.Balance,
                    Notes = EditSupplier.Notes
                };
                await _apiClient.UpdateSupplierAsync(request);
                _dialog.ShowMessage("تم تحديث المورد بنجاح", "نجاح");
            }
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
    private async Task DeleteAsync(SupplierDto? s)
    {
        if (s == null) return;
        if (!_dialog.Confirm($"هل أنت متأكد من حذف المورد '{s.Name}'؟")) return;
        try
        {
            await _apiClient.DeleteSupplierAsync(s.Id);
            _dialog.ShowMessage("تم حذف المورد بنجاح", "نجاح");
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            _dialog.ShowError(ex.Message, "خطأ");
        }
    }
}
