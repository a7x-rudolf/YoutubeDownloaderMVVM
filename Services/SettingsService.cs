using System;
using System.IO;
using System.Text.Json;

namespace YoutubeDownloaderMVVM.Services
{
    /// <summary>
    /// Preferensi yang disimpan antar sesi aplikasi (folder tujuan, tema).
    /// </summary>
    public sealed class AppSettings
    {
        public string? SelectedFolder { get; set; }
        public bool IsDarkTheme { get; set; }
    }

    /// <summary>
    /// Baca/tulis <see cref="AppSettings"/> sebagai JSON di %AppData%.
    /// Gagal baca/tulis tidak boleh menjatuhkan aplikasi — settings bersifat best-effort.
    /// </summary>
    public sealed class SettingsService
    {
        private readonly string _filePath;

        public SettingsService()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "YoutubeDownloaderMVVM");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "settings.json");
        }

        public AppSettings Load()
        {
            try
            {
                if (!File.Exists(_filePath)) return new AppSettings();
                string json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                // File korup/tidak terbaca -> jangan crash, pakai default.
                return new AppSettings();
            }
        }

        public void Save(AppSettings settings)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch
            {
                // Best-effort: gagal simpan tidak boleh mengganggu alur aplikasi.
            }
        }
    }
}
