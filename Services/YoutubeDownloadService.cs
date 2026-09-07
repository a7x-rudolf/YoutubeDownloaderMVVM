using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace YoutubeDownloaderMVVM.Services
{
    /// <summary>
    /// Hasil ringkas informasi video, dipisah dari model YoutubeExplode
    /// supaya ViewModel tidak bergantung langsung ke library eksternal.
    /// </summary>
    public sealed class VideoInfoResult
    {
        public required VideoId VideoId { get; init; }
        public required string Title { get; init; }
        public required string Author { get; init; }
        public required string Duration { get; init; }
        public required string ThumbnailUrl { get; init; }
        public required StreamManifest Manifest { get; init; }
    }

    /// <summary>
    /// Membungkus semua interaksi dengan YoutubeExplode agar ViewModel
    /// hanya bicara dengan abstraksi sederhana ini (testable & terpisah dari UI).
    /// </summary>
    public sealed class YoutubeDownloadService
    {
        private readonly YoutubeClient _client = new();

        /// <summary>
        /// Cek apakah ffmpeg.exe bisa ditemukan (folder aplikasi atau PATH sistem).
        /// Dipanggil di awal supaya user tahu sebelum mulai fetch/download, bukan setelah gagal.
        /// </summary>
        public static bool IsFfmpegAvailable()
        {
            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
            if (File.Exists(localPath)) return true;

            string pathVar = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (var dir in pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    if (File.Exists(Path.Combine(dir, "ffmpeg.exe"))) return true;
                }
                catch
                {
                    // Entri PATH tidak valid -> abaikan dan lanjut cek entri berikutnya.
                }
            }
            return false;
        }

        public bool LooksLikeYoutubeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return System.Text.RegularExpressions.Regex.IsMatch(
                url.Trim(),
                @"^(https?:\/\/)?(www\.|m\.)?(youtube\.com\/(watch\?v=|shorts\/|embed\/)|youtu\.be\/)[\w\-]+",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        public async Task<VideoInfoResult> GetVideoInfoAsync(string url, CancellationToken ct)
        {
            var video = await _client.Videos.GetAsync(url, ct);
            var manifest = await _client.Videos.Streams.GetManifestAsync(video.Id, ct);

            return new VideoInfoResult
            {
                VideoId = video.Id,
                Title = video.Title,
                Author = video.Author.ChannelTitle,
                Duration = video.Duration?.ToString(@"hh\:mm\:ss") ?? "Live",
                ThumbnailUrl = video.Thumbnails.GetWithHighestResolution()?.Url ?? string.Empty,
                Manifest = manifest
            };
        }

        public static double GetHighestAudioBitrateSizeMb(StreamManifest manifest)
        {
            var audio = manifest.GetAudioStreams().GetWithHighestBitrate();
            return audio is null ? 0 : audio.Size.MegaBytes;
        }

        public async Task DownloadAsync(
            VideoId videoId,
            StreamManifest manifest,
            FormatOption format,
            string filePath,
            IProgress<double> progress,
            CancellationToken ct)
        {
            if (format.IsAudioOnly)
            {
                var audioStream = manifest.GetAudioStreams().GetWithHighestBitrate();
                var request = new ConversionRequestBuilder(filePath).Build();
                await _client.Videos.DownloadAsync(new IStreamInfo[] { audioStream }, request, progress, ct);
            }
            else
            {
                // Sumber video boleh MP4 atau WebM (WebM sering satu-satunya sumber untuk
                // resolusi tinggi seperti 1440p/4K) — output tetap di-mux ke ekstensi yang
                // diminta lewat ConversionRequestBuilder, jadi container sumber tidak masalah.
                var videoStream = manifest.GetVideoStreams()
                    .Where(s => s.Container == Container.Mp4 || s.Container == Container.WebM)
                    .Where(s => s.VideoQuality.Label == format.VideoLabel)
                    .OrderByDescending(s => s.Container == Container.Mp4) // prefer MP4 kalau labelnya sama-sama tersedia
                    .FirstOrDefault();

                if (videoStream is null)
                    throw new InvalidOperationException($"Resolusi {format.VideoLabel} tidak lagi tersedia untuk video ini.");

                var audioStream = manifest.GetAudioStreams().GetWithHighestBitrate();
                var request = new ConversionRequestBuilder(filePath).Build();
                await _client.Videos.DownloadAsync(new IStreamInfo[] { videoStream, audioStream }, request, progress, ct);
            }
        }

        public static string GenerateSafeFileName(string title, string ext, string folderPath)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                title = title.Replace(c, '_');

            title = title.Trim();
            if (title.Length > 120) title = title[..120].TrimEnd();

            string fileName = $"{title}.{ext}";
            string filePath = Path.Combine(folderPath, fileName);

            int counter = 1;
            while (File.Exists(filePath))
            {
                fileName = $"{title} ({counter}).{ext}";
                filePath = Path.Combine(folderPath, fileName);
                counter++;
            }

            return fileName;
        }
    }
}
