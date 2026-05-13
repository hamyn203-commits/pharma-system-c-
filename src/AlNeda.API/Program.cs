using System.Text;
using AlNeda.API.Authorization;
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
    // واجهة الإدارة (WPF): أدوار الموظفين فقط — لا يشمل pharmacy.
    options.AddPolicy(AuthPolicies.Staff, p => p.RequireRole("admin", "accountant", "rep"));
    options.AddPolicy(AuthPolicies.PharmacyApp, p => p.RequireRole("pharmacy"));
    options.AddPolicy(AuthPolicies.AdminOnly, p => p.RequireRole("admin"));
});
builder.Services.AddRateLimiter(options =>
{
    var isTesting = builder.Environment.IsEnvironment("Testing");
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = isTesting ? 100 : 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.AddPolicy("offer-events", context =>
    {
        var pharmacyId = context.User.FindFirst("pharmacyId")?.Value
            ?? context.User.FindFirst("PharmacyId")?.Value
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            pharmacyId,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = isTesting ? 1000 : 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
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
    await using var initDb = await dbFactory.CreateDbContextAsync();
    // الإنتاج/التكرار: EF Migrations. التطوير السريع بدون ملف DB: Migrate ينشئ الملف تلقائياً.
    // قواعد قديمة أنشئت بـ EnsureCreated فقط (بدون __EFMigrationsHistory): راجع تعليمات baseline في README أو نفّذ Migrate على نسخة احتياطية ثم أدرج سجل الهجرة يدوياً عند الحاجة.
    await initDb.Database.MigrateAsync();

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
    if (app.Environment.IsEnvironment("Testing"))
        throw;
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
if (!app.Environment.IsEnvironment("Testing"))
    app.UseRateLimiter();
app.UseStaticFiles();
app.UseAuthentication();
app.UseMiddleware<MobileAccountStatusMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();

/// <summary>يسمح بـ <c>WebApplicationFactory&lt;Program&gt;</c> في اختبارات التكامل.</summary>
public partial class Program { }
