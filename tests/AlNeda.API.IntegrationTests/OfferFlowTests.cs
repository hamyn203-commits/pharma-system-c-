using System.Net;
using System.Net.Http.Json;
using AlNeda.Core.Entities;
using AlNeda.Core.Models;
using AlNeda.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AlNeda.API.IntegrationTests;

public class OfferFlowTests : IClassFixture<AlNedaApiFactory>
{
    private readonly AlNedaApiFactory _factory;

    public OfferFlowTests(AlNedaApiFactory factory) => _factory = factory;

    [Fact]
    public async Task DuplicateImpression_SameDaySameDevice_IsNotCountedTwice()
    {
        using var client = _factory.CreateClient();
        var offer = SeedActiveOffer();
        _factory.EnsurePharmacyTestUser();
        var token = await ApiTestAuth.LoginAsync(client, "pharmacytest", "PharmacyPw!9");
        Assert.NotNull(token);
        ApiTestAuth.SetBearer(client, token);

        var first = await client.PostAsJsonAsync($"/api/mobile/offers/{offer.Id}/events", new OfferEventRequest { Type = "impression", DeviceId = "device-1" });
        var second = await client.PostAsJsonAsync($"/api/mobile/offers/{offer.Id}/events", new OfferEventRequest { Type = "impression", DeviceId = "device-1" });

        first.EnsureSuccessStatusCode();
        second.EnsureSuccessStatusCode();
        var firstBody = await first.Content.ReadFromJsonAsync<OfferEventResponse>();
        var secondBody = await second.Content.ReadFromJsonAsync<OfferEventResponse>();
        Assert.True(firstBody?.Counted);
        Assert.False(secondBody?.Counted);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
        var counted = await db.OfferEvents.CountAsync(e => e.MarketingOfferId == offer.Id && e.IsUniqueDailyImpression);
        Assert.Equal(1, counted);
    }

    [Fact]
    public async Task PharmacyJwt_IsForbidden_On_AdminOfferAnalytics()
    {
        using var client = _factory.CreateClient();
        _factory.EnsurePharmacyTestUser();
        var token = await ApiTestAuth.LoginAsync(client, "pharmacytest", "PharmacyPw!9");
        Assert.NotNull(token);
        ApiTestAuth.SetBearer(client, token);

        var res = await ApiTestAuth.GetWithBearerAsync(client, "api/admin/offers/analytics", token);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task SourceOfferOrder_IsIncluded_InConversionAnalytics()
    {
        using var client = _factory.CreateClient();
        _factory.EnsureDatabaseSeeded();
        var offer = SeedActiveOffer();
        SeedOrderFromOffer(offer.Id);

        var adminToken = await ApiTestAuth.LoginAsync(client, "admin", "IntegrationAdminPw!9");
        Assert.NotNull(adminToken);
        ApiTestAuth.SetBearer(client, adminToken);

        var res = await ApiTestAuth.GetWithBearerAsync(client, "api/admin/offers/analytics", adminToken);
        res.EnsureSuccessStatusCode();
        var analytics = await res.Content.ReadFromJsonAsync<OffersAnalyticsSummaryDto>();

        Assert.NotNull(analytics);
        Assert.Contains(analytics!.Offers, row => row.OfferId == offer.Id && row.Orders >= 1);
    }

    [Fact]
    public async Task MobileOffers_OnlyIncludes_SelectedPharmacyTargets()
    {
        using var client = _factory.CreateClient();
        _factory.EnsurePharmacyTestUser();

        var pharmacyId = GetPharmacyTestId();
        var offer = SeedActiveOffer("selected_pharmacies");
        SeedOfferTarget(offer.Id, pharmacyId + 1000);

        var token = await ApiTestAuth.LoginAsync(client, "pharmacytest", "PharmacyPw!9");
        Assert.NotNull(token);
        ApiTestAuth.SetBearer(client, token);

        var hidden = await client.GetFromJsonAsync<List<MarketingOfferDto>>("/api/mobile/offers");
        Assert.DoesNotContain(hidden ?? [], o => o.Id == offer.Id);

        SeedOfferTarget(offer.Id, pharmacyId);
        var visible = await client.GetFromJsonAsync<List<MarketingOfferDto>>("/api/mobile/offers");
        Assert.Contains(visible ?? [], o => o.Id == offer.Id);
    }

    private MarketingOffer SeedActiveOffer(string audienceRule = "all")
    {
        _factory.EnsureDatabaseSeeded();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
        var offer = new MarketingOffer
        {
            Title = $"Integration offer {Guid.NewGuid():N}",
            Subtitle = "test",
            Description = "test",
            OfferType = "discount",
            AudienceRule = audienceRule,
            Status = "published",
            StartsAt = DateTime.Now.AddHours(-1),
            EndsAt = DateTime.Now.AddDays(2),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        db.MarketingOffers.Add(offer);
        db.SaveChanges();
        return offer;
    }

    private int GetPharmacyTestId()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
        return db.Users.Where(u => u.Username == "pharmacytest").Select(u => u.PharmacyId!.Value).First();
    }

    private void SeedOfferTarget(int offerId, int pharmacyId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
        if (!db.Pharmacies.Any(p => p.Id == pharmacyId))
        {
            db.Pharmacies.Add(new Pharmacy
            {
                Id = pharmacyId,
                Name = $"Target Pharmacy {pharmacyId}",
                AccountStatus = "active",
                CreatedAt = DateTime.Now
            });
            db.SaveChanges();
        }

        if (!db.MarketingOfferPharmacyTargets.Any(t => t.MarketingOfferId == offerId && t.PharmacyId == pharmacyId))
        {
            db.MarketingOfferPharmacyTargets.Add(new MarketingOfferPharmacyTarget
            {
                MarketingOfferId = offerId,
                PharmacyId = pharmacyId
            });
            db.SaveChanges();
        }
    }

    private void SeedOrderFromOffer(int offerId)
    {
        _factory.EnsureDatabaseSeeded();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
        var pharmacy = db.Pharmacies.FirstOrDefault() ?? new Pharmacy
        {
            Name = "Analytics Pharmacy",
            AccountStatus = "active",
            CreatedAt = DateTime.Now
        };
        if (pharmacy.Id == 0)
        {
            db.Pharmacies.Add(pharmacy);
            db.SaveChanges();
        }

        db.OfferEvents.Add(new OfferEvent
        {
            MarketingOfferId = offerId,
            PharmacyId = pharmacy.Id,
            DeviceId = "conversion-device",
            DeviceKey = "conversion-device",
            EventType = "impression",
            OccurredAt = DateTime.Now,
            EventDateKey = DateTime.Now.ToString("yyyy-MM-dd"),
            IsUniqueDailyImpression = true
        });

        db.Orders.Add(new Order
        {
            OrderNumber = $"TST-{Guid.NewGuid():N}"[..16],
            PharmacyId = pharmacy.Id,
            TotalAmount = 100,
            FinalTotal = 100,
            BalanceBefore = pharmacy.Balance,
            BalanceAfter = pharmacy.Balance + 100,
            Status = "pending",
            Source = "mobile",
            SourceOfferId = offerId,
            CreatedAt = DateTime.Now,
            MobileCreatedAt = DateTime.Now
        });
        db.SaveChanges();
    }
}
