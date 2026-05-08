using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace AlNeda.Services;

public class UpdateInfo
{
    public string Version { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string ReleaseNotes { get; set; } = "";
    public DateTime ReleaseDate { get; set; }
    public string Checksum { get; set; } = "";
}

public class UpdateService
{
    private readonly string _updateUrl;
    private readonly string _currentVersion;
    private readonly string _downloadPath;
    private readonly HttpClient _httpClient;
    private static readonly string _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "update.log");

    public UpdateService(string updateUrl, string currentVersion, string downloadPath)
    {
        _updateUrl = updateUrl;
        _currentVersion = currentVersion;
        _downloadPath = downloadPath;
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(10);
    }

    private static void Log(string message)
    {
        try
        {
            var logDir = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            File.AppendAllText(_logPath, logEntry);
        }
        catch { }
    }

    public async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(_updateUrl);
            var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (updateInfo == null) return null;

            if (IsNewerVersion(updateInfo.Version))
            {
                Log($"Update available: {updateInfo.Version}");
                return updateInfo;
            }

            Log($"No update available. Current: {_currentVersion}");
            return null;
        }
        catch (Exception ex)
        {
            Log($"Update check failed: {ex.Message}");
            return null;
        }
    }

    public async Task<string?> DownloadUpdateAsync(UpdateInfo updateInfo)
    {
        try
        {
            var fileName = $"AlNeda-{updateInfo.Version}.zip";
            var targetPath = Path.Combine(_downloadPath, fileName);

            if (File.Exists(targetPath))
            {
                Log($"Update file already exists: {targetPath}");
                return targetPath;
            }

            Log($"Downloading update from: {updateInfo.DownloadUrl}");

            var response = await _httpClient.GetAsync(updateInfo.DownloadUrl);
            response.EnsureSuccessStatusCode();

            var tempPath = Path.Combine(Path.GetTempPath(), fileName);
            await File.WriteAllBytesAsync(tempPath, await response.Content.ReadAsByteArrayAsync());

            if (!string.IsNullOrEmpty(updateInfo.Checksum))
            {
                var hash = ComputeHash(tempPath);
                if (hash != updateInfo.Checksum)
                {
                    throw new Exception("Checksum mismatch");
                }
            }

            File.Move(tempPath, targetPath, overwrite: true);
            Log($"Update downloaded to: {targetPath}");

            return targetPath;
        }
        catch (Exception ex)
        {
            Log($"Download failed: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> ApplyUpdateAsync(string updateFilePath)
    {
        try
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var backupDir = Path.Combine(appDir, "updates", "backup");
            var extractDir = Path.Combine(appDir, "updates", "temp");

            Directory.CreateDirectory(backupDir);
            Directory.CreateDirectory(extractDir);

            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var currentExe = Path.Combine(appDir, "AlNeda.exe");

            if (File.Exists(currentExe))
            {
                var backupPath = Path.Combine(backupDir, $"AlNeda-backup-{timestamp}.exe");
                File.Copy(currentExe, backupPath);
            }

            Log("Extracting update...");
            System.IO.Compression.ZipFile.ExtractToDirectory(updateFilePath, extractDir, overwriteFiles: true);

            var newExe = Path.Combine(extractDir, "AlNeda.exe");
            if (!File.Exists(newExe))
            {
                throw new Exception("Invalid update package");
            }

            File.Copy(newExe, currentExe, overwrite: true);

            foreach (var file in Directory.GetFiles(extractDir, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(extractDir, file);
                var targetFile = Path.Combine(appDir, relativePath);
                var targetDir = Path.GetDirectoryName(targetFile);

                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                File.Copy(file, targetFile, overwrite: true);
            }

            Directory.Delete(extractDir, recursive: true);

            Log("Update applied successfully");
            return true;
        }
        catch (Exception ex)
        {
            Log($"Apply failed: {ex.Message}");
            return false;
        }
    }

    private bool IsNewerVersion(string newVersion)
    {
        try
        {
            var current = Version.Parse(_currentVersion.TrimStart('v'));
            var newer = Version.Parse(newVersion.TrimStart('v'));
            return newer > current;
        }
        catch
        {
            return false;
        }
    }

    private string ComputeHash(string filePath)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLower();
    }
}