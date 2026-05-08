
using System.Windows;
using AlNeda.Admin.ViewModels;

namespace AlNeda.Admin.Views;

public partial class AddEditUserDialog : Window
{
    public AddEditUserDialog(AddEditUserDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseAction = () => Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
