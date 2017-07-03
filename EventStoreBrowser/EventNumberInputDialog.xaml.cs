using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EventStoreBrowser
{
    public partial class EventNumberInputDialog : Window
    {
        public static readonly DependencyProperty CurrentBeginningOfStreamProperty =
            DependencyProperty.Register(nameof(CurrentBeginningOfStream), typeof(int), typeof(EventNumberInputDialog), new PropertyMetadata(0));

        public static readonly DependencyProperty SelectedBeginningOfStreamProperty =
            DependencyProperty.Register(nameof(SelectedBeginningOfStream), typeof(int), typeof(EventNumberInputDialog), new PropertyMetadata(0));

        public static readonly DependencyProperty EndOfStreamProperty =
            DependencyProperty.Register(nameof(EndOfStream), typeof(int), typeof(EventNumberInputDialog), new PropertyMetadata(0));

        public int CurrentBeginningOfStream
        {
            get => (int)GetValue(CurrentBeginningOfStreamProperty);
            set => SetValue(CurrentBeginningOfStreamProperty, value);
        }

        public int SelectedBeginningOfStream
        {
            get => (int)GetValue(SelectedBeginningOfStreamProperty);
            set => SetValue(SelectedBeginningOfStreamProperty, value);
        }

        public int EndOfStream
        {
            get => (int)GetValue(EndOfStreamProperty);
            set => SetValue(EndOfStreamProperty, value);
        }

        public EventNumberInputDialog()
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

    class PlusOneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            (value is int i) ? i + 1 : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
