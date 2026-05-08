
using System.IO;
using System.Text.Json;
using AlNeda.Core.Models;

namespace AlNeda.Services;

public class SettingsService
{
    private readonly string _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
    private AppSettings? _currentSettings;

    public AppSettings Settings => _currentSettings ??= LoadSettings();

    public AppSettings LoadSettings()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                var defaults = GetDefaultSettings();
                SaveSettings(defaults);
                return defaults;
            }

            var json = File.ReadAllText(_settingsPath);
            _currentSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? GetDefaultSettings();
            return _currentSettings;
        }
        catch
        {
            return GetDefaultSettings();
        }
    }

    public void SaveSettings(AppSettings settings)
    {
        _currentSettings = settings;
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsPath, json);
    }

    public AppSettings GetDefaultSettings()
    {
        return new AppSettings();
    }

    public void ApplySettings()
    {
        var settings = Settings;
        // Logic to apply theme, font, etc. will be handled by App.xaml.cs or MainViewModel
    }
}
