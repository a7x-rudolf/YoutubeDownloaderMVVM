using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Threading;

namespace YoutubeDownloaderMVVM
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Tanpa handler ini, exception yang lolos dari try/catch di ViewModel
            // (mis. error tak terduga dari library eksternal) akan menutup aplikasi
            // tanpa pesan sama sekali.
            DispatcherUnhandledException += OnDispatcherUnhandledException;
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                $"Terjadi kesalahan yang tidak terduga:\n\n{e.Exception.Message}\n\nAplikasi akan tetap berjalan.",
                "Kesalahan Tak Terduga",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }
    }

}
