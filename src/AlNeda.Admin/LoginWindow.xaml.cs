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

        ViewModel.Username = "admin";
        ViewModel.Password = "admin123";
        UsernameBox.Focus();
    }

    private void OnLoginSucceeded(object? sender, LoginSuccessEventArgs e)
    {
        Log.Information("User logged in: {Username} ({Role})", e.Username, e.Role);
        var mainWindow = ((App)Application.Current).Services.GetRequiredService<MainWindow>();
        mainWindow.ViewModel.Initialize(e.Username, e.Role);
        mainWindow.Show();
        Close();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }
}
