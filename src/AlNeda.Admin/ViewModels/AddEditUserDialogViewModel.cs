
using AlNeda.Core.Entities;
using AlNeda.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlNeda.Admin.ViewModels;

public partial class AddEditUserDialogViewModel : ObservableObject
{
    private readonly UserService _userService;
    private readonly User? _originalUser;

    [ObservableProperty] private string _username = "";
    [ObservableProperty] private string _selectedRole = "rep";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _confirmPassword = "";
    [ObservableProperty] private string _errorMessage = "";

    public List<string> Roles { get; } = ["admin", "accountant", "rep"];

    public Action? CloseAction { get; set; }
    public bool IsSuccess { get; private set; }

    public AddEditUserDialogViewModel(UserService userService, User? user = null)
    {
        _userService = userService;
        _originalUser = user;

        if (user != null)
        {
            Username = user.Username;
            SelectedRole = user.Role;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "اسم المستخدم مطلوب";
            return;
        }

        if (_originalUser == null && string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "كلمة المرور مطلوبة للمستخدم الجديد";
            return;
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "كلمات المرور غير متطابقة";
            return;
        }

        try
        {
            if (_originalUser == null)
            {
                var newUser = new User { Username = Username, Role = SelectedRole };
                await _userService.AddUserAsync(newUser, Password);
            }
            else
            {
                _originalUser.Username = Username;
                _originalUser.Role = SelectedRole;
                await _userService.UpdateUserAsync(_originalUser, string.IsNullOrEmpty(Password) ? null : Password);
            }

            IsSuccess = true;
            CloseAction?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
