using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using YoutubeDownloaderMVVM.Services;
using YoutubeExplode.Common;

namespace YoutubeDownloaderMVVM
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly YoutubeDownloadService _service = new();
        private readonly SettingsService _settingsService = new();
        private CancellationTokenSource? _cts;
        private YoutubeExplode.Videos.Streams.StreamManifest? _manifest;
        private YoutubeExplode.Videos.VideoId? _videoId;
        private double _lastAudioSizeMb;

        [ObservableProperty] private string _url = string.Empty;
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
        private FormatOption? _selectedFormat;
        [ObservableProperty] private string _selectedFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        [ObservableProperty] private double _progress;
        [ObservableProperty] private string _statusMessage = "Tempel URL video YouTube untuk memulai.";
        [ObservableProperty] private StatusKind _statusKind = StatusKind.Info;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(FetchInfoCommand))]
        [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
        private bool _isDownloading;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(FetchInfoCommand))]
        [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
        private bool _isFetching;

        [ObservableProperty] private bool _hasVideoInfo;
        [ObservableProperty] private string _videoTitle = string.Empty;
        [ObservableProperty] private string _videoAuthor = string.Empty;
        [ObservableProperty] private string _videoDuration = string.Empty;
        [ObservableProperty] private string _thumbnailUrl = string.Empty;

        [ObservableProperty] private bool _isDarkTheme;

        public bool IsBusy => IsDownloading || IsFetching;

        public ObservableCollection<FormatOption> FormatOptions { get; } = [];
        public ObservableCollection<DownloadHistoryItem> History { get; } = [];

        public MainViewModel()
        {
            var settings = _settingsService.Load();
            if (!string.IsNullOrWhiteSpace(settings.SelectedFolder) && Directory.Exists(settings.SelectedFolder))
            {
                SelectedFolder = settings.SelectedFolder;
            }
            IsDarkTheme = settings.IsDarkTheme;

            if (!YoutubeDownloadService.IsFfmpegAvailable())
            {
                SetStatus(
                    "ffmpeg.exe tidak ditemukan. Letakkan ffmpeg.exe di folder aplikasi atau tambahkan ke PATH sebelum mendownload.",
                    StatusKind.Warning);
            }
        }

        public void SaveSettings()
        {
            _settingsService.Save(new AppSettings
            {
                SelectedFolder = SelectedFolder,
                IsDarkTheme = IsDarkTheme
            });
        }

        partial void OnIsDownloadingChanged(bool value) => OnPropertyChanged(nameof(IsBusy));
        partial void OnIsFetchingChanged(bool value) => OnPropertyChanged(nameof(IsBusy));
        partial void OnSelectedFolderChanged(string value) => SaveSettings();
        partial void OnIsDarkThemeChanged(bool value) => SaveSettings();

        private bool CanRunWhenIdle() => !IsBusy;

        [RelayCommand]
        private void PasteUrl()
        {
            if (Clipboard.ContainsText())
            {
                Url = Clipboard.GetText().Trim();
            }
        }

        [RelayCommand(CanExecute = nameof(CanRunWhenIdle))]
        private async Task FetchInfo()
        {
            var trimmed = Url.Trim();

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                SetStatus("Masukkan URL video YouTube terlebih dahulu.", StatusKind.Warning);
                return;
            }

            if (!_service.LooksLikeYoutubeUrl(trimmed))
            {
                SetStatus("URL tidak dikenali sebagai tautan YouTube yang valid.", StatusKind.Error);
                return;
            }

            IsFetching = true;
            HasVideoInfo = false;
            FormatOptions.Clear();
            // Reset eksplisit, jangan andalkan efek samping ComboBox yang mengosongkan
            // SelectedFormat saat koleksinya di-Clear() — itu perilaku implisit WPF,
            // bukan jaminan logika. Tanpa ini, manifest/videoId video LAMA bisa tetap
            // tertaut kalau fetch video BARU gagal di tengah jalan.
            _manifest = null;
            _videoId = null;
            SelectedFormat = null;
            SetStatus("Mengambil informasi video...", StatusKind.Info);
            _cts = new CancellationTokenSource();

            try
            {
                var info = await _service.GetVideoInfoAsync(trimmed, _cts.Token);
                _manifest = info.Manifest;
                _videoId = info.VideoId;

                VideoTitle = info.Title;
                VideoAuthor = info.Author;
                VideoDuration = info.Duration;
                ThumbnailUrl = info.ThumbnailUrl;
                _lastAudioSizeMb = YoutubeDownloadService.GetHighestAudioBitrateSizeMb(info.Manifest);

                // MP4 dan WebM sama-sama dijadikan kandidat: WebM sering satu-satunya sumber
                // untuk resolusi tinggi (1440p/4K). Output tetap di-mux jadi MP4, jadi ini
                // hanya menambah pilihan kualitas, bukan mengubah format hasil unduhan.
                var videoStreams = info.Manifest.GetVideoStreams()
                    .Where(s => s.Container == YoutubeExplode.Videos.Streams.Container.Mp4
                             || s.Container == YoutubeExplode.Videos.Streams.Container.WebM)
                    .OrderByDescending(s => s.VideoQuality.MaxHeight)
                    .ThenByDescending(s => s.Container == YoutubeExplode.Videos.Streams.Container.Mp4);

                foreach (var stream in videoStreams)
                {
                    var label = $"MP4 {stream.VideoQuality.Label}";
                    if (FormatOptions.Any(f => f.DisplayName == label)) continue;

                    double sizeMb = stream.Size.MegaBytes + _lastAudioSizeMb;
                    FormatOptions.Add(new FormatOption
                    {
                        DisplayName = label,
                        Extension = "mp4",
                        IsAudioOnly = false,
                        VideoLabel = stream.VideoQuality.Label,
                        SizeLabel = $"~{sizeMb:0.#} MB"
                    });
                }

                if (info.Manifest.GetAudioStreams().Any())
                {
                    FormatOptions.Add(new FormatOption
                    {
                        DisplayName = "Audio Only (M4A)",
                        Extension = "m4a",
                        IsAudioOnly = true,
                        SizeLabel = $"~{_lastAudioSizeMb:0.#} MB"
                    });
                    FormatOptions.Add(new FormatOption
                    {
                        DisplayName = "Audio Only (MP3)",
                        Extension = "mp3",
                        IsAudioOnly = true,
                        SizeLabel = $"~{_lastAudioSizeMb:0.#} MB"
                    });
                }

                SelectedFormat = FormatOptions.FirstOrDefault(f => f.DisplayName.Contains("720")) ?? FormatOptions.FirstOrDefault();
                HasVideoInfo = true;
                DownloadCommand.NotifyCanExecuteChanged();

                SetStatus(FormatOptions.Count > 0
                    ? "Informasi video berhasil dimuat. Pilih format dan mulai download."
                    : "Video ditemukan, tetapi tidak ada format yang tersedia.",
                    FormatOptions.Count > 0 ? StatusKind.Success : StatusKind.Warning);
            }
            catch (OperationCanceledException)
            {
                SetStatus("Pengambilan informasi dibatalkan.", StatusKind.Warning);
            }
            catch (Exception)
            {
                SetStatus("Tidak dapat mengambil video ini. Pastikan URL benar dan video bersifat publik.", StatusKind.Error);
            }
            finally
            {
                IsFetching = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        [RelayCommand]
        private void BrowseFolder()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                InitialDirectory = Directory.Exists(SelectedFolder) ? SelectedFolder : null
            };
            if (dialog.ShowDialog() == true)
            {
                SelectedFolder = dialog.FolderName;
            }
        }

        [RelayCommand]
        private void OpenFolder()
        {
            if (Directory.Exists(SelectedFolder))
            {
                Process.Start(new ProcessStartInfo { FileName = SelectedFolder, UseShellExecute = true });
            }
        }

        [RelayCommand]
        private void ClearHistory()
        {
            History.Clear();
        }

        [RelayCommand]
        private void OpenHistoryItem(DownloadHistoryItem? item)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.FilePath)) return;

            if (!File.Exists(item.FilePath))
            {
                SetStatus("File dari riwayat sudah tidak ditemukan di lokasi penyimpanan.", StatusKind.Warning);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = item.FilePath,
                    UseShellExecute = true
                });
            }
            catch
            {
                SetStatus("File ditemukan, tetapi tidak dapat dibuka.", StatusKind.Error);
            }
        }

        private bool CanDownload() => !IsBusy && _manifest is not null && SelectedFormat is not null;

        [RelayCommand(CanExecute = nameof(CanDownload))]
        private async Task Download()
        {
            if (_manifest is null || _videoId is null)
            {
                SetStatus("Klik \"Ambil Info\" terlebih dahulu sebelum mendownload.", StatusKind.Warning);
                return;
            }
            if (SelectedFormat is null)
            {
                SetStatus("Pilih format terlebih dahulu.", StatusKind.Warning);
                return;
            }
            if (!Directory.Exists(SelectedFolder))
            {
                SetStatus("Folder tujuan tidak ditemukan. Pilih folder yang valid.", StatusKind.Error);
                return;
            }

            IsDownloading = true;
            Progress = 0;
            SetStatus($"Mengunduh \u2013 {SelectedFormat.DisplayName}...", StatusKind.Info);
            _cts = new CancellationTokenSource();

            string filePath = string.Empty;
            try
            {
                string fileName = YoutubeDownloadService.GenerateSafeFileName(VideoTitle, SelectedFormat.Extension, SelectedFolder);
                filePath = Path.Combine(SelectedFolder, fileName);
                var progressHandler = new Progress<double>(p => Progress = Math.Round(p * 100, 1));

                await _service.DownloadAsync(_videoId.Value, _manifest, SelectedFormat, filePath, progressHandler, _cts.Token);

                Progress = 100;
                SetStatus($"Selesai! Tersimpan sebagai \"{fileName}\".", StatusKind.Success);

                History.Insert(0, new DownloadHistoryItem
                {
                    Title = VideoTitle,
                    FormatLabel = SelectedFormat.DisplayName,
                    FilePath = filePath
                });
                while (History.Count > 6) History.RemoveAt(History.Count - 1);
            }
            catch (OperationCanceledException)
            {
                SetStatus("Download dibatalkan.", StatusKind.Warning);
                Progress = 0;
                TryDeletePartialFile(filePath);
            }
            catch (Exception ex) when (ex.Message.Contains("ffmpeg", StringComparison.OrdinalIgnoreCase))
            {
                SetStatus("ffmpeg.exe tidak ditemukan. Letakkan ffmpeg.exe di folder aplikasi lalu coba lagi.", StatusKind.Error);
                Progress = 0;
            }
            catch (Exception)
            {
                SetStatus("Terjadi kesalahan saat mendownload. Coba lagi atau pilih format lain.", StatusKind.Error);
                Progress = 0;
            }
            finally
            {
                IsDownloading = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        [RelayCommand]
        private void CancelOperation()
        {
            _cts?.Cancel();
        }

        private static void TryDeletePartialFile(string path)
        {
            try { if (!string.IsNullOrEmpty(path) && File.Exists(path)) File.Delete(path); }
            catch { /* best-effort cleanup */ }
        }

        private void SetStatus(string message, StatusKind kind)
        {
            StatusMessage = message;
            StatusKind = kind;
        }
    }

    public enum StatusKind
    {
        Info,
        Success,
        Warning,
        Error
    }
}
