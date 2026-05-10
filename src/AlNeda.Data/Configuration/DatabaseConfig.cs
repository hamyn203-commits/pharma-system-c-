using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AlNeda.Data;
using AlNeda.Core.Entities;

namespace AlNeda.Data.Configuration;

public enum DatabaseProvider
{
    Sqlite,
    SqlServer,
    PostgreSql
}

public class DatabaseConfig
{
    public string Provider { get; set; } = "Sqlite";
    public string Path { get; set; } = "";
    public string ConnectionString { get; set; } = "";
    public SqliteConfig? Sqlite { get; set; }
    public SqlServerConfig? SqlServer { get; set; }
    public PostgreSqlConfig? PostgreSql { get; set; }
}

public class SqliteConfig
{
    public string DatabaseName { get; set; } = "pharmacy";
}

public class SqlServerConfig
{
    public string Server { get; set; } = "localhost";
    public string Database { get; set; } = "AlNedaDB";
    public string UserId { get; set; } = "";
    public string Password { get; set; } = "";
    public bool TrustServerCertificate { get; set; } = true;

    public string GetConnectionString()
    {
        if (!string.IsNullOrEmpty(UserId))
        {
            return $"Server={Server};Database={Database};User Id={UserId};Password={Password};TrustServerCertificate={TrustServerCertificate};";
        }
        return $"Server={Server};Database={Database};Integrated Security=true;TrustServerCertificate={TrustServerCertificate};";
    }
}

public class PostgreSqlConfig
{
    public string Host { get; set; } = "localhost";
    public string Database { get; set; } = "alneda";
    public string Username { get; set; } = "postgres";
    public string Password { get; set; } = "";
    public int Port { get; set; } = 5432;

    public string GetConnectionString()
    {
        return $"Host={Host};Database={Database};Username={Username};Password={Password};Port={Port}";
    }
}

public static class DbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder ConfigureDatabase(
        this DbContextOptionsBuilder builder,
        DatabaseConfig config,
        string basePath)
    {
        var provider = ParseProvider(config.Provider);

        var connectionString = ResolveConnectionString(config, basePath, provider);

        return provider switch
        {
            DatabaseProvider.Sqlite => builder.UseSqlite(connectionString),
            DatabaseProvider.SqlServer => builder.UseSqlServer(connectionString),
            DatabaseProvider.PostgreSql => builder.UseNpgsql(connectionString),
            _ => throw new NotSupportedException($"Database provider '{config.Provider}' is not supported")
        };
    }

    public static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        DatabaseConfig config,
        string basePath)
    {
        var connectionString = ResolveConnectionString(config, basePath, ParseProvider(config.Provider));

        services.AddDbContextFactory<AppDbContext>(options =>
        {
            switch (ParseProvider(config.Provider))
            {
                case DatabaseProvider.Sqlite:
                    options.UseSqlite(connectionString);
                    break;
                case DatabaseProvider.SqlServer:
                    options.UseSqlServer(connectionString);
                    break;
                case DatabaseProvider.PostgreSql:
                    options.UseNpgsql(connectionString);
                    break;
            }
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.NavigationBaseIncludeIgnored));
        });

        return services;
    }

    private static DatabaseProvider ParseProvider(string provider)
    {
        return provider.ToLowerInvariant() switch
        {
            "sqlite" or "sqlite3" => DatabaseProvider.Sqlite,
            "sqlserver" or "mssql" or "server" => DatabaseProvider.SqlServer,
            "postgresql" or "postgres" or "npgsql" => DatabaseProvider.PostgreSql,
            _ => throw new NotSupportedException($"Unknown database provider: {provider}")
        };
    }

    private static string ResolveConnectionString(DatabaseConfig config, string basePath, DatabaseProvider provider)
    {
        if (!string.IsNullOrWhiteSpace(config.ConnectionString))
        {
            return config.ConnectionString;
        }

        return provider switch
        {
            DatabaseProvider.Sqlite => ResolveSqliteConnection(config, basePath),
            DatabaseProvider.SqlServer => config.SqlServer?.GetConnectionString() ?? throw new InvalidOperationException("SQL Server configuration is required"),
            DatabaseProvider.PostgreSql => config.PostgreSql?.GetConnectionString() ?? throw new InvalidOperationException("PostgreSQL configuration is required"),
            _ => throw new NotSupportedException($"Provider {provider} not supported")
        };
    }

    private static string ResolveSqliteConnection(DatabaseConfig config, string basePath)
    {
        var dbPath = config.Path;

        if (string.IsNullOrEmpty(dbPath))
        {
            dbPath = config.Sqlite?.DatabaseName ?? "pharmacy";
        }

        if (!Path.IsPathRooted(dbPath))
        {
            dbPath = Path.Combine(basePath, $"{dbPath}.db");
        }

        if (!File.Exists(dbPath))
        {
            var solutionPath = Path.Combine(basePath, "..", "..", "..", $"{config.Sqlite?.DatabaseName ?? "pharmacy"}.db");
            var resolvedSolutionPath = Path.GetFullPath(solutionPath);
            if (File.Exists(resolvedSolutionPath))
            {
                dbPath = resolvedSolutionPath;
            }
        }

        return $"Data Source={dbPath}";
    }
}

public static class DbContextExtensions
{
    public static async Task EnsureDatabaseCreatedAsync(this AppDbContext context)
    {
        await context.Database.EnsureCreatedAsync();
    }

    public static async Task MigrateToDatabaseAsync(this AppDbContext context, DatabaseProvider provider)
    {
        if (provider == DatabaseProvider.Sqlite)
        {
            await context.Database.EnsureCreatedAsync();
        }
        else
        {
            await context.Database.MigrateAsync();
        }
    }
}

public static class DbProviderServices
{
    public static string[] SupportedProviders => ["Sqlite", "SqlServer", "PostgreSql"];

    public static string[] GetProviderDisplayNames() =>
    [
        "SQLite (افتراضي - للسطح المكتب)",
        "SQL Server (قاعدة بيانات محلية/خادم)",
        "PostgreSQL (قاعدة بيانات متقدمة)"
    ];

    public static string GetProviderDescription(string provider) => provider.ToLowerInvariant() switch
    {
        "sqlite" => "مناسب للتطبيقات المكتبية، لا يحتاج إعداد خادم",
        "sqlserver" => "مناسب للشركات المتوسطة والكبيرة",
        "postgresql" => "مناسب للتطبيقات عالية الأداء والسحابية",
        _ => "غير معروف"
    };
}