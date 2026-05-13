using System.Net.Http;
using System.Windows.Input;
using AlNeda.Admin.Services.ApiClient;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlNeda.Admin.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAlNedaApiClient _apiClient;
    private readonly bool _allowDevSkip;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _loginButtonText = "تسجيل الدخول";

    [ObservableProperty]
    private string _selectedRole = "admin";

    [ObservableProperty]
    private bool _showPassword;

    [ObservableProperty]
    private bool _showDevSkip;

    partial void OnShowPasswordChanged(bool value)
    {
    }

    public List<string> Roles { get; } = ["admin", "accountant", "rep"];

    public event EventHandler<LoginSuccessEventArgs>? LoginSucceeded;

    public LoginViewModel(IAlNedaApiClient apiClient)
    {
        _apiClient = apiClient;
        _allowDevSkip = string.Equals(
            Environment.GetEnvironmentVariable("ALNEDA_ALLOW_DEV_SKIP"),
            "true",
            StringComparison.OrdinalIgnoreCase);
        ShowDevSkip = _allowDevSkip;
    }

    [RelayCommand]
    private async Task LoginAsync(object? parameter)
    {
        if (!ShowPassword && parameter is System.Windows.Controls.PasswordBox passwordBox)
        {
            Password = passwordBox.Password;
        }

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "يرجى إدخال اسم المستخدم وكلمة المرور";
            return;
        }

        IsLoading = true;
        LoginButtonText = "جاري تسجيل الدخول...";
        ErrorMessage = string.Empty;

        try
        {
            var result = await _apiClient.LoginAsync(Username.Trim(), Password);
            if (result?.User != null)
            {
                if (!string.IsNullOrEmpty(SelectedRole) && 
                    !string.Equals(result.User.Role, SelectedRole, StringComparison.OrdinalIgnoreCase))
                {
                    ErrorMessage = $"هذا المستخدم ليس لديه صلاحية {SelectedRole}";
                    return;
                }

                LoginSucceeded?.Invoke(this, new LoginSuccessEventArgs(result.User.Username, result.User.Role));
            }
            else
            {
                ErrorMessage = "اسم المستخدم أو كلمة المرور غير صحيحة";
            }
        }
        catch (ApiException apiEx)
        {
            ErrorMessage = apiEx.ErrorResponse != null 
                ? $"خطأ من الخادم: {apiEx.ErrorResponse.Message}" 
                : $"خطأ في الاتصال (رمز: {apiEx.StatusCode})";
            Serilog.Log.Warning(apiEx, "API Login error");
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "تعذر الاتصال بالخادم. تأكد من تشغيل الخدمة";
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Login failed unexpectedly");
            ErrorMessage = $"خطأ غير متوقع: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            LoginButtonText = "تسجيل الدخول";
        }
    }

    [RelayCommand]
    private void SkipLogin()
    {
        if (!_allowDevSkip)
        {
            ErrorMessage = "تخطي تسجيل الدخول معطل في هذه البيئة";
            return;
        }

        LoginSucceeded?.Invoke(this, new LoginSuccessEventArgs("admin", "admin"));
    }
}

public record LoginSuccessEventArgs(string Username, string Role);
