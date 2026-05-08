using System.Net.Http;
using System.Net.NetworkInformation;
using AlNeda.Admin.Services.ApiClient;
using AlNeda.Core.Models;
using AlNeda.Data;
using Microsoft.EntityFrameworkCore;
using AlNeda.Core.Entities;
using AlNeda.DomainLogic;

namespace AlNeda.Admin.Services;

public class SyncService
{
    private readonly IAlNedaApiClient _apiClient;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private const string LastSyncKey = "LastSyncTime";

    public SyncService(IAlNedaApiClient apiClient, IDbContextFactory<AppDbContext> contextFactory)
    {
        _apiClient = apiClient;
        _contextFactory = contextFactory;
    }

    public bool IsNetworkAvailable() =>
        NetworkInterface.GetIsNetworkAvailable();

    public async Task<SyncResult> PerformSyncAsync()
    {
        if (!IsNetworkAvailable())
        {
            Serilog.Log.Warning("Network unavailable, queuing sync for later");
            return SyncResult.OfflineQueued;
        }

        try
        {
            await using var db = await _contextFactory.CreateDbContextAsync();

            // 1. Gather local changes
            var request = new SyncRequest
            {
                LastSyncTime = GetLastSyncTime(),
                Products = await db.Products.Where(p => !p.IsSynced).ToListAsync(),
                Categories = await db.Categories.Where(c => !c.IsSynced).ToListAsync(),
            };

            if (!request.Products.Any() && !request.Categories.Any())
            {
                return SyncResult.NoChanges;
            }

            // 2. Send to API
            var response = await _apiClient.SyncAsync(request);
            if (response == null || !response.Success)
                return SyncResult.Failed;

            // 3. Apply server changes to local
            foreach (var serverProduct in response.NewOrUpdatedProducts)
            {
                var localProduct = await db.Products
                    .FirstOrDefaultAsync(p => p.RemoteId == serverProduct.RemoteId || p.Id == serverProduct.Id);
                if (localProduct == null)
                    db.Products.Add(serverProduct);
                else
                    db.Entry(localProduct).CurrentValues.SetValues(serverProduct);
            }

            foreach (var serverCategory in response.NewOrUpdatedCategories)
            {
                var localCategory = await db.Categories
                    .FirstOrDefaultAsync(c => c.RemoteId == serverCategory.RemoteId || c.Id == serverCategory.Id);
                if (localCategory == null)
                    db.Categories.Add(serverCategory);
                else
                    db.Entry(localCategory).CurrentValues.SetValues(serverCategory);
            }

            // 4. Mark local changes as synced
            foreach (var p in request.Products) p.IsSynced = true;
            foreach (var c in request.Categories) c.IsSynced = true;

            await db.SaveChangesAsync();
            SaveLastSyncTime(response.ServerTime);

            return SyncResult.Success;
        }
        catch (ApiException ex)
        {
            Serilog.Log.Error(ex, "API sync failed: {Message}", ex.Message);
            return SyncResult.Failed;
        }
        catch (HttpRequestException ex)
        {
            Serilog.Log.Error(ex, "Network error during sync");
            return SyncResult.OfflineQueued;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Sync failed unexpectedly");
            return SyncResult.Failed;
        }
    }

    private static DateTime _lastSyncTime = DateTime.MinValue;

    private DateTime GetLastSyncTime() => _lastSyncTime;

    private void SaveLastSyncTime(DateTime time) => _lastSyncTime = time;
}

public enum SyncResult
{
    Success,
    NoChanges,
    Failed,
    OfflineQueued
}
