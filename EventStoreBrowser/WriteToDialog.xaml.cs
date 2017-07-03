using System;
using System.Threading.Tasks;
using System.Windows;

namespace EventStoreBrowser
{
    public partial class WriteToDialog : Window
    {
        public static readonly DependencyProperty ConnectionUriProperty =
            DependencyProperty.Register(nameof(ConnectionUri), typeof(string), typeof(WriteToDialog), new PropertyMetadata("tcp://localhost:1113"));

        public static readonly DependencyProperty StreamNameProperty =
            DependencyProperty.Register(nameof(StreamName), typeof(string), typeof(WriteToDialog), new PropertyMetadata("develop"));

        private Func<string, string, Task> _callback;

        public string ConnectionUri
        {
            get => (string)GetValue(ConnectionUriProperty);
            set => SetValue(ConnectionUriProperty, value);
        }

        public string StreamName
        {
            get => (string)GetValue(StreamNameProperty);
            set => SetValue(StreamNameProperty, value);
        }

        public WriteToDialog()
        {
            InitializeComponent();
        }

        public static bool ShowNew(Window owner, Func<string, string, Task> callback)
        {
            var dialog = new WriteToDialog { _callback = callback, Owner = owner };
            dialog.ShowDialog();
            return dialog.DialogResult ?? false;
        }

        private async void OnWriteButtonClick(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;
            WindowProgressBar.Visibility = Visibility.Visible;
            await _callback(ConnectionUri, StreamName);
            DialogResult = true;
            Close();
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
