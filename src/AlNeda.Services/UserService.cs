
using AlNeda.Core.Entities;
using AlNeda.Data;
using Microsoft.EntityFrameworkCore;

namespace AlNeda.Services;

public class UserService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public UserService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        return await db.Users.OrderBy(u => u.Username).ToListAsync();
    }

    public async Task AddUserAsync(User user, string plainPassword)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var (hash, salt) = AuthService.HashPassword(plainPassword);
        user.Password = hash;
        user.PasswordSalt = salt;
        user.CreatedAt = DateTime.Now;
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    public async Task UpdateUserAsync(User user, string? newPlainPassword = null)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        if (!string.IsNullOrEmpty(newPlainPassword))
        {
            var (hash, salt) = AuthService.HashPassword(newPlainPassword);
            user.Password = hash;
            user.PasswordSalt = salt;
        }
        db.Users.Update(user);
        await db.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(int userId)
    {
        await using var db = await _contextFactory.CreateDbContextAsync();
        var user = await db.Users.FindAsync(userId);
        if (user != null)
        {
            // Prevent deleting the last admin
            if (user.Role.ToLower() == "admin")
            {
                var adminCount = await db.Users.CountAsync(u => u.Role.ToLower() == "admin");
                if (adminCount <= 1)
                    throw new Exception("لا يمكن حذف آخر مدير في النظام");
            }

            db.Users.Remove(user);
            await db.SaveChangesAsync();
        }
    }
}
