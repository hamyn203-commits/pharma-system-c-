using System.Text;
using AlNeda.API.BackgroundServices;
using AlNeda.API.Middleware;
using AlNeda.Core.Entities;
using AlNeda.Data.Configuration;
using AlNeda.Data.UnitOfWork;
using AlNeda.Services;
using AlNeda.Services.Export;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = Environment.GetEnvironmentVariable("ALNEDA_JWT_KEY")
    ?? builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("JWT Key is not configured. Set ALNEDA_JWT_KEY env var or Jwt:Key in config.");
}

var jwtIssuer = Environment.GetEnvironmentVariable("ALNEDA_JWT_ISSUER")
    ?? builder.Configuration["Jwt:Issuer"]
    ?? "AlNeda.API";

var jwtAudience = Environment.GetEnvironmentVariable("ALNEDA_JWT_AUDIENCE")
    ?? builder.Configuration["Jwt:Audience"]
    ?? "AlNeda.Clients";

var jwtExpireMinutes = int.TryParse(
    Environment.GetEnvironmentVariable("ALNEDA_JWT_EXPIRE_MINUTES")
    ?? builder.Configuration["Jwt:ExpireMinutes"],
    out var expire) ? expire : 1440;

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowedOrigins is { Length: > 0 })
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
            return;
        }

        // Fail-closed by default: no cross-origin access unless explicitly configured.
        policy.WithOrigins("http://localhost:5000")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var dbConfig = builder.Configuration.GetSection("Database").Get<DatabaseConfig>() ?? new DatabaseConfig();
var basePath = AppDomain.CurrentDomain.BaseDirectory;
builder.Services.AddSingleton(dbConfig);
builder.Services.AddDatabaseServices(dbConfig, basePath);

builder.Services.AddSingleton<IUnitOfWorkFactory, UnitOfWorkFactory>();

builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IReportExporter, ReportExporterService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<AlertService>();

// ─── خدمات العروض المتقدمة ──────────────────────────────────────
builder.Services.AddSingleton<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<INotificationsService, NotificationsService>();
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<IBogoOfferService, BogoOfferService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();

// ─── خدمة النشر التلقائي للعروض ──────────────────────────────────
builder.Services.AddHostedService<OfferAutoPublishService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new AlNeda.Core.Models.ApiError
                {
                    Message = "يجب تسجيل الدخول",
                    Code = "AUTH_REQUIRED"
                });
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new AlNeda.Core.Models.ApiError
                {
                    Message = "غير مسموح بالوصول",
                    Code = "FORBIDDEN"
                });
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AlNeda.API.Authorization.AuthPolicies.Staff, policy => 
        policy.RequireRole("admin", "accountant", "rep"));
    options.AddPolicy(AlNeda.API.Authorization.AuthPolicies.PharmacyApp, policy => 
        policy.RequireRole("pharmacy"));
    options.AddPolicy(AlNeda.API.Authorization.AuthPolicies.AdminOnly, policy => 
        policy.RequireRole("admin"));
});
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AlNeda API",
        Version = "v1",
        Description = "نظام إدارة مخزن الأدوية - API للمزامنة والتقارير"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "أدخل رمز JWT للمصادقة"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

try
{
    var dbFactory = app.Services.GetRequiredService<IDbContextFactory<AlNeda.Data.AppDbContext>>();
    using var initDb = dbFactory.CreateDbContext();

    // Use EnsureCreated for all providers (avoids migration compatibility issues)
    await initDb.Database.EnsureCreatedAsync();

    // SQLite-specific schema additions
    var provider = (app.Services.GetRequiredService<DatabaseConfig>()).Provider?.ToLowerInvariant();
    if (provider == "sqlite" || provider == "sqlite3")
    {
        await EnsureMobileSchemaAsync(initDb);
    }

    if (!await initDb.Users.AnyAsync())
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        var adminPassword = Environment.GetEnvironmentVariable("ALNEDA_DEFAULT_ADMIN_PASSWORD");
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("No users exist and ALNEDA_DEFAULT_ADMIN_PASSWORD is not set. Default admin user was not created.");
            goto SeedDone;
        }

        logger.LogInformation("Seeding default admin user from environment variable...");
        var (hash, salt) = AuthService.HashPassword(adminPassword);
        initDb.Users.Add(new User
        {
            Username = "admin",
            Password = hash,
            PasswordSalt = salt,
            Role = "admin",
            IsActive = true,
            CreatedAt = DateTime.Now
        });
        await initDb.SaveChangesAsync();
    }
SeedDone:;
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(ex, "Database initialization skipped (will retry on first request)");
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "AlNeda API v1");
    options.RoutePrefix = "swagger";
    options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
});

app.UseCors();
app.UseRateLimiter();
app.UseStaticFiles();
app.UseAuthentication();
app.UseMiddleware<MobileAccountStatusMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();

static async Task EnsureMobileSchemaAsync(AlNeda.Data.AppDbContext db)
{
    var userColumns = await db.Database
        .SqlQueryRaw<string>("SELECT name AS Value FROM pragma_table_info('Users')")
        .ToListAsync();
    if (!userColumns.Contains("PharmacyId"))
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE Users ADD COLUMN PharmacyId INTEGER NULL");
    if (!userColumns.Contains("LastLoginAt"))
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE Users ADD COLUMN LastLoginAt TEXT NULL");

    var pharmacyColumns = await db.Database
        .SqlQueryRaw<string>("SELECT name AS Value FROM pragma_table_info('Pharmacies')")
        .ToListAsync();
    if (!pharmacyColumns.Contains("AccountStatus"))
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE Pharmacies ADD COLUMN AccountStatus TEXT NOT NULL DEFAULT 'active'");
    if (!pharmacyColumns.Contains("ApprovedAt"))
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE Pharmacies ADD COLUMN ApprovedAt TEXT NULL");
    if (!pharmacyColumns.Contains("BlockedAt"))
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE Pharmacies ADD COLUMN BlockedAt TEXT NULL");
    if (!pharmacyColumns.Contains("LastLoginAt"))
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE Pharmacies ADD COLUMN LastLoginAt TEXT NULL");
    if (!pharmacyColumns.Contains("DeviceId"))
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE Pharmacies ADD COLUMN DeviceId TEXT NULL");

    var orderColumns = await db.Database
        .SqlQueryRaw<string>("SELECT name AS Value FROM pragma_table_info('Orders')")
        .ToListAsync();
    if (!orderColumns.Contains("Source"))
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE Orders ADD COLUMN Source TEXT NOT NULL DEFAULT 'admin'");
    if (!orderColumns.Contains("ClientNotes"))
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE Orders ADD COLUMN ClientNotes TEXT NOT NULL DEFAULT ''");
    if (!orderColumns.Contains("MobileCreatedAt"))
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE Orders ADD COLUMN MobileCreatedAt TEXT NULL");
    if (!orderColumns.Contains("CancellationRequestedAt"))
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE Orders ADD COLUMN CancellationRequestedAt TEXT NULL");
}

public partial class Program { }
