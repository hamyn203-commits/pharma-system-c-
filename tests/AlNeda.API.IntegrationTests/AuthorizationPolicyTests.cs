using System.Net;

namespace AlNeda.API.IntegrationTests;

public class AuthorizationPolicyTests : IClassFixture<AlNedaApiFactory>
{
    private readonly AlNedaApiFactory _factory;

    public AuthorizationPolicyTests(AlNedaApiFactory factory) => _factory = factory;

    [Fact]
    public async Task PharmacyJwt_IsForbidden_On_AdminProducts()
    {
        using var client = _factory.CreateClient();
        _factory.EnsurePharmacyTestUser();

        var token = await ApiTestAuth.LoginAsync(client, "pharmacytest", "PharmacyPw!9");
        Assert.NotNull(token);

        var res = await ApiTestAuth.GetWithBearerAsync(client, "api/products", token);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task PharmacyJwt_IsForbidden_On_AdminOrders()
    {
        using var client = _factory.CreateClient();
        _factory.EnsurePharmacyTestUser();

        var token = await ApiTestAuth.LoginAsync(client, "pharmacytest", "PharmacyPw!9");
        Assert.NotNull(token);

        var res = await ApiTestAuth.GetWithBearerAsync(client, "api/orders", token);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task AdminJwt_Succeeds_On_Products()
    {
        using var client = _factory.CreateClient();
        _factory.EnsureDatabaseSeeded();
        var token = await ApiTestAuth.LoginAsync(client, "admin", "IntegrationAdminPw!9");
        Assert.NotNull(token);

        var res = await ApiTestAuth.GetWithBearerAsync(client, "api/products", token);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
