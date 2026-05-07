using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.IO;
using System.Windows;
using AlNeda.Services;
using AlNeda.Admin.Services;

namespace AlNeda.Admin;

public partial class App : Application
{
    public IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "alneda-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30)
            .CreateLogger();

        try
        {
            Log.Information("AlNeda Admin starting up");

            var services = new ServiceCollection();

            var dbPath = @"D:\New folder (3)\pharmacy.db";

            if (!File.Exists(dbPath))
            {
                dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pharmacy.db");
            }

            Log.Information("Using database at: {DbPath}", dbPath);

            services.AddDbContextFactory<Data.AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            services.AddSingleton<IDialogService, DialogService>();
            services.AddTransient<AuthService>();
            services.AddTransient<ProductService>();
            services.AddTransient<CategoryService>();
            services.AddTransient<PharmacyService>();
            services.AddTransient<SupplierService>();
            services.AddTransient<PurchaseService>();
            services.AddTransient<OrderService>();
            services.AddTransient<PaymentService>();
            services.AddTransient<ReturnService>();
            services.AddTransient<AuditService>();
            services.AddTransient<BackupService>();
            services.AddTransient<ReportService>();

            services.AddTransient<ViewModels.LoginViewModel>();
            services.AddSingleton<ViewModels.MainViewModel>();
            services.AddTransient<ViewModels.ProductsViewModel>();
            services.AddTransient<ViewModels.CategoriesViewModel>();
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

    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
