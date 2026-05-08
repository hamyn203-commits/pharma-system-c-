using System.Collections.ObjectModel;
using AlNeda.Admin.Services.ApiClient;
using AlNeda.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlNeda.Admin.ViewModels;

public partial class AuditLogViewModel : ObservableObject
{
    private readonly IAlNedaApiClient _apiClient;

    [ObservableProperty] private ObservableCollection<AuditLogDto> _logs = [];
    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private string _searchUsername = "";
    [ObservableProperty] private DateTime? _fromDate;
    [ObservableProperty] private DateTime? _toDate;

    public AuditLogViewModel(IAlNedaApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var filter = new AuditLogFilterRequest
            {
                Username = SearchUsername,
                From = FromDate,
                To = ToDate,
                Page = 1,
                PageSize = 100
            };
            var result = await _apiClient.GetAuditLogsAsync(filter);
            Logs = new ObservableCollection<AuditLogDto>(result.Items);
        }
        catch (Exception ex)
        {
            // Error handling can be added here
        }
        finally
        {
            IsLoading = false;
        }
    }
}