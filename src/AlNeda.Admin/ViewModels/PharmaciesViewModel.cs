using System.Collections.ObjectModel;
using AlNeda.Admin.Services;
using AlNeda.Admin.Services.ApiClient;
using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlNeda.Admin.ViewModels;

public partial class PharmaciesViewModel : ObservableObject
{
    private readonly IAlNedaApiClient _apiClient;
    private readonly IDialogService _dialog;

    [ObservableProperty] private ObservableCollection<PharmacyDto> _pharmacies = [];
    [ObservableProperty] private PharmacyDto? _selectedPharmacy;
    [ObservableProperty] private bool _showEditor;
    [ObservableProperty] private Pharmacy _editPharmacy = new();
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _validationMessage = "";

    public PharmaciesViewModel(IAlNedaApiClient apiClient, IDialogService dialog)
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
            Pharmacies = new ObservableCollection<PharmacyDto>(await _apiClient.GetPharmaciesAsync(SearchText));
        }
        catch (ApiException ex)
        {
            _dialog.ShowError(ex.Message, "خطأ");
        }
        IsLoading = false;
    }

    [RelayCommand]
    private void NewPharmacy()
    {
        EditPharmacy = new Pharmacy { AccountStatus = "pending" };
        ShowEditor = true;
    }

    [RelayCommand]
    private void EditSelected()
    {
        if (SelectedPharmacy == null) return;
        EditPharmacy = new Pharmacy
        {
            Id = SelectedPharmacy.Id,
            Name = SelectedPharmacy.Name,
            Address = SelectedPharmacy.Address,
            Phone = SelectedPharmacy.Phone,
            Balance = SelectedPharmacy.Balance,
            AccountStatus = SelectedPharmacy.AccountStatus
        };
        ShowEditor = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidationMessage = "";
        if (string.IsNullOrWhiteSpace(EditPharmacy.Name))
        {
            ValidationMessage = "اسم الصيدلية مطلوب";
            return;
        }
        if (EditPharmacy.Balance < 0)
        {
            ValidationMessage = "الرصيد لا يمكن أن يكون سالباً";
            return;
        }

        try
        {
            if (EditPharmacy.Id == 0)
            {
                var request = new CreatePharmacyRequest
                {
                    Name = EditPharmacy.Name.Trim(),
                    Address = EditPharmacy.Address,
                    Phone = EditPharmacy.Phone,
                    Balance = EditPharmacy.Balance,
                    AccountStatus = EditPharmacy.AccountStatus
                };
                await _apiClient.CreatePharmacyAsync(request);
                _dialog.ShowMessage("تم إضافة الصيدلية بنجاح", "نجاح");
            }
            else
            {
                var request = new UpdatePharmacyRequest
                {
                    Id = EditPharmacy.Id,
                    Name = EditPharmacy.Name.Trim(),
                    Address = EditPharmacy.Address,
                    Phone = EditPharmacy.Phone,
                    Balance = EditPharmacy.Balance,
                    AccountStatus = EditPharmacy.AccountStatus
                };
                await _apiClient.UpdatePharmacyAsync(request);
                _dialog.ShowMessage("تم تحديث الصيدلية بنجاح", "نجاح");
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
    private async Task SetStatusAsync(string? status)
    {
        if (SelectedPharmacy == null || status == null) return;
        if (status == "blocked" && !_dialog.Confirm($"هل تريد حظر الصيدلية '{SelectedPharmacy.Name}'؟")) return;
        if (status == "active" && !_dialog.Confirm($"هل تريد تفعيل الصيدلية '{SelectedPharmacy.Name}'؟")) return;
        try
        {
            await _apiClient.SetPharmacyStatusAsync(SelectedPharmacy.Id, status);
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            _dialog.ShowError(ex.Message, "خطأ");
        }
    }
}