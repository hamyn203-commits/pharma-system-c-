using System.Text;
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
    });

builder.Services.AddAuthorization();

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
    await initDb.Database.EnsureCreatedAsync();

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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
