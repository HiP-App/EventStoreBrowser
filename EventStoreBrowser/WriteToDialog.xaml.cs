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

        private Func<CloneArgs, Task> _callback;

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

        public static bool ShowNew(Window owner, Func<CloneArgs, Task> callback)
        {
            var dialog = new WriteToDialog { _callback = callback, Owner = owner };
            dialog.ShowDialog();
            return dialog.DialogResult ?? false;
        }

        private async void OnWriteButtonClick(object sender, RoutedEventArgs e)
        {
            IsEnabled = false;
            WindowProgressBar.Visibility = Visibility.Visible;

            var args = new CloneArgs
            {
                TargetConnectionUri = ConnectionUri,
                TargetStreamName = StreamName,
                IncludeEventsBeforeLastSoftDelete = IncludeEventsBeforeLastSoftDeleteCheckBox.IsChecked.GetValueOrDefault(),
                IncludeMetadata = IncludeMetadataCheckBox.IsChecked.GetValueOrDefault()
            };

            await _callback(args);
            DialogResult = true;
            Close();
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class CloneArgs
    {
        public string TargetConnectionUri { get; set; }
        public string TargetStreamName { get; set; }
        public bool IncludeEventsBeforeLastSoftDelete { get; set; }
        public bool IncludeMetadata { get; set; }
    }
}
