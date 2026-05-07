using System.Windows.Input;
using AlNeda.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlNeda.Admin.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly AuthService _authService;

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

    public List<string> Roles { get; } = ["admin", "accountant", "rep"];

    public event EventHandler<LoginSuccessEventArgs>? LoginSucceeded;

    public LoginViewModel(AuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task LoginAsync(object? parameter)
    {
        if (parameter is System.Windows.Controls.PasswordBox passwordBox)
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
            var user = await _authService.LoginAsync(Username.Trim(), Password);
            if (user != null)
            {
                // Verify role if selected
                if (!string.IsNullOrEmpty(SelectedRole) && user.Role.ToLower() != SelectedRole.ToLower())
                {
                    ErrorMessage = $"هذا المستخدم ليس لديه صلاحية {SelectedRole}";
                    return;
                }

                LoginSucceeded?.Invoke(this, new LoginSuccessEventArgs(user.Username, user.Role));
            }
            else
            {
                ErrorMessage = "اسم المستخدم أو كلمة المرور غير صحيحة";
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Login failed");
            ErrorMessage = $"خطأ في الاتصال بقاعدة البيانات: {ex.Message}";
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
        // Bypass authentication for development/testing
        LoginSucceeded?.Invoke(this, new LoginSuccessEventArgs("admin", "admin"));
    }
}

public record LoginSuccessEventArgs(string Username, string Role);
