using System;
using Microsoft.Data.Sqlite;

try {
    using var conn = new SqliteConnection("Data Source=pharmacy.db");
    conn.Open();
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT username, role, password FROM users";
    using var reader = cmd.ExecuteReader();
    Console.WriteLine("--- Users in pharmacy.db ---");
    while (reader.Read()) {
        Console.WriteLine($"User: {reader[\"username\"]}, Role: {reader[\"role\"]}, Password: {reader[\"password\"]}");
    }
} catch (Exception ex) {
    Console.WriteLine($"Error: {ex.Message}");
}
