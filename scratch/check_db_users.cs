
using System;
using System.Linq;
using AlNeda.Data;
using AlNeda.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var dbPath = "pharmacy.db";
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite($"Data Source={dbPath}")
    .Options;

using var db = new AppDbContext(options);
var users = db.Users.ToList();

Console.WriteLine($"Found {users.Count} users:");
foreach (var u in users)
{
    Console.WriteLine($"- ID: {u.Id}, Username: '{u.Username}', Role: '{u.Role}', Salt: '{u.PasswordSalt}'");
}
