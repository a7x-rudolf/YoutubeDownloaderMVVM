using System;
using System.IO;

namespace YoutubeDownloaderMVVM
{
    /// <summary>
    /// Merepresentasikan satu opsi format download (mis. "MP4 1080p" atau "Audio Only (MP3)").
    /// </summary>
    public class FormatOption
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public bool IsAudioOnly { get; set; }
        public string? VideoLabel { get; set; }
        public string SizeLabel { get; set; } = string.Empty;
        public string IconKind => IsAudioOnly ? "MusicNote" : "VideoOutline";

        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// Satu entri riwayat download yang berhasil, ditampilkan di panel History.
    /// </summary>
    public class DownloadHistoryItem
    {
        public string Title { get; set; } = string.Empty;
        public string FormatLabel { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; } = DateTime.Now;
        public string TimeLabel => CompletedAt.Date == DateTime.Today
            ? $"Hari ini • {CompletedAt:HH:mm}"
            : CompletedAt.ToString("dd MMM • HH:mm");

        public string FileName => Path.GetFileName(FilePath);
    }
}
