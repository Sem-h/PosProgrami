using System.Net;
using System.Text;
using System.Text.Json;
using PosProjesi.DataAccess;
using PosProjesi.Models;

namespace PosProjesi.Services
{
    public class GarsonWebServer : IDisposable
    {
        private HttpListener? _listener;
        private CancellationTokenSource? _cts;
        private Thread? _thread;
        private bool _disposed;

        private readonly AyarlarRepository _ayarRepo = new();
        private readonly MasaRepository _masaRepo = new();
        private readonly UrunRepository _urunRepo = new();
        private readonly KategoriRepository _katRepo = new();

        public int Port { get; private set; } = 5555;
        public bool IsRunning => _listener?.IsListening ?? false;

        /// <summary>Fires on UI thread when a garson submits an order</summary>
        public event Action<string, string>? SiparisGeldi; // masaAdi, detay

        private readonly SynchronizationContext? _syncContext;

        public GarsonWebServer()
        {
            _syncContext = SynchronizationContext.Current;
        }

        public void Start(int port = 5555)
        {
            Stop();
            Port = port;
            _cts = new CancellationTokenSource();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://+:{port}/");
            _listener.Start();

            _thread = new Thread(() => ListenLoop(_cts.Token))
            {
                IsBackground = true,
                Name = "GarsonWebServer"
            };
            _thread.Start();
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Close();
            _listener = null;
        }

        private void ListenLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var ctx = _listener!.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => HandleRequest(ctx));
                }
                catch (HttpListenerException) when (ct.IsCancellationRequested) { break; }
                catch (ObjectDisposedException) { break; }
                catch { }
            }
        }

        private void HandleRequest(HttpListenerContext ctx)
        {
            try
            {
                var path = ctx.Request.Url?.AbsolutePath ?? "/";
                var method = ctx.Request.HttpMethod;

                // CORS headers
                ctx.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                ctx.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                ctx.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

                if (method == "OPTIONS")
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.Close();
                    return;
                }

                if (path == "/" || path == "/index.html")
                    ServeHtml(ctx);
                else if (path == "/api/pin-check" && method == "POST")
                    HandlePinCheck(ctx);
                else if (path == "/api/masalar")
                    HandleMasalar(ctx);
                else if (path == "/api/kategoriler")
                    HandleKategoriler(ctx);
                else if (path == "/api/urunler")
                    HandleUrunler(ctx);
                else if (path == "/api/siparis" && method == "POST")
                    HandleSiparis(ctx);
                else
                {
                    ctx.Response.StatusCode = 404;
                    WriteJson(ctx, new { error = "Not Found" });
                }
            }
            catch (Exception ex)
            {
                try
                {
                    ctx.Response.StatusCode = 500;
                    WriteJson(ctx, new { error = ex.Message });
                }
                catch { }
            }
        }

        private void HandlePinCheck(HttpListenerContext ctx)
        {
            var body = ReadBody(ctx);
            var pin = JsonSerializer.Deserialize<JsonElement>(body).GetProperty("pin").GetString() ?? "";
            var correctPin = _ayarRepo.GetAyar(AyarlarRepository.GarsonPin, "1234");

            if (pin == correctPin)
                WriteJson(ctx, new { success = true });
            else
            {
                ctx.Response.StatusCode = 401;
                WriteJson(ctx, new { success = false, error = "Geçersiz PIN" });
            }
        }

        private void HandleMasalar(HttpListenerContext ctx)
        {
            var masalar = _masaRepo.GetAll().Select(m => new
            {
                m.Id,
                m.Ad,
                Kategori = m.MasaKategoriAdi ?? "",
                m.Durum,
                SepetVarMi = !string.IsNullOrEmpty(m.AktifSepet)
            });
            WriteJson(ctx, masalar);
        }

        private void HandleKategoriler(HttpListenerContext ctx)
        {
            var kategoriler = _katRepo.GetAll().Select(k => new { k.Id, k.Ad });
            WriteJson(ctx, kategoriler);
        }

        private void HandleUrunler(HttpListenerContext ctx)
        {
            var katIdStr = ctx.Request.QueryString["kategoriId"];
            List<Urun> urunler;
            if (int.TryParse(katIdStr, out int katId))
                urunler = _urunRepo.GetByKategori(katId);
            else
                urunler = _urunRepo.GetAll();

            var result = urunler.Select(u => new
            {
                u.Id,
                u.Ad,
                Fiyat = u.SatisFiyati,
                Kategori = u.KategoriAdi ?? "",
                u.Stok
            });
            WriteJson(ctx, result);
        }

        private void HandleSiparis(HttpListenerContext ctx)
        {
            var body = ReadBody(ctx);
            var doc = JsonSerializer.Deserialize<JsonElement>(body);

            var masaId = doc.GetProperty("masaId").GetInt32();
            var itemsEl = doc.GetProperty("items");

            // Build sepet items for the table
            var sepetItems = new List<object>();
            var detayParts = new List<string>();

            foreach (var item in itemsEl.EnumerateArray())
            {
                var urunId = item.GetProperty("urunId").GetInt32();
                var miktar = item.GetProperty("miktar").GetInt32();
                var urun = _urunRepo.GetAll().FirstOrDefault(u => u.Id == urunId);
                if (urun == null) continue;

                sepetItems.Add(new
                {
                    UrunId = urun.Id,
                    UrunAdi = urun.Ad,
                    Miktar = miktar,
                    BirimFiyat = urun.SatisFiyati,
                    ToplamFiyat = urun.SatisFiyati * miktar
                });
                detayParts.Add($"{urun.Ad} x{miktar}");
            }

            if (sepetItems.Count == 0)
            {
                ctx.Response.StatusCode = 400;
                WriteJson(ctx, new { error = "Sipariş boş" });
                return;
            }

            // Get existing sepet and merge
            var masa = _masaRepo.GetById(masaId);
            if (masa == null)
            {
                ctx.Response.StatusCode = 404;
                WriteJson(ctx, new { error = "Masa bulunamadı" });
                return;
            }

            var existingItems = new List<object>();
            if (!string.IsNullOrEmpty(masa.AktifSepet))
            {
                try
                {
                    var existing = JsonSerializer.Deserialize<List<JsonElement>>(masa.AktifSepet);
                    if (existing != null)
                    {
                        foreach (var e in existing)
                            existingItems.Add(e);
                    }
                }
                catch { }
            }

            // Append new items
            foreach (var si in sepetItems)
                existingItems.Add(si);

            var sepetJson = JsonSerializer.Serialize(existingItems);
            _masaRepo.SaveSepet(masaId, sepetJson);

            // Fire event on UI thread
            var masaAdi = masa.MasaKategoriAdi != null
                ? $"{masa.MasaKategoriAdi} - {masa.Ad}"
                : masa.Ad;
            var detay = string.Join(", ", detayParts);

            if (_syncContext != null)
                _syncContext.Post(_ => SiparisGeldi?.Invoke(masaAdi, detay), null);
            else
                SiparisGeldi?.Invoke(masaAdi, detay);

            WriteJson(ctx, new { success = true, message = $"Sipariş {masaAdi} masasına eklendi" });
        }

        private void ServeHtml(HttpListenerContext ctx)
        {
            var html = GetGarsonHtml();
            var bytes = Encoding.UTF8.GetBytes(html);
            ctx.Response.ContentType = "text/html; charset=utf-8";
            ctx.Response.ContentLength64 = bytes.Length;
            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
            ctx.Response.Close();
        }

        private void WriteJson(HttpListenerContext ctx, object data)
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
            var bytes = Encoding.UTF8.GetBytes(json);
            ctx.Response.ContentType = "application/json; charset=utf-8";
            ctx.Response.ContentLength64 = bytes.Length;
            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
            ctx.Response.Close();
        }

        private string ReadBody(HttpListenerContext ctx)
        {
            using var reader = new System.IO.StreamReader(ctx.Request.InputStream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        public static string GetLocalIP()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                return host.AddressList
                    .FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?
                    .ToString() ?? "localhost";
            }
            catch { return "localhost"; }
        }

        private string GetGarsonHtml()
        {
            var isletmeAdi = _ayarRepo.GetAyar(AyarlarRepository.IsletmeAdi, "Verimek POS");
            return GarsonHtmlTemplate.Get(isletmeAdi);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
        }
    }
}
