using System.Windows;
using System.Windows.Input;
using AlNeda.Admin.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace AlNeda.Admin;

public partial class LoginWindow : Window
{
    public LoginViewModel ViewModel { get; }

    public LoginWindow()
    {
        InitializeComponent();
        ViewModel = ((App)Application.Current).Services.GetRequiredService<LoginViewModel>();
        DataContext = ViewModel;
        ViewModel.LoginSucceeded += OnLoginSucceeded;

        ViewModel.PropertyChanged += (s, e) => {
            if (e.PropertyName == nameof(LoginViewModel.ShowPassword) && !ViewModel.ShowPassword)
            {
                PasswordBox.Password = ViewModel.Password;
            }
        };

        ViewModel.Username = "admin";
        ViewModel.Password = "admin";
        PasswordBox.Password = "admin"; // Initialize the PasswordBox too
        UsernameBox.Focus();
    }

    private void OnLoginSucceeded(object? sender, LoginSuccessEventArgs e)
    {
        Log.Information("User logged in: {Username} ({Role})", e.Username, e.Role);

        Hide();
        var welcomeWindow = new WelcomeWindow
        {
            Owner = this
        };
        welcomeWindow.ShowDialog();

        var mainWindow = ((App)Application.Current).Services.GetRequiredService<MainWindow>();
        mainWindow.ViewModel.Initialize(e.Username, e.Role);
        mainWindow.Show();
        Close();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && !vm.ShowPassword)
        {
            vm.Password = PasswordBox.Password;
        }
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }
}
