using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace YoutubeDownloaderMVVM
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel = new();

        public MainWindow()
        {
            InitializeComponent();

            DataContext = _viewModel;
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            // Apply both the Material Design base theme and the app-specific palette.
            ApplyTheme(_viewModel.IsDarkTheme);

            Closing += (_, _) =>
            {
                _viewModel.SaveSettings();
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            };
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsDarkTheme))
                ApplyTheme(_viewModel.IsDarkTheme);
        }

        private void ApplyTheme(bool isDark)
        {
            var bundledTheme = Resources.MergedDictionaries
                .OfType<BundledTheme>()
                .FirstOrDefault();

            if (bundledTheme is not null)
                bundledTheme.BaseTheme = isDark ? BaseTheme.Dark : BaseTheme.Light;

            // The original UI used StaticResource for these brushes, which meant
            // changing BundledTheme did not repaint the application's own surfaces.
            // DynamicResource + replacing the brushes fixes that theme-mode bug.
            SetBrush("AppBackgroundBrush", isDark ? "#111113" : "#F7F7F8");
            SetBrush("CardBrush", isDark ? "#1B1B1E" : "#FFFFFF");
            SetBrush("CardSubtleBrush", isDark ? "#242427" : "#FAFAFA");
            SetBrush("AccentBrush", isDark ? "#FF5A5A" : "#FF0000");
            SetBrush("AccentDarkBrush", isDark ? "#FF3B3B" : "#D90000");
            SetBrush("TextPrimaryBrush", isDark ? "#F4F4F5" : "#18181B");
            SetBrush("TextSecondaryBrush", isDark ? "#A1A1AA" : "#71717A");
            SetBrush("BorderSoftBrush", isDark ? "#303034" : "#E7E7EA");
            SetBrush("InputBrush", isDark ? "#202023" : "#FFFFFF");
            SetBrush("ThumbnailBackgroundBrush", isDark ? "#27272A" : "#F1F1F2");
            SetBrush("AccentSoftBrush", isDark ? "#3A2020" : "#FFF0F0");
            SetBrush("TextTertiaryBrush", isDark ? "#71717A" : "#A1A1AA");
        }

        private void SetBrush(string key, string hex)
        {
            if (new BrushConverter().ConvertFromString(hex) is SolidColorBrush brush)
            {
                brush.Freeze();
                Resources[key] = brush;
            }
        }
    }
}
