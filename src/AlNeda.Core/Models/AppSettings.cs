
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
    public string CompanyName { get; set; } = "مخزن الندا";
    public string BranchName { get; set; } = "الفرع الرئيسي";
    public string DefaultCurrency { get; set; } = "EGP";
    public bool CompactSidebar { get; set; } = false;
    public bool ShowDashboardAnimations { get; set; } = true;

    // Database & API
    public string DbPath { get; set; } = "pharmacy.db";
    public string ApiBaseUrl { get; set; } = "http://localhost:5000";
    public string LogsPath { get; set; } = "logs/log.txt";
    public bool EnableAuditTrail { get; set; } = true;
    public bool ConfirmBeforeDelete { get; set; } = true;
    public bool AutoExportReports { get; set; } = false;
    public string DefaultReportRange { get; set; } = "آخر 30 يوم";

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
    public bool EnableSessionLock { get; set; } = true;
    public int SessionTimeoutMinutes { get; set; } = 15;
    public bool RequireStrongPasswords { get; set; } = true;
    public bool MaskSensitiveFinancials { get; set; } = false;
    public bool PrintCompanyHeader { get; set; } = true;
}
