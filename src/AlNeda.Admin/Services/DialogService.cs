namespace AlNeda.Admin.Services;

public interface IDialogService
{
    bool Confirm(string message, string title = "تأكيد");
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
}
