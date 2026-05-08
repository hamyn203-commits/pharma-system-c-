using AlNeda.Admin.Services;
using AlNeda.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlNeda.Admin.ViewModels;

public partial class BackupViewModel : ObservableObject
{
    private readonly BackupService _backupService;
    private readonly IDialogService _dialog;

    [ObservableProperty] private string _status = "جاهز";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string[] _backups = [];

    public BackupViewModel(BackupService backupService, IDialogService dialog)
    {
        _backupService = backupService;
        _dialog = dialog;
    }

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        if (!_dialog.Confirm("هل تريد إنشاء نسخة احتياطية الآن؟")) return;
        IsLoading = true;
        Status = "جاري إنشاء النسخة الاحتياطية...";
        try
        {
            var path = await _backupService.BackupAsync();
            Status = "تم إنشاء النسخة الاحتياطية بنجاح";
            Backups = _backupService.ListBackups();
        }
        catch (Exception ex)
        {
            Status = $"خطأ: {ex.Message}";
        }
        IsLoading = false;
    }

    [RelayCommand]
    private async Task RestoreBackupAsync(string? backupPath)
    {
        if (string.IsNullOrEmpty(backupPath)) return;
        if (!_dialog.Confirm("تحذير: سيتم استبدال قاعدة البيانات الحالية بالنسخة المحددة!\nهل تريد المتابعة؟")) return;

        IsLoading = true;
        Status = "جاري استرجاع النسخة الاحتياطية...";
        try
        {
            var success = await _backupService.RestoreAsync(backupPath);
            Status = success ? "تم استرجاع النسخة الاحتياطية بنجاح" : "فشل في استرجاع النسخة";
        }
        catch (Exception ex)
        {
            Status = $"خطأ: {ex.Message}";
        }
        IsLoading = false;
    }

    [RelayCommand]
    private void DeleteBackup(string? backupPath)
    {
        if (string.IsNullOrEmpty(backupPath)) return;
        if (!_dialog.Confirm("هل تريد حذف هذه النسخة الاحتياطية؟")) return;

        var success = _backupService.DeleteBackup(backupPath);
        Backups = _backupService.ListBackups();
        Status = success ? "تم حذف النسخة الاحتياطية" : "فشل في حذف النسخة";
    }

    [RelayCommand]
    private Task LoadAsync()
    {
        Backups = _backupService.ListBackups();
        Status = $"يوجد {Backups.Length} نسخة احتياطية";
        return Task.CompletedTask;
    }
}