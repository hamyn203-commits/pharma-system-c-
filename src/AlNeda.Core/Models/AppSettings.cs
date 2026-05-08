
namespace AlNeda.Core.Models;

public class AppSettings
{
    // General
    public string Language { get; set; } = "العربية";
    public string Theme { get; set; } = "داكن";
    public string FontFamily { get; set; } = "Segoe UI";
    public double FontSize { get; set; } = 14;
    public bool StartWithWindows { get; set; } = false;
    public bool CheckUpdatesOnStartup { get; set; } = true;

    // Database & API
    public string DbPath { get; set; } = "pharmacy.db";
    public string ApiBaseUrl { get; set; } = "http://localhost:5000";
    public string LogsPath { get; set; } = "logs/log.txt";

    // Notifications
    public bool ExpiryAlertEnabled { get; set; } = true;
    public int ExpiryDaysThreshold { get; set; } = 30;
    public bool LowStockAlertEnabled { get; set; } = true;
    public int LowStockThreshold { get; set; } = 10;
    public bool PendingPaymentsAlertEnabled { get; set; } = true;
    public decimal PendingPaymentsThreshold { get; set; } = 5000;
    public string AlertMethod { get; set; } = "كلاهما";
    public bool PlaySoundOnAlert { get; set; } = true;

    // Backup
    public bool AutoBackupEnabled { get; set; } = true;
    public string BackupInterval { get; set; } = "يومي";
    public int BackupRetentionCount { get; set; } = 7;
    public string BackupPath { get; set; } = "backups";
    public DateTime? LastBackupDate { get; set; }
}
