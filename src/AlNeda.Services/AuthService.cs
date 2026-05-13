using AlNeda.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Security.Cryptography;
using System.Text;

namespace AlNeda.Services;

public class AuthService
{
    private readonly IDbContextFactory<AlNeda.Data.AppDbContext> _contextFactory;

    public AuthService(IDbContextFactory<AlNeda.Data.AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();

        var user = await db.Users
            .Include(u => u.Pharmacy)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        if (user == null)
        {
            Serilog.Log.Warning("Login failed: User '{Username}' not found", username);
            return null;
        }

        // Support legacy SHA256 or Plaintext for a migration period if needed, 
        // but since this is a new project structure, we transition to PBKDF2.
        if (string.IsNullOrEmpty(user.PasswordSalt))
        {
            var legacyHash = LegacyHash(password);
            if (user.Password == legacyHash)
            {
                Serilog.Log.Information("Upgrading legacy SHA256 password for user '{Username}'", username);
                var (newHash, newSalt) = HashPassword(password);
                user.Password = newHash;
                user.PasswordSalt = newSalt;
                user.LastLoginAt = DateTime.Now;
                if (user.Role == "pharmacy" && user.Pharmacy != null)
                    user.Pharmacy.LastLoginAt = user.LastLoginAt;
                await db.SaveChangesAsync();
                return user;
            }

            // رفض كلمات المرور المخزنة كنص صريح — يجب إعادة تعيين كلمة المرور من الإدارة
            if (string.Equals(user.Password, password, StringComparison.Ordinal))
            {
                Serilog.Log.Warning(
                    "Login rejected: user '{Username}' has plaintext password in database; reset password required",
                    username);
                return null;
            }

            Serilog.Log.Warning("Login failed: Legacy password mismatch for user '{Username}'", username);
            return null;
        }

        if (!VerifyPassword(password, user.Password, user.PasswordSalt))
        {
            Serilog.Log.Warning("Login failed: Password mismatch for user '{Username}'", username);
            return null;
        }

        user.LastLoginAt = DateTime.Now;
        if (user.Role == "pharmacy" && user.Pharmacy != null)
            user.Pharmacy.LastLoginAt = user.LastLoginAt;
        await db.SaveChangesAsync();

        Serilog.Log.Information("Login successful for user '{Username}'", username);
        return user;
    }

    public static (string Hash, string Salt) HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(32);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations: 100000,
            HashAlgorithmName.SHA256,
            outputLength: 32);
        return (Convert.ToHexString(hash).ToLower(), Convert.ToHexString(salt).ToLower());
    }

    public static bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        var salt = Convert.FromHexString(storedSalt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations: 100000,
            HashAlgorithmName.SHA256,
            outputLength: 32);
        return Convert.ToHexString(hash).ToLower() == storedHash;
    }

    private static string LegacyHash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }
}
