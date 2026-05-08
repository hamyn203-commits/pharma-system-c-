using AlNeda.Core.Models;
using Refit;

namespace AlNeda.Admin.Services;

public interface IAlNedaApi
{
    [Post("/api/auth/login")]
    Task<LoginResponse> LoginAsync([Body] LoginRequest request);

    [Headers("Authorization: Bearer")]
    [Post("/api/sync")]
    Task<SyncResponse> SyncAsync([Body] SyncRequest request);
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public LoginUserInfo? User { get; set; }
}

public class LoginUserInfo
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
