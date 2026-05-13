namespace AlNeda.Core.Models;

public class ApiError
{
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = "SERVER_ERROR";
    public object Details { get; set; } = new { };
}

public class LoginResponseDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? PharmacyId { get; set; }
    public string? PharmacyName { get; set; }
    public string Token { get; set; } = string.Empty;
    public LoginUserDto User { get; set; } = new();
}

public class LoginUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? PharmacyId { get; set; }
    public string? PharmacyName { get; set; }
}

public class PharmacyRegisterRequest
{
    public string PharmacyName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
}

public class PharmacyRegisterResponseDto
{
    public int PharmacyId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PharmacyName { get; set; } = string.Empty;
    public string AccountStatus { get; set; } = "pending";
    public bool RequiresApproval { get; set; } = true;
    public string Message { get; set; } = string.Empty;
}

public class MobileCreateOrderRequest
{
    public List<MobileCreateOrderItemRequest> Items { get; set; } = [];
    public string Notes { get; set; } = string.Empty;
    public int? SourceOfferId { get; set; }
}

public class MobileCreateOrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class MobileCancelOrderRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class MobileCreateReturnRequest
{
    public int? OrderId { get; set; }
    public string? ReturnType { get; set; }
    public string? Reason { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<MobileCreateOrderItemRequest> Items { get; set; } = [];
}

public class MobileSyncRequest
{
    public DateTime? LastSyncTime { get; set; }
}

public class MobileSyncResponse
{
    public List<ProductDto> Products { get; set; } = [];
    public List<CategoryDto> Categories { get; set; } = [];
    public List<OrderDto> Orders { get; set; } = [];
    public List<PaymentDto> Payments { get; set; } = [];
    public DateTime ServerTime { get; set; }
}

public class MobileSettingsDto
{
    public bool AcceptMobileOrders { get; set; } = true;
    public string ApiUrl { get; set; } = string.Empty;
    public int NotificationPollingSeconds { get; set; } = 30;
    public bool AllowMobileCancellation { get; set; } = true;
    public bool ShowUnavailableProducts { get; set; }
    public int MaxQuantityPerOrderItem { get; set; } = 100;
}
