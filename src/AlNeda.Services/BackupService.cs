using Microsoft.EntityFrameworkCore;

namespace AlNeda.Services;

public class BackupService
{
    private readonly IDbContextFactory<Data.AppDbContext> _contextFactory;
    private readonly string _dbPath;
    private readonly string _backupDir;

    public BackupService(IDbContextFactory<Data.AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
        _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pharmacy.db");
        _backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backups");
    }

    public async Task<string> BackupAsync(string? targetDir = null)
    {
        targetDir ??= _backupDir;
        Directory.CreateDirectory(targetDir);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var target = Path.Combine(targetDir, $"pharmacy_{timestamp}.db");

        await using var db = await _contextFactory.CreateDbContextAsync();
        await db.Database.ExecuteSqlAsync($"VACUUM INTO '{target}'");

        return target;
    }

    public string[] ListBackups(string? dir = null)
    {
        dir ??= _backupDir;
        if (!Directory.Exists(dir)) return [];
        return Directory.GetFiles(dir, "pharmacy_*.db").OrderByDescending(f => f).ToArray();
    }

    public async Task<bool> RestoreAsync(string backupPath)
    {
        try
        {
            if (!File.Exists(backupPath))
                return false;

            await using var db = await _contextFactory.CreateDbContextAsync();
            await db.Database.CloseConnectionAsync();

            File.Copy(backupPath, _dbPath, true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool DeleteBackup(string backupPath)
    {
        try
        {
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
}