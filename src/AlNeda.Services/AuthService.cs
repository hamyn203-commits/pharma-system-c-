using AlNeda.Core.Entities;
using Microsoft.EntityFrameworkCore;
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
        
        // Check if any users exist, if not, seed the default admin
        if (!await db.Users.AnyAsync())
        {
            var defaultAdmin = new User 
            { 
                Username = "admin", 
                Password = HashPassword("admin123"), 
                Role = "admin",
                CreatedAt = DateTime.Now 
            };
            db.Users.Add(defaultAdmin);
            await db.SaveChangesAsync();
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return null;

        var hashedInput = HashPassword(password);
        if (user.Password != hashedInput && user.Password != password)
            return null;

        if (user.Password == password)
        {
            user.Password = HashPassword(password);
            await db.SaveChangesAsync();
        }

        return user;
    }

    public bool VerifyHash(string raw, string stored)
    {
        return stored == HashPassword(raw) || stored == raw;
    }

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }
}
