using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AlNeda.Core.Models;

namespace AlNeda.Admin.Services.ApiClient;

public class ApiErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string Timestamp { get; set; } = string.Empty;
}

public class ApiException : Exception
{
    public int StatusCode { get; }
    public ApiErrorResponse? ErrorResponse { get; }

    public ApiException(string message, int statusCode = 0, ApiErrorResponse? errorResponse = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorResponse = errorResponse;
    }
}

public interface IAlNedaApiClient
{
    // Auth
    Task<LoginResult?> LoginAsync(string username, string password);
    Task<bool> ValidateTokenAsync();
    Task<SyncResponse?> SyncAsync(SyncRequest request);
    Task<byte[]?> DownloadReportAsync(string reportPath);
    void UpdateBaseUrl(string newBaseUrl);

    // Dashboard
    Task<DashboardStats?> GetDashboardStatsAsync();
    Task<List<SalesDataPoint>?> GetLast30DaysSalesAsync();
    Task<List<ProductAlertDto>?> GetLowStockProductsAsync();
    Task<List<ProductAlertDto>?> GetExpiringProductsAsync();
    Task<List<ProductAlertDto>?> GetExpiredProductsAsync();

    // Categories
    Task<List<CategoryDto>> GetCategoriesAsync();
    Task<CategoryDto?> GetCategoryByIdAsync(int id);
    Task<CategoryDto?> CreateCategoryAsync(CreateCategoryRequest request);
    Task<CategoryDto?> UpdateCategoryAsync(UpdateCategoryRequest request);
    Task DeleteCategoryAsync(int id);

    // Products
    Task<List<ProductDto>> GetProductsAsync(string? search = null);
    Task<ProductDto?> GetProductByIdAsync(int id);
    Task<ProductDto?> CreateProductAsync(CreateProductRequest request);
    Task<ProductDto?> UpdateProductAsync(UpdateProductRequest request);
    Task DeleteProductAsync(int id);

    // Orders
    Task<List<OrderDto>> GetOrdersAsync(string? status = null);
    Task<OrderDto?> GetOrderByIdAsync(int id);
    Task<OrderDto?> CreateOrderAsync(CreateOrderRequest request);
    Task<OrderDto?> UpdateOrderStatusAsync(int id, string status);

    // Purchases
    Task<List<PurchaseDto>> GetPurchasesAsync();
    Task<PurchaseDto?> CreatePurchaseAsync(CreatePurchaseRequest request);

    // Returns
    Task<List<ReturnDto>> GetReturnsAsync();
    Task<ReturnDto?> CreateReturnAsync(CreateReturnRequest request);

    // Payments
    Task<List<PaymentDto>> GetPaymentsAsync(int? pharmacyId = null);
    Task<PaymentDto?> CreatePaymentAsync(CreatePaymentRequest request);

    // Audit Log
    Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(AuditLogFilterRequest filter);

    // Pharmacies
    Task<List<PharmacyDto>> GetPharmaciesAsync(string? search = null);
    Task<PharmacyDto?> GetPharmacyByIdAsync(int id);
    Task<PharmacyDto?> CreatePharmacyAsync(CreatePharmacyRequest request);
    Task<PharmacyDto?> UpdatePharmacyAsync(UpdatePharmacyRequest request);
    Task DeletePharmacyAsync(int id);
    Task SetPharmacyStatusAsync(int id, string status);

    // Suppliers
    Task<List<SupplierDto>> GetSuppliersAsync(string? search = null);
    Task<SupplierDto?> GetSupplierByIdAsync(int id);
    Task<SupplierDto?> CreateSupplierAsync(CreateSupplierRequest request);
    Task<SupplierDto?> UpdateSupplierAsync(UpdateSupplierRequest request);
    Task DeleteSupplierAsync(int id);

    // Settings / Users
    Task<List<AppUserDto>> GetUsersAsync();
    Task<AppUserDto?> CreateUserAsync(CreateUserRequest request);
    Task<AppUserDto?> UpdateUserAsync(UpdateUserRequest request);
    Task DeleteUserAsync(int userId);
}

public class ProductAlertDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? ExpiryDate { get; set; }
    public decimal Price { get; set; }
}

public class LoginResult
{
    public string Token { get; set; } = string.Empty;
    public LoginUser? User { get; set; }
}

public class LoginUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class ApiClientService : IAlNedaApiClient, IDisposable
{
    private HttpClient _httpClient;
    private string? _token;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public ApiClientService(string baseUrl = "http://localhost:5000")
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public void SetToken(string token)
    {
        _token = token;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public void UpdateBaseUrl(string newBaseUrl)
    {
        var newUri = new Uri(newBaseUrl.TrimEnd('/') + "/");
        if (_httpClient.BaseAddress == newUri) return;

        var token = _token;
        _httpClient.Dispose();
        _httpClient = new HttpClient
        {
            BaseAddress = newUri,
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrEmpty(token))
            SetToken(token);
    }

    public async Task<LoginResult?> LoginAsync(string username, string password)
    {
        var request = new LoginRequest { Username = username, Password = password };
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", request, _jsonOptions);

        if (!response.IsSuccessStatusCode)
            return null;

        var result = await response.Content.ReadFromJsonAsync<LoginResult>(_jsonOptions);
        if (result != null && !string.IsNullOrEmpty(result.Token))
        {
            SetToken(result.Token);
        }
        return result;
    }

    public async Task<SyncResponse?> SyncAsync(SyncRequest request)
    {
        EnsureAuthenticated();
        var response = await _httpClient.PostAsJsonAsync("api/sync", request, _jsonOptions);
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<SyncResponse>(_jsonOptions);
    }

    public async Task<bool> ValidateTokenAsync()
    {
        if (string.IsNullOrEmpty(_token))
            return false;

        var response = await _httpClient.PostAsync("api/auth/validate", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<byte[]?> DownloadReportAsync(string reportPath)
    {
        EnsureAuthenticated();
        var response = await _httpClient.GetAsync($"api/reports/{reportPath.TrimStart('/')}");
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<DashboardStats?> GetDashboardStatsAsync()
    {
        EnsureAuthenticated();
        var response = await _httpClient.GetAsync("api/dashboard/stats");
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<DashboardStats>(_jsonOptions);
    }

    public async Task<List<SalesDataPoint>?> GetLast30DaysSalesAsync()
    {
        EnsureAuthenticated();
        var response = await _httpClient.GetAsync("api/dashboard/sales/last30");
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<List<SalesDataPoint>>(_jsonOptions);
    }

    public async Task<List<ProductAlertDto>?> GetLowStockProductsAsync()
    {
        EnsureAuthenticated();
        var response = await _httpClient.GetAsync("api/dashboard/alerts/low-stock");
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<List<ProductAlertDto>>(_jsonOptions);
    }

    public async Task<List<ProductAlertDto>?> GetExpiringProductsAsync()
    {
        EnsureAuthenticated();
        var response = await _httpClient.GetAsync("api/dashboard/alerts/expiring");
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<List<ProductAlertDto>>(_jsonOptions);
    }

    public async Task<List<ProductAlertDto>?> GetExpiredProductsAsync()
    {
        EnsureAuthenticated();
        var response = await _httpClient.GetAsync("api/dashboard/alerts/expired");
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<List<ProductAlertDto>>(_jsonOptions);
    }

    // ── Categories ──
    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        EnsureAuthenticated();
        var response = await _httpClient.GetAsync("api/categories");
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<List<CategoryDto>>(_jsonOptions) ?? [];
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
    {
        EnsureAuthenticated();
        var response = await _httpClient.GetAsync($"api/categories/{id}");
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<CategoryDto>(_jsonOptions);
    }

    public async Task<CategoryDto?> CreateCategoryAsync(CreateCategoryRequest request)
    {
        EnsureAuthenticated();
        var response = await _httpClient.PostAsJsonAsync("api/categories", request, _jsonOptions);
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<CategoryDto>(_jsonOptions);
    }

    public async Task<CategoryDto?> UpdateCategoryAsync(UpdateCategoryRequest request)
    {
        EnsureAuthenticated();
        var response = await _httpClient.PutAsJsonAsync($"api/categories/{request.Id}", request, _jsonOptions);
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<CategoryDto>(_jsonOptions);
    }

    public async Task DeleteCategoryAsync(int id)
    {
        EnsureAuthenticated();
        var response = await _httpClient.DeleteAsync($"api/categories/{id}");
        await ThrowIfErrorAsync(response);
    }

    // ── Products ──
    public async Task<List<ProductDto>> GetProductsAsync(string? search = null)
    {
        EnsureAuthenticated();
        var url = "api/products";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"?search={Uri.EscapeDataString(search)}";
        var response = await _httpClient.GetAsync(url);
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<List<ProductDto>>(_jsonOptions) ?? [];
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        EnsureAuthenticated();
        var response = await _httpClient.GetAsync($"api/products/{id}");
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<ProductDto>(_jsonOptions);
    }

    public async Task<ProductDto?> CreateProductAsync(CreateProductRequest request)
    {
        EnsureAuthenticated();
        var response = await _httpClient.PostAsJsonAsync("api/products", request, _jsonOptions);
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<ProductDto>(_jsonOptions);
    }

    public async Task<ProductDto?> UpdateProductAsync(UpdateProductRequest request)
    {
        EnsureAuthenticated();
        var response = await _httpClient.PutAsJsonAsync($"api/products/{request.Id}", request, _jsonOptions);
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<ProductDto>(_jsonOptions);
    }

    public async Task DeleteProductAsync(int id)
    {
        EnsureAuthenticated();
        var response = await _httpClient.DeleteAsync($"api/products/{id}");
        await ThrowIfErrorAsync(response);
    }

    // ── Orders ──
    public async Task<List<OrderDto>> GetOrdersAsync(string? status = null)
    {
        EnsureAuthenticated();
        var url = "api/orders";
        if (!string.IsNullOrWhiteSpace(status))
            url += $"?status={Uri.EscapeDataString(status)}";
        var response = await _httpClient.GetAsync(url);
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<List<OrderDto>>(_jsonOptions) ?? [];
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id)
    {
        EnsureAuthenticated();
        var response = await _httpClient.GetAsync($"api/orders/{id}");
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<OrderDto>(_jsonOptions);
    }

    public async Task<OrderDto?> CreateOrderAsync(CreateOrderRequest request)
    {
        EnsureAuthenticated();
        var response = await _httpClient.PostAsJsonAsync("api/orders", request, _jsonOptions);
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<OrderDto>(_jsonOptions);
    }

    public async Task<OrderDto?> UpdateOrderStatusAsync(int id, string status)
    {
        EnsureAuthenticated();
        var response = await _httpClient.PutAsJsonAsync($"api/orders/{id}/status", new { status }, _jsonOptions);
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<OrderDto>(_jsonOptions);
    }

    // ── Purchases ──
    public async Task<List<PurchaseDto>> GetPurchasesAsync()
    {
        EnsureAuthenticated();
        var response = await _httpClient.GetAsync("api/purchases");
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<List<PurchaseDto>>(_jsonOptions) ?? [];
    }

    public async Task<PurchaseDto?> CreatePurchaseAsync(CreatePurchaseRequest request)
    {
        EnsureAuthenticated();
        var response = await _httpClient.PostAsJsonAsync("api/purchases", request, _jsonOptions);
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<PurchaseDto>(_jsonOptions);
    }

    // ── Returns ──
    public async Task<List<ReturnDto>> GetReturnsAsync()
    {
        EnsureAuthenticated();
        var response = await _httpClient.GetAsync("api/returns");
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<List<ReturnDto>>(_jsonOptions) ?? [];
    }

    public async Task<ReturnDto?> CreateReturnAsync(CreateReturnRequest request)
    {
        EnsureAuthenticated();
        var response = await _httpClient.PostAsJsonAsync("api/returns", request, _jsonOptions);
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<ReturnDto>(_jsonOptions);
    }

    // ── Payments ──
    public async Task<List<PaymentDto>> GetPaymentsAsync(int? pharmacyId = null)
    {
        EnsureAuthenticated();
        var url = "api/payments";
        if (pharmacyId.HasValue)
            url += $"?pharmacyId={pharmacyId.Value}";

        var response = await _httpClient.GetAsync(url);
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<List<PaymentDto>>(_jsonOptions) ?? [];
    }

    public async Task<PaymentDto?> CreatePaymentAsync(CreatePaymentRequest request)
    {
        EnsureAuthenticated();
        var response = await _httpClient.PostAsJsonAsync("api/payments", request, _jsonOptions);
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<PaymentDto>(_jsonOptions);
    }

    // ── Audit Log ──
    public async Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(AuditLogFilterRequest filter)
    {
        EnsureAuthenticated();
        var response = await _httpClient.PostAsJsonAsync("api/audit-logs", filter, _jsonOptions);
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<PagedResult<AuditLogDto>>(_jsonOptions) ?? new PagedResult<AuditLogDto>();
    }

    // ── Users ──
    public async Task<List<AppUserDto>> GetUsersAsync()
    {
        EnsureAuthenticated();
        var response = await _httpClient.GetAsync("api/users");
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<List<AppUserDto>>(_jsonOptions) ?? [];
    }

    public async Task<AppUserDto?> CreateUserAsync(CreateUserRequest request)
    {
        EnsureAuthenticated();
        var response = await _httpClient.PostAsJsonAsync("api/users", request, _jsonOptions);
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<AppUserDto>(_jsonOptions);
    }

    public async Task<AppUserDto?> UpdateUserAsync(UpdateUserRequest request)
    {
        EnsureAuthenticated();
        var response = await _httpClient.PutAsJsonAsync($"api/users/{request.Id}", request, _jsonOptions);
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<AppUserDto>(_jsonOptions);
    }

    public async Task DeleteUserAsync(int userId)
    {
        EnsureAuthenticated();
        var response = await _httpClient.DeleteAsync($"api/users/{userId}");
        await ThrowIfErrorAsync(response);
    }

    // ── Pharmacies ──
    public async Task<List<PharmacyDto>> GetPharmaciesAsync(string? search = null)
    {
        EnsureAuthenticated();
        var url = "api/pharmacies";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"?search={Uri.EscapeDataString(search)}";
        var response = await _httpClient.GetAsync(url);
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<List<PharmacyDto>>(_jsonOptions) ?? [];
    }

    public async Task<PharmacyDto?> GetPharmacyByIdAsync(int id)
    {
        EnsureAuthenticated();
        var response = await _httpClient.GetAsync($"api/pharmacies/{id}");
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<PharmacyDto>(_jsonOptions);
    }

    public async Task<PharmacyDto?> CreatePharmacyAsync(CreatePharmacyRequest request)
    {
        EnsureAuthenticated();
        var response = await _httpClient.PostAsJsonAsync("api/pharmacies", request, _jsonOptions);
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<PharmacyDto>(_jsonOptions);
    }

    public async Task<PharmacyDto?> UpdatePharmacyAsync(UpdatePharmacyRequest request)
    {
        EnsureAuthenticated();
        var response = await _httpClient.PutAsJsonAsync($"api/pharmacies/{request.Id}", request, _jsonOptions);
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<PharmacyDto>(_jsonOptions);
    }

    public async Task DeletePharmacyAsync(int id)
    {
        EnsureAuthenticated();
        var response = await _httpClient.DeleteAsync($"api/pharmacies/{id}");
        await ThrowIfErrorAsync(response);
    }

    public async Task SetPharmacyStatusAsync(int id, string status)
    {
        EnsureAuthenticated();
        var response = await _httpClient.PutAsJsonAsync($"api/pharmacies/{id}/status", new PharmacyStatusRequest { Status = status }, _jsonOptions);
        await ThrowIfErrorAsync(response);
    }

    // ── Suppliers ──
    public async Task<List<SupplierDto>> GetSuppliersAsync(string? search = null)
    {
        EnsureAuthenticated();
        var url = "api/suppliers";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"?search={Uri.EscapeDataString(search)}";
        var response = await _httpClient.GetAsync(url);
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<List<SupplierDto>>(_jsonOptions) ?? [];
    }

    public async Task<SupplierDto?> GetSupplierByIdAsync(int id)
    {
        EnsureAuthenticated();
        var response = await _httpClient.GetAsync($"api/suppliers/{id}");
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<SupplierDto>(_jsonOptions);
    }

    public async Task<SupplierDto?> CreateSupplierAsync(CreateSupplierRequest request)
    {
        EnsureAuthenticated();
        var response = await _httpClient.PostAsJsonAsync("api/suppliers", request, _jsonOptions);
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<SupplierDto>(_jsonOptions);
    }

    public async Task<SupplierDto?> UpdateSupplierAsync(UpdateSupplierRequest request)
    {
        EnsureAuthenticated();
        var response = await _httpClient.PutAsJsonAsync($"api/suppliers/{request.Id}", request, _jsonOptions);
        await ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<SupplierDto>(_jsonOptions);
    }

    public async Task DeleteSupplierAsync(int id)
    {
        EnsureAuthenticated();
        var response = await _httpClient.DeleteAsync($"api/suppliers/{id}");
        await ThrowIfErrorAsync(response);
    }

    private void EnsureAuthenticated()
    {
        if (string.IsNullOrEmpty(_token))
            throw new ApiException("يجب تسجيل الدخول أولاً", 401);
    }

    private async Task ThrowIfErrorAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(_jsonOptions);
            if (error != null && !string.IsNullOrEmpty(error.Message))
                throw new ApiException(error.Message, (int)response.StatusCode, error);
        }
        catch (JsonException) { /* ignore parse errors */ }

        var statusCode = (int)response.StatusCode;
        var reason = statusCode switch
        {
            401 => "غير مصرح به - يرجى تسجيل الدخول مرة أخرى",
            403 => "غير مسموح بالوصول",
            404 => "الموارد غير موجودة",
            500 => "خطأ في الخادم الداخلي",
            _ => $"خطأ في الخادم (رمز: {statusCode})"
        };
        throw new ApiException(reason, statusCode);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
