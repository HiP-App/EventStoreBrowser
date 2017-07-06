using System.Windows;

namespace EventStoreBrowser
{
    public partial class MetadataWindow : Window
    {
        public static readonly DependencyProperty MetadataJsonStringProperty =
            DependencyProperty.Register(nameof(MetadataJsonString), typeof(string), typeof(MetadataWindow), new PropertyMetadata(""));

        public string MetadataJsonString
        {
            get => (string)GetValue(MetadataJsonStringProperty);
            set => SetValue(MetadataJsonStringProperty, value);
        }

        public MetadataWindow()
        {
            InitializeComponent();
        }

        private void OnApplyButtonClick(object sender, RoutedEventArgs e)
        {
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
