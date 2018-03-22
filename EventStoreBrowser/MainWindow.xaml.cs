using EventStoreBrowser.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EventStoreBrowser
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = DataContext as MainViewModel;
        }

        private async void OnConnectButtonClick(object sender, RoutedEventArgs e)
        {
            await ConnectAndRead();
        }
        private async void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                //explicitly update Text property binding
                (sender as TextBox).GetBindingExpression(TextBox.TextProperty).UpdateSource();
                await ConnectAndRead();
            }
        }

        private async System.Threading.Tasks.Task ConnectAndRead()
        {
            IsEnabled = false;
            WindowProgressBar.Visibility = Visibility.Visible;
            await ViewModel.ConnectAndReadAsync();
            WindowProgressBar.Visibility = Visibility.Collapsed;
            IsEnabled = true;
        }

        private void OnCopyButtonClick(object sender, RoutedEventArgs e) => ViewModel.CopyToClipboard();

        private void OnCloneButtonClick(object sender, RoutedEventArgs e)
        {
            WriteToDialog.ShowNew(this, ViewModel.CloneTo);
        }

        private async void OnUndoSoftDeleteButtonClick(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;
            WindowProgressBar.Visibility = Visibility.Visible;
            await ViewModel.UndoLastSoftDeleteAsync();
            WindowProgressBar.Visibility = Visibility.Collapsed;
            IsEnabled = true;
        }

        private async void OnSoftDeleteAtButtonClick(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;
            WindowProgressBar.Visibility = Visibility.Visible;
            await ViewModel.SetBeginningOfStreamAsync();
            WindowProgressBar.Visibility = Visibility.Collapsed;
            IsEnabled = true;
        }

        private async void OnEditMetadataButtonClick(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;
            WindowProgressBar.Visibility = Visibility.Visible;
            await ViewModel.EditMetadataAsync(this);
            WindowProgressBar.Visibility = Visibility.Collapsed;
            IsEnabled = true;
        }

    }
}
