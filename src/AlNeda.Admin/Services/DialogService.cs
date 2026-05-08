using AlNeda.Core.Entities;
using AlNeda.Services;
using AlNeda.Admin.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AlNeda.Admin.Services;

public interface IDialogService
{
    bool Confirm(string message, string title = "تأكيد");
    string? ShowOpenFileDialog(string title, string filter, string? defaultFileName = null);
    void ShowMessage(string message, string title = "تنبيه");
    void ShowError(string message, string title = "خطأ");
    bool ShowUserDialog(User? user = null);
    bool ShowCategoryDialog(AlNeda.Core.Models.CategoryDto? category = null);
    void ShowCategoryProducts(Category category);
}

public class DialogService : IDialogService
{
    public bool Confirm(string message, string title = "تأكيد")
    {
        var result = System.Windows.MessageBox.Show(
            message,
            title,
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question
        );
        return result == System.Windows.MessageBoxResult.Yes;
    }

    public string? ShowOpenFileDialog(string title, string filter, string? defaultFileName = null)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = title,
            Filter = filter,
            FileName = defaultFileName ?? string.Empty
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public void ShowMessage(string message, string title = "تنبيه")
    {
        System.Windows.MessageBox.Show(
            message,
            title,
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information
        );
    }

    public void ShowError(string message, string title = "خطأ")
    {
        System.Windows.MessageBox.Show(
            message,
            title,
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Error
        );
    }

    public bool ShowUserDialog(User? user = null)
    {
        if (System.Windows.Application.Current == null) return false;
        var userService = ((App)System.Windows.Application.Current).Services.GetRequiredService<UserService>();
        var vm = new AddEditUserDialogViewModel(userService, user);
        var dialog = new Views.AddEditUserDialog(vm);
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        dialog.ShowDialog();
        return vm.IsSuccess;
    }

    public bool ShowCategoryDialog(AlNeda.Core.Models.CategoryDto? category = null)
    {
        if (System.Windows.Application.Current == null) return false;
        var service = ((App)System.Windows.Application.Current).Services.GetRequiredService<AlNeda.Admin.Services.ApiClient.IAlNedaApiClient>();
        var window = new Views.AddEditCategoryDialog();
        var vm = new AddEditCategoryDialogViewModel(service, window, category);
        window.DataContext = vm;
        window.Owner = System.Windows.Application.Current.MainWindow;
        return window.ShowDialog() == true;
    }

    public void ShowCategoryProducts(Category category)
    {
        if (System.Windows.Application.Current == null) return;
        var service = ((App)System.Windows.Application.Current).Services.GetRequiredService<ProductService>();
        var window = new Views.CategoryProductsDialog();
        var vm = new CategoryProductsDialogViewModel(service, window, category);
        window.DataContext = vm;
        window.Owner = System.Windows.Application.Current.MainWindow;
        window.ShowDialog();
    }
}
