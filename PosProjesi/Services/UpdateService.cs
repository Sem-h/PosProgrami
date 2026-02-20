using System.Net.Http;
using System.Text.Json;
using System.IO.Compression;
using System.Diagnostics;

namespace PosProjesi.Services
{
    public class UpdateInfo
    {
        public string Version { get; set; } = "";
        public string Notes { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
    }

    public class UpdateService : IDisposable
    {
        public const string CurrentVersion = "1.0.0";

        private const string VersionUrl =
            "https://raw.githubusercontent.com/Sem-h/PosProgrami/main/version.json";

        private static readonly string AppDir = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string LastCheckFile = Path.Combine(AppDir, "lastcheck.txt");
        private static readonly string UpdateDir = Path.Combine(AppDir, "_update");

        private readonly System.Threading.Timer _timer;
        private readonly HttpClient _http;
        private bool _disposed;

        public event Action<UpdateInfo>? UpdateAvailable;

        private readonly SynchronizationContext? _syncContext;

        public UpdateService()
        {
            _syncContext = SynchronizationContext.Current;
            _http = new HttpClient();
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("VerimekPOS/1.0");
            _http.Timeout = TimeSpan.FromSeconds(30);

            _timer = new System.Threading.Timer(
                async _ => await CheckInBackground(),
                null,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromMinutes(5));
        }

        public async Task<UpdateInfo?> CheckOnceAsync()
        {
            try
            {
                var remote = await FetchRemoteVersionAsync();
                if (remote != null && IsNewer(remote.Version, CurrentVersion))
                {
                    SaveLastCheck(remote.Version);
                    return remote;
                }
            }
            catch { }
            return null;
        }

        public static bool HasPendingUpdate(out string remoteVersion)
        {
            remoteVersion = "";
            try
            {
                if (!File.Exists(LastCheckFile)) return false;
                remoteVersion = File.ReadAllText(LastCheckFile).Trim();
                return IsNewer(remoteVersion, CurrentVersion);
            }
            catch { return false; }
        }

        /// <summary>
        /// Download update ZIP, extract, and launch updater script that replaces files and restarts.
        /// </summary>
        public async Task<bool> DownloadAndApplyAsync(UpdateInfo info, Action<int>? onProgress = null)
        {
            try
            {
                if (string.IsNullOrEmpty(info.DownloadUrl)) return false;

                // Clean up previous update folder
                if (Directory.Exists(UpdateDir))
                    Directory.Delete(UpdateDir, true);
                Directory.CreateDirectory(UpdateDir);

                var zipPath = Path.Combine(UpdateDir, "update.zip");
                var extractDir = Path.Combine(UpdateDir, "files");

                // Download ZIP with progress
                using var response = await _http.GetAsync(info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                long downloaded = 0;

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var buffer = new byte[81920];
                    int bytesRead;
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        downloaded += bytesRead;
                        if (totalBytes > 0)
                            onProgress?.Invoke((int)(downloaded * 100 / totalBytes));
                    }
                }

                onProgress?.Invoke(100);

                // Extract ZIP
                ZipFile.ExtractToDirectory(zipPath, extractDir, true);

                // Create updater batch script
                var batPath = Path.Combine(UpdateDir, "update.bat");
                var exeName = Path.GetFileName(Environment.ProcessPath ?? "PosProjesi.exe");
                var script = $@"@echo off
chcp 65001 >nul
echo Güncelleme uygulanıyor...
echo Uygulama kapanması bekleniyor...
timeout /t 2 /nobreak >nul

:waitloop
tasklist /FI ""IMAGENAME eq {exeName}"" 2>NUL | find /I ""{exeName}"" >NUL
if not errorlevel 1 (
    timeout /t 1 /nobreak >nul
    goto waitloop
)

echo Dosyalar kopyalanıyor...
xcopy ""{extractDir}\*"" ""{AppDir}"" /E /Y /Q >nul 2>nul

echo Uygulama yeniden başlatılıyor...
start """" ""{Path.Combine(AppDir, exeName)}""

echo Temizlik yapılıyor...
timeout /t 2 /nobreak >nul
rmdir /S /Q ""{UpdateDir}"" 2>nul
exit
";
                File.WriteAllText(batPath, script, System.Text.Encoding.UTF8);

                // Launch updater and close app
                var psi = new ProcessStartInfo
                {
                    FileName = batPath,
                    CreateNoWindow = true,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process.Start(psi);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task CheckInBackground()
        {
            try
            {
                var remote = await FetchRemoteVersionAsync();
                if (remote != null && IsNewer(remote.Version, CurrentVersion))
                {
                    SaveLastCheck(remote.Version);

                    if (_syncContext != null)
                        _syncContext.Post(_ => UpdateAvailable?.Invoke(remote), null);
                    else
                        UpdateAvailable?.Invoke(remote);
                }
            }
            catch { }
        }

        private async Task<UpdateInfo?> FetchRemoteVersionAsync()
        {
            var url = VersionUrl + "?t=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var json = await _http.GetStringAsync(url);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<UpdateInfo>(json, options);
        }

        private static bool IsNewer(string remote, string local)
        {
            if (Version.TryParse(remote, out var r) && Version.TryParse(local, out var l))
                return r > l;
            return false;
        }

        private static void SaveLastCheck(string version)
        {
            try { File.WriteAllText(LastCheckFile, version); } catch { }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _timer.Dispose();
            _http.Dispose();
        }
    }
}
