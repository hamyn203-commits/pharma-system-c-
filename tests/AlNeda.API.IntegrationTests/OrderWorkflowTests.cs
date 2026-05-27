using System;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using AlNeda.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AlNeda.API.IntegrationTests;

public class OrderWorkflowTests : IClassFixture<AlNedaApiFactory>
{
    private const int InitialStock = 50;
    private const int OrderedQuantity = 10;

    private readonly AlNedaApiFactory _factory;

    public OrderWorkflowTests(AlNedaApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MobileOrder_WhenCreatedPending_DoesNotDeductStockUntilAdminReviews()
    {
        using var client = _factory.CreateClient();
        _factory.EnsurePharmacyTestUser();

        var product = await SeedActiveProductAsync();

        var pharmacyToken = await ApiTestAuth.LoginAsync(client, "pharmacytest", "PharmacyPw!9");
        Assert.NotNull(pharmacyToken);
        ApiTestAuth.SetBearer(client, pharmacyToken);

        var createOrderReq = new MobileCreateOrderRequest
        {
            Notes = "Integration test order created by pharmacy",
            Items =
            {
                new MobileCreateOrderItemRequest
                {
                    ProductId = product.Id,
                    Quantity = OrderedQuantity
                }
            }
        };

        var orderRes = await client.PostAsJsonAsync("/api/mobile/orders", createOrderReq);
        orderRes.EnsureSuccessStatusCode();

        var orderDto = await orderRes.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(orderDto);
        Assert.Equal("pending", orderDto.Status);
        Assert.Equal("mobile", orderDto.Source);

        var stockAfterCreation = await GetProductStockAsync(product.Id);
        Assert.Equal(InitialStock, stockAfterCreation);

        var adminToken = await ApiTestAuth.LoginAsync(client, "admin", "IntegrationAdminPw!9");
        Assert.NotNull(adminToken);
        ApiTestAuth.SetBearer(client, adminToken);

        var changeStatusReq = new { Status = "reviewed" };
        var reviewRes = await client.PutAsJsonAsync($"/api/orders/{orderDto.Id}/status", changeStatusReq);
        reviewRes.EnsureSuccessStatusCode();

        var reviewedOrder = await reviewRes.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(reviewedOrder);
        Assert.Equal("reviewed", reviewedOrder.Status);

        var stockAfterReview = await GetProductStockAsync(product.Id);
        Assert.Equal(InitialStock - OrderedQuantity, stockAfterReview);
    }

    private async Task<Product> SeedActiveProductAsync()
    {
        using var scope = _factory.Services.CreateScope();
        await using var db = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<AppDbContext>>()
            .CreateDbContextAsync();

        var category = await db.Categories.FirstOrDefaultAsync();
        if (category == null)
        {
            category = new Category
            {
                Name = "Test Category",
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            db.Categories.Add(category);
            await db.SaveChangesAsync();
        }

        var product = new Product
        {
            Name = $"Test Product {Guid.NewGuid():N}",
            Barcode = $"BC-{Guid.NewGuid():N}"[..12],
            CategoryId = category.Id,
            Quantity = InitialStock,
            UnitPrice = 10.0m,
            IsActive = 1,
            CreatedAt = DateTime.Now
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        return product;
    }

    private async Task<int> GetProductStockAsync(int productId)
    {
        using var scope = _factory.Services.CreateScope();
        await using var db = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<AppDbContext>>()
            .CreateDbContextAsync();

        var p = await db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == productId);
        return p?.Quantity ?? 0;
    }
}
