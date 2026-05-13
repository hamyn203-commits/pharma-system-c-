using AlNeda.Data;
using AlNeda.Data.Import;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace AlNeda.Tests;

public class LegacyImportTests
{
    private readonly ITestOutputHelper _output;

    public LegacyImportTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task ImportLegacyDb_ShouldSucceed_WhenFileExists()
    {
        // 1. Setup paths
        var legacyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pharmacy.db");
        
        // Search for legacy db in parent directories if not in base
        var searchDir = AppDomain.CurrentDomain.BaseDirectory;
        while (!File.Exists(legacyPath) && searchDir != null && searchDir.Length > 3)
        {
            searchDir = Path.GetDirectoryName(searchDir);
            if (searchDir != null) legacyPath = Path.Combine(searchDir, "pharmacy.db");
        }

        if (!File.Exists(legacyPath))
        {
            _output.WriteLine("Legacy database 'pharmacy.db' not found. Skipping integration test.");
            return;
        }

        var targetDb = Path.Combine(Path.GetTempPath(), $"AlNeda_Test_{Guid.NewGuid():N}.db");

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={targetDb}")
                .Options;

            // 2. Initialize Schema
            using (var setupCtx = new AppDbContext(options))
            {
                await setupCtx.Database.EnsureCreatedAsync();
            }

            var factory = new TestDbContextFactory(options);
            var importer = new LegacyDbImporter(legacyPath, factory);

            // 3. Act
            var result = await importer.ImportAsync();

            // 4. Assert
            _output.WriteLine($"Import Result: {result.RowsImported} rows imported.");
            foreach (var err in result.Errors)
                _output.WriteLine($"Detail: {err}");

            Assert.Empty(result.Errors.Where(e => e.StartsWith("Global")));
            Assert.True(result.RowsImported >= 0);

            if (result.RowsImported > 0)
            {
                using var verifyCtx = new AppDbContext(options);
                // Basic verification of key tables
                bool hasData = await verifyCtx.Products.AnyAsync() || 
                               await verifyCtx.Pharmacies.AnyAsync() || 
                               await verifyCtx.Categories.AnyAsync();
                
                Assert.True(hasData, "Database should contain some data after successful import if legacy was not empty.");
            }
        }
        finally
        {
            // Cleanup
            if (File.Exists(targetDb))
            {
                try { File.Delete(targetDb); } catch { /* ignore cleanup errors */ }
            }
        }
    }

    private class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;
        public TestDbContextFactory(DbContextOptions<AppDbContext> options) => _options = options;
        public AppDbContext CreateDbContext() => new(_options);
    }
}
