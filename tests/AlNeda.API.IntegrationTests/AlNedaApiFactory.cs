using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using AlNeda.Data;
using AlNeda.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlNeda.API.IntegrationTests;

/// <summary>قاعدة SQLite مؤقتة + JWT للاختبارات؛ يضبط متغيرات البيئة لتطابق إصدار التوكن مع التحقق.</summary>
public sealed class AlNedaApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"alneda_api_test_{Guid.NewGuid():N}.db");
    private readonly object _seedLock = new();
    private bool _databaseSeeded;
    private bool _pharmacyUserSeeded;

    public AlNedaApiFactory()
    {
        Environment.SetEnvironmentVariable("ALNEDA_DEFAULT_ADMIN_PASSWORD", "IntegrationAdminPw!9");
        Environment.SetEnvironmentVariable("ALNEDA_JWT_KEY", "integration-tests-jwt-key-32chars-min!!");
        Environment.SetEnvironmentVariable("ALNEDA_JWT_ISSUER", "AlNeda.API.Test");
        Environment.SetEnvironmentVariable("ALNEDA_JWT_AUDIENCE", "AlNeda.Test");
        Environment.SetEnvironmentVariable("ALNEDA_JWT_EXPIRE_MINUTES", "60");
        Environment.SetEnvironmentVariable("Database__Provider", "Sqlite");
        Environment.SetEnvironmentVariable("Database__ConnectionString", $"Data Source={_dbPath}");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Sqlite",
                ["Database:ConnectionString"] = $"Data Source={_dbPath}",
                ["Jwt:Key"] = "integration-tests-jwt-key-32chars-min!!",
                ["Jwt:Issuer"] = "AlNeda.API.Test",
                ["Jwt:Audience"] = "AlNeda.Test",
                ["Jwt:ExpireMinutes"] = "60",
            });
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        client.BaseAddress = new Uri("http://localhost/");
        base.ConfigureClient(client);
    }

    /// <summary>يطبّق الهجرات ويضمن وجود مدير للاختبارات (عند الحاجة قبل إدراج بيانات يدوية).</summary>
    public void EnsureDatabaseSeeded()
    {
        lock (_seedLock)
        {
            if (_databaseSeeded) return;
            using var scope = Services.CreateScope();
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var db = dbFactory.CreateDbContext();
            db.Database.EnsureCreated();
            try { db.Database.ExecuteSqlRaw("ALTER TABLE MarketingOffers ADD COLUMN AdditionalImageUrls TEXT NOT NULL DEFAULT ''"); } catch { }

            if (!db.Users.Any(u => u.Username == "admin"))
            {
                var (hash, salt) = AuthService.HashPassword("IntegrationAdminPw!9");
                db.Users.Add(new User
                {
                    Username = "admin",
                    Password = hash,
                    PasswordSalt = salt,
                    Role = "admin",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });
                db.SaveChanges();
            }

            _databaseSeeded = true;
        }
    }

    /// <summary>يضيف مستخدم صيدلية بعد تشغيل المضيف (الهجرة والبذرة من Program).</summary>
    public void EnsurePharmacyTestUser()
    {
        EnsureDatabaseSeeded();
        lock (_seedLock)
        {
            if (_pharmacyUserSeeded) return;
            using var scope = Services.CreateScope();
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var db = dbFactory.CreateDbContext();
            if (db.Users.Any(u => u.Username == "pharmacytest")) { _pharmacyUserSeeded = true; return; }

            var pharmacy = new Pharmacy
            {
                Name = "صيدلية اختبار",
                Phone = "0500000000",
                Address = "",
                Balance = 0,
                AccountStatus = "active",
                CreatedAt = DateTime.Now
            };
            db.Pharmacies.Add(pharmacy);
            db.SaveChanges();

            var (hash, salt) = AuthService.HashPassword("PharmacyPw!9");
            db.Users.Add(new User
            {
                Username = "pharmacytest",
                Password = hash,
                PasswordSalt = salt,
                Role = "pharmacy",
                PharmacyId = pharmacy.Id,
                IsActive = true,
                CreatedAt = DateTime.Now
            });
            db.SaveChanges();
            _pharmacyUserSeeded = true;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { /* ignore */ }
        }
        base.Dispose(disposing);
    }
}

public static class ApiTestAuth
{
    private static readonly JsonSerializerOptions JsonRead = new() { PropertyNameCaseInsensitive = true };

    public static async Task<string?> LoginAsync(HttpClient client, string username, string password)
    {
        var body = JsonSerializer.Serialize(new { username, password }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        using var req = new HttpRequestMessage(HttpMethod.Post, new Uri("api/auth/login", UriKind.Relative))
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        var res = await client.SendAsync(req);
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync();
            throw new Exception($"Login failed with status {res.StatusCode} and body {err}");
        }
        var dto = await res.Content.ReadFromJsonAsync<LoginResponseDto>(JsonRead);
        return string.IsNullOrEmpty(dto?.Token) ? null : dto.Token;
    }

    public static Task<HttpResponseMessage> GetWithBearerAsync(HttpClient client, string relativeUrl, string token)
    {
        var path = relativeUrl.TrimStart('/');
        using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client.SendAsync(req);
    }

    public static void SetBearer(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
}
