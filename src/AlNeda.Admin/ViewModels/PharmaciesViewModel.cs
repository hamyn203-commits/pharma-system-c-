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
    [ObservableProperty] private bool _isNewPharmacy = true;
    [ObservableProperty] private Pharmacy _editPharmacy = new();
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _validationMessage = "";
    [ObservableProperty] private string _appUsername = "";
    [ObservableProperty] private string _appPassword = "";
    [ObservableProperty] private bool _showAppAccountEditor;
    [ObservableProperty] private bool _showPendingOnly;

    // Editor fields for manual pharmacy creation
    [ObservableProperty] private string _editOwnerName = "";
    [ObservableProperty] private string _editAppUsername = "";
    [ObservableProperty] private string _editAppPassword = "";

    public PharmaciesViewModel(IAlNedaApiClient apiClient, IDialogService dialog)
    {
        _apiClient = apiClient;
        _dialog = dialog;
    }

    public int TotalPharmacies => Pharmacies.Count;
    public int ActivePharmacies => Pharmacies.Count(x => string.Equals(x.AccountStatus, "active", StringComparison.OrdinalIgnoreCase));
    public int PendingPharmacies => Pharmacies.Count(x => string.Equals(x.AccountStatus, "pending", StringComparison.OrdinalIgnoreCase));
    public decimal TotalDebt => Pharmacies.Where(x => x.Balance > 0).Sum(x => x.Balance);
    public bool HasNoPharmacies => !IsLoading && Pharmacies.Count == 0;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var statusFilter = ShowPendingOnly ? "pending" : null;
            Pharmacies = new ObservableCollection<PharmacyDto>(
                await _apiClient.GetPharmaciesAsync(SearchText, statusFilter));
        }
        catch (ApiException ex)
        {
            _dialog.ShowError(ex.Message, "خطأ");
        }
        IsLoading = false;
    }

    partial void OnPharmaciesChanged(ObservableCollection<PharmacyDto> value)
    {
        OnPropertyChanged(nameof(TotalPharmacies));
        OnPropertyChanged(nameof(ActivePharmacies));
        OnPropertyChanged(nameof(PendingPharmacies));
        OnPropertyChanged(nameof(TotalDebt));
        OnPropertyChanged(nameof(HasNoPharmacies));
    }

    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(HasNoPharmacies));

    partial void OnShowPendingOnlyChanged(bool value)
    {
        _ = LoadAsync();
    }

    [RelayCommand]
    private void NewPharmacy()
    {
        IsNewPharmacy = true;
        EditPharmacy = new Pharmacy { AccountStatus = "active" };
        EditOwnerName = "";
        EditAppUsername = "";
        EditAppPassword = "";
        ValidationMessage = "";
        ShowEditor = true;
    }

    [RelayCommand]
    private void EditSelected()
    {
        if (SelectedPharmacy == null) return;
        IsNewPharmacy = false;
        EditPharmacy = new Pharmacy
        {
            Id = SelectedPharmacy.Id,
            Name = SelectedPharmacy.Name ?? string.Empty,
            Address = SelectedPharmacy.Address ?? string.Empty,
            Phone = SelectedPharmacy.Phone ?? string.Empty,
            OwnerName = SelectedPharmacy.OwnerName,
            Balance = SelectedPharmacy.Balance,
            AccountStatus = SelectedPharmacy.AccountStatus ?? "active"
        };
        EditOwnerName = SelectedPharmacy.OwnerName ?? "";
        EditAppUsername = "";
        EditAppPassword = "";
        ValidationMessage = "";
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
        if (IsNewPharmacy && !string.IsNullOrWhiteSpace(EditAppPassword) && EditAppPassword.Length < 8)
        {
            ValidationMessage = "كلمة المرور للتطبيق يجب ألا تقل عن 8 أحرف";
            return;
        }

        try
        {
            if (IsNewPharmacy)
            {
                var request = new CreatePharmacyRequest
                {
                    Name = EditPharmacy.Name.Trim(),
                    Address = EditPharmacy.Address,
                    Phone = EditPharmacy.Phone,
                    Balance = EditPharmacy.Balance,
                    AccountStatus = "active",
                    OwnerName = EditOwnerName?.Trim(),
                    Username = string.IsNullOrWhiteSpace(EditAppUsername) ? null : EditAppUsername.Trim(),
                    Password = string.IsNullOrWhiteSpace(EditAppPassword) ? null : EditAppPassword
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
                    AccountStatus = EditPharmacy.AccountStatus,
                    OwnerName = EditOwnerName?.Trim()
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

    [RelayCommand]
    private void OpenAppAccountEditor()
    {
        if (SelectedPharmacy == null) return;
        AppUsername = SelectedPharmacy.Phone?.Replace(" ", "") ?? $"pharmacy{SelectedPharmacy.Id}";
        AppPassword = "";
        ShowAppAccountEditor = true;
    }

    [RelayCommand]
    private async Task ApprovePharmacyAsync(PharmacyDto? pharmacy)
    {
        if (pharmacy == null) return;
        if (!_dialog.Confirm($"هل تريد اعتماد حساب الصيدلية '{pharmacy.Name}' وفتح تطبيق الصيدلي لها؟")) return;

        try
        {
            await _apiClient.SetPharmacyStatusAsync(pharmacy.Id, "active");
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            _dialog.ShowError(ex.Message, "خطأ");
        }
    }

    [RelayCommand]
    private async Task RevokePharmacyAsync(PharmacyDto? pharmacy)
    {
        if (pharmacy == null) return;
        if (!_dialog.Confirm($"هل تريد سحب صلاحية تطبيق الصيدلي من '{pharmacy.Name}'؟")) return;

        try
        {
            await _apiClient.SetPharmacyStatusAsync(pharmacy.Id, "blocked");
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            _dialog.ShowError(ex.Message, "خطأ");
        }
    }

    [RelayCommand]
    private async Task DeletePharmacyAsync(PharmacyDto? pharmacy)
    {
        if (pharmacy == null) return;
        if (!_dialog.Confirm($"هل تريد حذف الصيدلية '{pharmacy.Name}' نهائياً؟ لا يمكن التراجع عن هذا الإجراء.")) return;

        try
        {
            await _apiClient.DeletePharmacyAsync(pharmacy.Id);
            await LoadAsync();
            _dialog.ShowMessage("تم حذف الصيدلية بنجاح", "نجاح");
        }
        catch (ApiException ex)
        {
            _dialog.ShowError(ex.Message, "خطأ");
        }
    }

    [RelayCommand]
    private async Task CreateAppAccountAsync()
    {
        if (SelectedPharmacy == null) return;
        if (string.IsNullOrWhiteSpace(AppUsername) || string.IsNullOrWhiteSpace(AppPassword))
        {
            ValidationMessage = "اسم المستخدم وكلمة المرور مطلوبان";
            return;
        }

        try
        {
            await _apiClient.CreatePharmacyAppAccountAsync(SelectedPharmacy.Id, new CreatePharmacyAppAccountRequest
            {
                Username = AppUsername.Trim(),
                Password = AppPassword,
                IsActive = true
            });
            ShowAppAccountEditor = false;
            await LoadAsync();
            _dialog.ShowMessage("تم إنشاء حساب التطبيق للصيدلية", "نجاح");
        }
        catch (ApiException ex)
        {
            ValidationMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task ToggleAppAccountAsync()
    {
        if (SelectedPharmacy == null || !SelectedPharmacy.HasAppAccount) return;
        try
        {
            await _apiClient.SetPharmacyAppAccountStatusAsync(SelectedPharmacy.Id, !SelectedPharmacy.IsAppAccountActive);
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            _dialog.ShowError(ex.Message, "خطأ");
        }
    }

    [RelayCommand]
    private async Task ResetAppPasswordAsync()
    {
        if (SelectedPharmacy == null || !SelectedPharmacy.HasAppAccount) return;
        AppPassword = $"P@{SelectedPharmacy.Id}{DateTime.Now:HHmm}";
        try
        {
            await _apiClient.ResetPharmacyAppPasswordAsync(SelectedPharmacy.Id, AppPassword);
            _dialog.ShowMessage($"تم تعيين كلمة المرور الجديدة: {AppPassword}", "كلمة مرور التطبيق");
        }
        catch (ApiException ex)
        {
            _dialog.ShowError(ex.Message, "خطأ");
        }
    }

    [RelayCommand]
    private void CancelAppAccount() => ShowAppAccountEditor = false;
}
