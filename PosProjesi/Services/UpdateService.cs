using System.Net.Http;
using System.Text.Json;

namespace PosProjesi.Services
{
    public class UpdateInfo
    {
        public string Version { get; set; } = "";
        public string Notes { get; set; } = "";
    }

    public class UpdateService : IDisposable
    {
        public const string CurrentVersion = "1.0.0";

        private const string VersionUrl =
            "https://raw.githubusercontent.com/Sem-h/PosProgrami/main/version.json";

        private static readonly string LastCheckFile =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lastcheck.txt");

        private readonly System.Threading.Timer _timer;
        private readonly HttpClient _http;
        private bool _disposed;

        /// <summary>
        /// Fires on the UI thread when a new version is detected while the app is running.
        /// </summary>
        public event Action<UpdateInfo>? UpdateAvailable;

        private readonly SynchronizationContext? _syncContext;

        public UpdateService()
        {
            _syncContext = SynchronizationContext.Current;
            _http = new HttpClient();
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("VerimekPOS/1.0");
            _http.Timeout = TimeSpan.FromSeconds(10);

            // Check every 5 minutes (first check after 30 seconds)
            _timer = new System.Threading.Timer(
                async _ => await CheckInBackground(),
                null,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromMinutes(5));
        }

        /// <summary>
        /// One-shot check, suitable for startup. Returns UpdateInfo if update available, null otherwise.
        /// </summary>
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
            catch { /* network errors are silently ignored */ }
            return null;
        }

        /// <summary>
        /// Check if an update arrived while app was closed.
        /// Compares the last-seen remote version stored in lastcheck.txt.
        /// </summary>
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

        private async Task CheckInBackground()
        {
            try
            {
                var remote = await FetchRemoteVersionAsync();
                if (remote != null && IsNewer(remote.Version, CurrentVersion))
                {
                    SaveLastCheck(remote.Version);

                    // Fire event on UI thread
                    if (_syncContext != null)
                        _syncContext.Post(_ => UpdateAvailable?.Invoke(remote), null);
                    else
                        UpdateAvailable?.Invoke(remote);
                }
            }
            catch { /* silently ignore network errors */ }
        }

        private async Task<UpdateInfo?> FetchRemoteVersionAsync()
        {
            // Add cache-buster to avoid GitHub CDN caching
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
