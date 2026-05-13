using System.Windows.Controls;
using AlNeda.Admin.ViewModels;

namespace AlNeda.Admin.Views;

public partial class OffersView : UserControl
{
    public OffersView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is OffersViewModel vm)
                await vm.LoadAsync();
        };
    }
}
