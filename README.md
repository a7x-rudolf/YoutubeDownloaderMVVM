# YouTube Downloader (WPF · MVVM)

Aplikasi desktop Windows untuk mengunduh video YouTube — pilih kualitas, pantau progres, dan simpan riwayat download — dibangun dengan WPF + pola MVVM.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6)
![License](https://img.shields.io/badge/license-MIT-green)

## Fitur

- Tempel URL video YouTube → ambil info (judul, channel, durasi, thumbnail)
- Pilih kualitas: MP4 (berbagai resolusi termasuk sumber WebM yang di-mux ulang), Audio Only (M4A / MP3)
- Estimasi ukuran file sebelum download
- Progress bar real-time + tombol batalkan
- Riwayat download selama sesi aplikasi berjalan, dengan akses cepat buka file
- Preferensi folder tujuan & tema (light/dark) tersimpan otomatis antar sesi
- Deteksi ketersediaan `ffmpeg` di awal, bukan setelah proses gagal

## Tech Stack

| Komponen | Library |
|---|---|
| UI Framework | WPF (.NET 8) |
| MVVM | [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) |
| Desain UI | [MaterialDesignThemes](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit) |
| Ekstraksi & download YouTube | [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) + `YoutubeExplode.Converter` |
| Eksekusi proses eksternal (ffmpeg) | [CliWrap](https://github.com/Tyrrrz/CliWrap) |

## Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (untuk build dari source)
- `ffmpeg.exe` — **tidak disertakan di repo ini**, unduh sendiri build "essentials" dari [gyan.dev/ffmpeg/builds](https://www.gyan.dev/ffmpeg/builds/) dan taruh di folder yang sama dengan `YoutubeDownloader.exe`, atau tambahkan ke PATH sistem

## Menjalankan dari source

```powershell
git clone https://github.com/a7x-rudolf/YoutubeDownloaderMVVM.git
cd YoutubeDownloaderMVVM
dotnet restore
dotnet run --project YoutubeDownloader.csproj
```

## Build ke .exe (self-contained, tanpa perlu install .NET Runtime terpisah)

```powershell
dotnet publish YoutubeDownloader.csproj -c Release -p:PublishProfile=FolderProfile
```

Hasil publish ada di `bin\Publish\win-x64\YoutubeDownloader.exe`. Jangan lupa taruh `ffmpeg.exe` di folder yang sama sebelum menjalankan.

## Struktur Proyek

```
YoutubeDownloader.csproj
MainWindow.xaml / .xaml.cs      # View
MainViewModel.cs                # ViewModel (state, commands)
Models/FormatOption.cs          # Model opsi format & riwayat download
Services/
  YoutubeDownloadService.cs     # Wrapper YoutubeExplode
  SettingsService.cs            # Persist preferensi ke %AppData%
Converters/Converters.cs        # Value converters untuk binding XAML
```

## Catatan

Aplikasi ini mengakses YouTube lewat *reverse-engineered client* (bukan API resmi YouTube), sehingga sewaktu-waktu bisa berhenti bekerja jika YouTube mengubah struktur internal mereka. Gunakan sesuai [Ketentuan Layanan YouTube](https://www.youtube.com/t/terms) dan hak cipta konten yang diunduh.

## Lisensi

MIT — lihat file [LICENSE](LICENSE).
