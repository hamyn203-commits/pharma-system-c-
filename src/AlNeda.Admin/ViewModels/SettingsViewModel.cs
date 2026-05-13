using System.Collections.ObjectModel;
using System.IO;
using AlNeda.Admin.Services;
using AlNeda.Admin.Services.ApiClient;
using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using AlNeda.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace AlNeda.Admin.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly UserService _userService;
    private readonly IDialogService _dialog;
    private readonly IDbContextFactory<Data.AppDbContext> _dbFactory;
    private readonly IAlNedaApiClient _apiClient;

    [ObservableProperty] private AppSettings _currentSettings;
    [ObservableProperty] private string _status = "";
    [ObservableProperty] private bool _isLoading;

    // Database Info
    [ObservableProperty] private string _dbSize = "0 MB";
    [ObservableProperty] private int _tableCount = 0;

    // User Management
    [ObservableProperty] private ObservableCollection<User> _users = [];
    [ObservableProperty] private User? _selectedUser;
    [ObservableProperty] private bool _showUserEditor;
    [ObservableProperty] private User _editUser = new();
    [ObservableProperty] private string _userPassword = "";
    [ObservableProperty] private string _confirmPassword = "";
    [ObservableProperty] private bool _showPassword;

    public List<string> Languages { get; } = ["العربية", "English"];
    public List<string> Themes { get; } = ["داكن", "فاتح", "نظام"];
    public List<string> Fonts { get; } = ["Segoe UI", "Cairo", "Traditional Arabic"];
    public List<string> AlertMethods { get; } = ["نافذة منبثقة", "إشعار داخلي", "كلاهما"];
    public List<string> BackupIntervals { get; } = ["يومي", "أسبوعي", "شهري"];
    public List<string> Roles { get; } = ["admin", "accountant", "rep"];
    public List<string> Currencies { get; } = ["EGP", "USD", "SAR", "AED"];
    public List<string> ReportRanges { get; } = ["اليوم", "آخر 7 أيام", "آخر 30 يوم", "الشهر الحالي", "السنة الحالية"];

    public SettingsViewModel(SettingsService settingsService, UserService userService, IDialogService dialog, IDbContextFactory<Data.AppDbContext> dbFactory, IAlNedaApiClient apiClient)
    {
        _settingsService = settingsService;
        _userService = userService;
        _dialog = dialog;
        _dbFactory = dbFactory;
        _apiClient = apiClient;
        _currentSettings = _settingsService.LoadSettings();
        _ = LoadDataAsync(); // Fire and forget but with discard to suppress warning
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            Users = new ObservableCollection<User>(await _userService.GetAllUsersAsync());
            await LoadDbInfoAsync();
        }
        catch (Exception ex)
        {
            Status = $"خطأ في تحميل البيانات: {ex.Message}";
        }
        IsLoading = false;
    }

    private async Task LoadDbInfoAsync()
    {
        var fileInfo = new FileInfo(CurrentSettings.DbPath);
        if (fileInfo.Exists)
        {
            DbSize = $"{(fileInfo.Length / 1024.0 / 1024.0):F2} MB";
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        TableCount = 0; // Simplified
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            await db.Database.CanConnectAsync();
            _dialog.ShowMessage("تم الاتصال بقاعدة البيانات بنجاح", "نجاح");
        }
        catch (Exception ex)
        {
            _dialog.ShowError($"فشل الاتصال: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task VacuumDbAsync()
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            await db.Database.ExecuteSqlRawAsync("VACUUM");
            await LoadDbInfoAsync();
            _dialog.ShowMessage("تم إصلاح وتقليص حجم قاعدة البيانات بنجاح", "نجاح");
        }
        catch (Exception ex)
        {
            _dialog.ShowError($"فشل العملية: {ex.Message}");
        }
    }

    [RelayCommand]
    private void BrowseDatabase()
    {
        var path = _dialog.ShowOpenFileDialog("اختر قاعدة البيانات", "SQLite Database (*.db)|*.db", CurrentSettings.DbPath);
        if (path != null)
        {
            CurrentSettings.DbPath = path;
            OnPropertyChanged(nameof(CurrentSettings));
        }
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            _settingsService.SaveSettings(CurrentSettings);
            _apiClient.UpdateBaseUrl(CurrentSettings.ApiBaseUrl);
            Status = "تم حفظ الإعدادات بنجاح. قد يتطلب تغيير الثيم أو اللغة إعادة تشغيل البرنامج.";
        }
        catch (Exception ex)
        {
            Status = $"خطأ: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ResetDefaults()
    {
        if (_dialog.Confirm("هل أنت متأكد من استعادة الإعدادات الافتراضية؟"))
        {
            CurrentSettings = _settingsService.GetDefaultSettings();
            Save();
        }
    }

    [RelayCommand]
    private void RestartApp()
    {
        if (_dialog.Confirm("هل تريد إعادة تشغيل البرنامج الآن لتطبيق التغييرات؟"))
        {
            var processPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (processPath != null)
            {
                System.Diagnostics.Process.Start(processPath);
                System.Windows.Application.Current.Shutdown();
            }
        }
    }

    [RelayCommand]
    private async Task SaveSelectedUserCredentialsAsync()
    {
        if (SelectedUser == null)
        {
            Status = "اختر مستخدما من جدول المستخدمين أولا.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(UserPassword) && UserPassword != ConfirmPassword)
        {
            Status = "كلمة المرور وتأكيدها غير متطابقين.";
            return;
        }

        await _userService.UpdateUserAsync(SelectedUser, string.IsNullOrWhiteSpace(UserPassword) ? null : UserPassword);
        UserPassword = "";
        ConfirmPassword = "";
        await LoadDataAsync();
        Status = "تم تحديث بيانات المستخدم بنجاح.";
    }

    [RelayCommand]
    private async Task RunHealthCheckAsync()
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var canConnect = await db.Database.CanConnectAsync();
            var dbExists = File.Exists(CurrentSettings.DbPath);
            var backupPathReady = !string.IsNullOrWhiteSpace(CurrentSettings.BackupPath);
            Status = canConnect && dbExists && backupPathReady
                ? "فحص النظام مكتمل: قاعدة البيانات والنسخ الاحتياطي جاهزان."
                : "فحص النظام مكتمل مع وجود بنود تحتاج مراجعة.";
        }
        catch (Exception ex)
        {
            Status = $"فشل فحص النظام: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task NewUserAsync()
    {
        if (_dialog.ShowUserDialog())
        {
            await LoadDataAsync();
            Status = "تم إضافة المستخدم بنجاح";
        }
    }

    [RelayCommand]
    private async Task EditSelectedUserAsync()
    {
        if (SelectedUser == null) return;
        if (_dialog.ShowUserDialog(SelectedUser))
        {
            await LoadDataAsync();
            Status = "تم تحديث بيانات المستخدم بنجاح";
        }
    }


    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        try
        {
            IsLoading = true;
            Status = "جارٍ إنشاء النسخة الاحتياطية...";
            var backupDir = CurrentSettings.BackupPath;
            if (!Directory.Exists(backupDir)) Directory.CreateDirectory(backupDir);
            var fileName = $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            var destPath = Path.Combine(backupDir, fileName);
            await using var src = System.IO.File.OpenRead(CurrentSettings.DbPath);
            await using var dst = System.IO.File.Create(destPath);
            await src.CopyToAsync(dst);
            Status = $"✅ تم إنشاء النسخة الاحتياطية بنجاح: {fileName}";
            _dialog.ShowMessage($"تم حفظ النسخة في:\n{destPath}", "نسخ احتياطي");
        }
        catch (Exception ex)
        {
            Status = $"❌ فشل النسخ الاحتياطي: {ex.Message}";
            _dialog.ShowError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteUserAsync(User? user)
    {
        if (user == null) return;
        if (!_dialog.Confirm($"هل أنت متأكد من حذف المستخدم '{user.Username}'؟")) return;

        try
        {
            await _userService.DeleteUserAsync(user.Id);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _dialog.ShowError(ex.Message);
        }
    }
}
