using System;
using Microsoft.EntityFrameworkCore;
using AlNeda.Data;
using AlNeda.Core.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

var services = new ServiceCollection();
services.AddDbContext<AppDbContext>(options => 
    options.UseSqlite("Data Source=pharmacy.db"));
var serviceProvider = services.BuildServiceProvider();

using var scope = serviceProvider.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

try 
{
    var users = db.Users.ToList();
    Console.WriteLine($"Found {users.Count} users:");
    foreach (var user in users)
    {
        Console.WriteLine($"- Username: {user.Username}, Role: {user.Role}, PasswordHash: {user.Password}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
