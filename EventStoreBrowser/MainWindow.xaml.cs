using EventStoreBrowser.ViewModels;
using System.Windows;

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
