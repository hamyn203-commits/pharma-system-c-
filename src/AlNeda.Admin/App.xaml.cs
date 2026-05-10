using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.IO;
using System.Windows;
using AlNeda.Services;
using AlNeda.Admin.Services;
using AlNeda.Admin.Services.ApiClient;
using AlNeda.Core.Entities;
using System.Linq;
using UpdateService = AlNeda.Services.UpdateService;

namespace AlNeda.Admin;

public partial class App : Application
{
    public IServiceProvider Services { get; private set; } = null!;
    public static IConfiguration Configuration { get; private set; } = null!;
    public static string DbPath { get; private set; } = string.Empty;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Global Exception Handling
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        Configuration = configBuilder.Build();

        var settingsService = new SettingsService();
        var settings = settingsService.LoadSettings();

        var logDir = Path.GetDirectoryName(settings.LogsPath) ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(settings.LogsPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30)
            .CreateLogger();

        try
        {
            Log.Information("AlNeda Admin starting up");

            DbPath = settings.DbPath;
            Log.Information("Using database at: {DbPath}", DbPath);

            var services = new ServiceCollection();

            services.AddDbContextFactory<Data.AppDbContext>(options =>
            {
                options.UseSqlite($"Data Source={DbPath}");
                options.ConfigureWarnings(w => w.Ignore(
                    Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning,
                    Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.NavigationBaseIncludeIgnored));
            });

            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<SettingsService>();
            services.AddTransient<DashboardService>();
            services.AddTransient<UserService>();
            services.AddTransient<AuthService>();
            services.AddTransient<ProductService>();
            services.AddTransient<CategoryService>();
            services.AddTransient<AlNeda.Data.Repositories.CategoryRepository>();
            services.AddTransient<PharmacyService>();
            services.AddTransient<PurchaseService>();
            services.AddTransient<OrderService>();
            services.AddTransient<PaymentService>();
            services.AddTransient<ReturnService>();
            services.AddTransient<AuditService>();
            services.AddTransient<BackupService>();
            services.AddTransient<ReportService>();
            services.AddTransient<AlertService>();
            services.AddTransient<AlNeda.Admin.Services.PrintingService>();

            // API Client
            var apiBaseUrl = settings.ApiBaseUrl ?? "http://localhost:5000";
            services.AddSingleton<IAlNedaApiClient>(_ => new ApiClientService(apiBaseUrl));

            services.AddTransient<ViewModels.LoginViewModel>();
            services.AddSingleton<ViewModels.MainViewModel>();
            services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<ViewModels.MainViewModel>());
            services.AddTransient<ViewModels.ProductsViewModel>();
            services.AddTransient<ViewModels.CategoryViewModel>();
            services.AddTransient<ViewModels.PharmaciesViewModel>();
            services.AddTransient<ViewModels.SuppliersViewModel>();
            services.AddTransient<ViewModels.PurchasesViewModel>();
            services.AddTransient<ViewModels.OrdersViewModel>();
            services.AddTransient<ViewModels.PaymentsViewModel>();
            services.AddTransient<ViewModels.ReturnsViewModel>();
            services.AddTransient<ViewModels.AccountStatementViewModel>();
            services.AddTransient<ViewModels.ReportsViewModel>();
            services.AddTransient<ViewModels.AuditLogViewModel>();
            services.AddTransient<ViewModels.BackupViewModel>();
            services.AddTransient<ViewModels.SettingsViewModel>();
            services.AddTransient<ViewModels.DashboardViewModel>();

            services.AddTransient<LoginWindow>();
            services.AddTransient<MainWindow>();

            Services = services.BuildServiceProvider();

            // Initialize and Migrate Database
            try
            {
                using var scope = Services.CreateScope();
                var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<Data.AppDbContext>>();
                await using var dbContext = await contextFactory.CreateDbContextAsync();
                
                Log.Information("Checking for database migrations...");
                await dbContext.Database.MigrateAsync();
                
                // Seed default admin if no users exist
                if (!await dbContext.Users.AnyAsync())
                {
                    var adminPassword = Environment.GetEnvironmentVariable("ALNEDA_DEFAULT_ADMIN_PASSWORD");
                    if (string.IsNullOrWhiteSpace(adminPassword))
                    {
                        Log.Warning("No users found and ALNEDA_DEFAULT_ADMIN_PASSWORD is not set; skipping default admin seed.");
                    }
                    else
                    {
                        Log.Information("Seeding default admin user from environment variable...");
                        var (hash, salt) = AuthService.HashPassword(adminPassword);
                        dbContext.Users.Add(new User
                        {
                            Username = "admin",
                            Password = hash,
                            PasswordSalt = salt,
                            Role = "admin",
                            CreatedAt = DateTime.Now
                        });
                        await dbContext.SaveChangesAsync();
                    }
                }
                Log.Information("Database initialization successful");
            }
            catch (Exception dbEx)
            {
                Log.Error(dbEx, "Failed to initialize database");
                MessageBox.Show($"فشل تهيئة قاعدة البيانات: {dbEx.Message}", "خطأ قاعدة بيانات", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            CheckForUpdatesAsync();

            Services.GetRequiredService<AlertService>();

            // Show Login Window
            var loginWindow = Services.GetRequiredService<LoginWindow>();
            loginWindow.Show();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to start");
            MessageBox.Show($"فشل تشغيل التطبيق: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private async void CheckForUpdatesAsync()
    {
        try
        {
            var updateUrl = Configuration["Update:Url"];
            if (string.IsNullOrWhiteSpace(updateUrl)) return;

            var currentVersion = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0";
            var downloadPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updates");
            Directory.CreateDirectory(downloadPath);

            var updateService = new UpdateService(updateUrl, currentVersion, downloadPath);
            var updateInfo = await updateService.CheckForUpdateAsync();

            if (updateInfo != null)
            {
                var result = MessageBox.Show(
                    $"يتوفر تحديث جديد (الإصدار {updateInfo.Version})\n\n{updateInfo.ReleaseNotes}\n\nهل تريد التحديث الآن؟",
                    "تحديث متوفر",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    var updateFile = await updateService.DownloadUpdateAsync(updateInfo);
                    if (updateFile != null)
                    {
                        MessageBox.Show(
                            "سيتم تطبيق التحديث عند إعادة تشغيل التطبيق",
                            "التحديث جاهز",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to check for updates");
        }
    }

    private static string ResolveDbPath()
    {
        var envPath = Environment.GetEnvironmentVariable("ALNEDA_DB_PATH");
        if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
        {
            Log.Information("Using database path from environment variable");
            return envPath;
        }

        var configPath = Configuration["Database:Path"];
        if (!string.IsNullOrWhiteSpace(configPath))
        {
            if (File.Exists(configPath))
            {
                Log.Information("Using database path from appsettings.json");
                return configPath;
            }
            Log.Warning("Database path from config not found: {Path}", configPath);
        }

        var defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pharmacy.db");
        if (File.Exists(defaultPath))
        {
            Log.Information("Using default database in application directory");
            return defaultPath;
        }

        var solutionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "pharmacy.db");
        var resolvedSolutionPath = Path.GetFullPath(solutionPath);
        if (File.Exists(resolvedSolutionPath))
        {
            Log.Information("Using database in solution directory");
            return resolvedSolutionPath;
        }

        Log.Warning("No existing database found, will create new one at default path");
        return defaultPath;
    }

    public static void UpdateDbPath(string newPath)
    {
        if (string.IsNullOrWhiteSpace(newPath)) return;

        var config = Configuration as IConfigurationRoot;
        if (config != null)
        {
            config["Database:Path"] = newPath;
        }

        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        if (File.Exists(configPath))
        {
            var json = File.ReadAllText(configPath);
            if (json.Contains("\"Database\""))
            {
                json = System.Text.RegularExpressions.Regex.Replace(
                    json,
                    @"(""Database""\s*:\s*\{[^}]*)(""Path""\s*:\s*"")[^""]*("")",
                    $"$1$2{newPath.Replace("\\", "\\\\")}$3"
                );
                File.WriteAllText(configPath, json);
            }
        }

        DbPath = newPath;
        Log.Information("Database path updated to: {Path}", newPath);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "Unhandled UI Exception");
        MessageBox.Show($"حدث خطأ غير متوقع في الواجهة:\n{e.Exception.Message}", "خطأ غير متوقع", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        Log.Fatal(ex, "Unhandled Domain Exception (IsTerminating: {IsTerminating})", e.IsTerminating);
        if (ex != null)
        {
            MessageBox.Show($"حدث خطأ فادح في النظام:\n{ex.Message}", "خطأ فادح", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unobserved Task Exception");
        e.SetObserved();
    }
}
