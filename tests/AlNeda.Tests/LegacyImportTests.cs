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
    public async Task ImportLegacyDb()
    {
        var legacyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "pharmacy.db");
        if (!File.Exists(legacyPath)) legacyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pharmacy.db");
        var targetDb = Path.GetTempFileName() + ".db";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={targetDb}")
            .Options;

        // First, ensure database has schema
        using (var setupCtx = new AppDbContext(options))
        {
            await setupCtx.Database.EnsureCreatedAsync();
        }

        var factory = new TestDbContextFactory(options);

        var importer = new LegacyDbImporter(legacyPath, factory);
        var result = await importer.ImportAsync();

        _output.WriteLine($"Imported: {result.RowsImported}, Skipped: {result.RowsSkipped}");
        foreach (var err in result.Errors)
            _output.WriteLine($"Error: {err}");

        // Verify - skip Users check since admin is seeded separately
        using (var verifyCtx = new AppDbContext(options))
        {
            Assert.True(verifyCtx.Categories.Any());
            Assert.True(verifyCtx.Products.Any());
            Assert.True(verifyCtx.Pharmacies.Any());
            Assert.True(verifyCtx.Orders.Any());
            Assert.True(verifyCtx.OrderItems.Any());
            Assert.True(verifyCtx.Payments.Any());
            Assert.True(verifyCtx.AuditLogs.Any());
        }

        Assert.True(result.RowsImported > 0);
        for (int i = 0; i < 5; i++)
        {
            try { if (File.Exists(targetDb)) File.Delete(targetDb); break; }
            catch { await Task.Delay(500); }
        }
    }

    private class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;
        public TestDbContextFactory(DbContextOptions<AppDbContext> options) => _options = options;
        public AppDbContext CreateDbContext() => new(_options);
    }
}
