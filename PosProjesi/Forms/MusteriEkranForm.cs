using System.Drawing.Drawing2D;
using System.Drawing.Text;
using PosProjesi.Models;
using PosProjesi.UI;

namespace PosProjesi.Forms
{
    public class MusteriEkranForm : Form
    {
        private readonly List<SatisDetay> _items = new();
        private string _sonUrunAdi = "";
        private decimal _sonUrunFiyat = 0;
        private decimal _toplam = 0;
        private string _durum = "welcome";
        private Image? _sonUrunResim = null;

        private System.Windows.Forms.Timer? _resetTimer;
        private System.Windows.Forms.Timer? _clockTimer;

        // Corporate colors
        private static readonly Color BgMain      = Color.FromArgb(13, 17, 23);
        private static readonly Color BgHeader    = Color.FromArgb(22, 27, 34);
        private static readonly Color BgFooter    = Color.FromArgb(22, 27, 34);
        private static readonly Color BgRowEven   = Color.FromArgb(22, 27, 34);
        private static readonly Color BgRowOdd    = Color.FromArgb(17, 21, 28);
        private static readonly Color BgHighlight = Color.FromArgb(28, 35, 48);
        private static readonly Color Accent      = Color.FromArgb(58, 130, 220);
        private static readonly Color Green       = Color.FromArgb(46, 160, 80);
        private static readonly Color White       = Color.FromArgb(230, 237, 243);
        private static readonly Color Gray        = Color.FromArgb(125, 133, 144);
        private static readonly Color DimGray     = Color.FromArgb(80, 88, 100);
        private static readonly Color LineDark    = Color.FromArgb(48, 54, 61);

        public MusteriEkranForm()
        {
            this.Text = "Müşteri Ekranı";
            this.BackColor = BgMain;
            this.ForeColor = White;
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = false;

            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer, true);
            this.DoubleBuffered = true;

            this.Paint += OnPaint;
            this.Resize += (s, e) => this.Invalidate();
            this.KeyPreview = true;
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) this.Close(); };

            _clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _clockTimer.Tick += (s, e) => { if (_durum == "welcome") this.Invalidate(); };
            _clockTimer.Start();

            OpenOnSecondScreen();
        }

        private void OpenOnSecondScreen()
        {
            var screens = Screen.AllScreens;
            if (screens.Length > 1)
            {
                var sec = screens.FirstOrDefault(s => !s.Primary) ?? screens[0];
                this.StartPosition = FormStartPosition.Manual;
                this.Bounds = sec.Bounds;
                this.WindowState = FormWindowState.Maximized;
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = FormWindowState.Normal;
                this.Size = new Size(720, 520);
                this.StartPosition = FormStartPosition.CenterScreen;
                this.TopMost = false;
                this.ShowInTaskbar = true;
            }
        }

        // ─── PUBLIC API ───────────────────────────

        public void SepetGuncelle(List<SatisDetay> items, decimal toplam)
        {
            _items.Clear(); _items.AddRange(items);
            _toplam = toplam;
            _durum = items.Count > 0 ? "scanning" : "welcome";
            SafeRefresh();
        }

        public void UrunEklendi(string urunAdi, decimal fiyat, int miktar, string? resimYolu = null)
        {
            _sonUrunAdi = urunAdi;
            _sonUrunFiyat = fiyat * miktar;
            LoadUrunResim(resimYolu);
            SafeRefresh();
        }

        private void LoadUrunResim(string? resimYolu)
        {
            _sonUrunResim?.Dispose();
            _sonUrunResim = null;

            if (!string.IsNullOrEmpty(resimYolu))
            {
                var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, resimYolu);
                if (File.Exists(fullPath))
                {
                    try
                    {
                        using var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                        _sonUrunResim = Image.FromStream(fs);
                    }
                    catch { }
                }
            }
        }

        public void SatisTamamlandi(decimal toplam)
        {
            _durum = "paid"; _toplam = toplam;
            _sonUrunAdi = ""; _items.Clear();
            SafeRefresh();

            _resetTimer?.Stop(); _resetTimer?.Dispose();
            _resetTimer = new System.Windows.Forms.Timer { Interval = 4000 };
            _resetTimer.Tick += (s, e) =>
            { _resetTimer!.Stop(); _durum = "welcome"; _toplam = 0; SafeRefresh(); };
            _resetTimer.Start();
        }

        private void SafeRefresh()
        {
            if (this.InvokeRequired) this.Invoke(this.Invalidate);
            else this.Invalidate();
        }

        // ─── PAINT ────────────────────────────────

        private void OnPaint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int W = this.ClientSize.Width;
            int H = this.ClientSize.Height;

            // Background
            using (var bg = new SolidBrush(BgMain))
                g.FillRectangle(bg, 0, 0, W, H);

            // ─── HEADER ───
            using var logoFont = new Font("Segoe UI", 28, FontStyle.Bold);
            using var subFont = new Font("Segoe UI", 11);
            var logoSize = MeasureText(g, "Verimek", logoFont);
            string subText = _durum == "paid" ? "Teşekkür Ederiz" : "POS Satış Sistemi";
            var subSize = MeasureText(g, subText, subFont);

            // Calculate header height dynamically: padding + logo + gap + subtitle + padding
            int headerPad = 18;
            int gap = 4;
            int headerH = headerPad + logoSize.Height + gap + subSize.Height + headerPad;

            using (var hBg = new SolidBrush(BgHeader))
                g.FillRectangle(hBg, 0, 0, W, headerH);

            // Logo — centered
            int logoY = headerPad;
            DrawText(g, "Verimek", logoFont, Accent, (W - logoSize.Width) / 2, logoY);

            // Subtitle — centered, below logo
            int subY = logoY + logoSize.Height + gap;
            DrawText(g, subText, subFont, DimGray, (W - subSize.Width) / 2, subY);

            // Header bottom line
            using (var pen = new Pen(LineDark))
                g.DrawLine(pen, 0, headerH - 1, W, headerH - 1);
            using (var accentPen = new Pen(Accent, 2))
                g.DrawLine(accentPen, W / 4, headerH - 1, W * 3 / 4, headerH - 1);

            // Content area
            var content = new Rectangle(0, headerH, W, H - headerH);

            switch (_durum)
            {
                case "welcome": PaintWelcome(g, content); break;
                case "paid":    PaintPaid(g, content); break;
                default:        PaintScanning(g, content); break;
            }
        }

        // ─── WELCOME ─────────────────────────────

        private void PaintWelcome(Graphics g, Rectangle r)
        {
            int cx = r.X + r.Width / 2;
            int cy = r.Y + r.Height / 2;

            // Clock — large, centered
            using var clockFont = new Font("Segoe UI", 64, FontStyle.Bold);
            var clockText = DateTime.Now.ToString("HH:mm");
            var clockSize = MeasureText(g, clockText, clockFont);
            int clockX = (r.Width - clockSize.Width) / 2;
            int clockY = cy - clockSize.Height / 2 - 30;
            DrawText(g, clockText, clockFont, White, clockX, clockY);

            // Date — below clock with clear gap
            using var dateFont = new Font("Segoe UI", 15);
            var dateText = DateTime.Now.ToString("dd MMMM yyyy, dddd");
            var dateSize = MeasureText(g, dateText, dateFont);
            int dateX = (r.Width - dateSize.Width) / 2;
            int dateY = clockY + clockSize.Height + 16;
            DrawText(g, dateText, dateFont, Gray, dateX, dateY);

            // Bottom prompt
            using var promptFont = new Font("Segoe UI", 11);
            var promptText = "Ürünlerinizi kasiyere veriniz";
            var promptSize = MeasureText(g, promptText, promptFont);
            DrawText(g, promptText, promptFont, DimGray,
                (r.Width - promptSize.Width) / 2, r.Bottom - 50);
        }

        // ─── PAID ─────────────────────────────────

        private void PaintPaid(Graphics g, Rectangle r)
        {
            int cx = r.X + r.Width / 2;
            int cy = r.Y + r.Height / 2;

            // Check circle
            int cr = 38;
            using (var pen = new Pen(Green, 3))
                g.DrawEllipse(pen, cx - cr, cy - 95, cr * 2, cr * 2);
            using var checkFont = new Font("Segoe UI", 32, FontStyle.Bold);
            var checkSize = MeasureText(g, "✓", checkFont);
            DrawText(g, "✓", checkFont, Green, cx - checkSize.Width / 2, cy - 92);

            // Message
            using var msgFont = new Font("Segoe UI", 22, FontStyle.Bold);
            var msgSize = MeasureText(g, "Ödeme Başarılı", msgFont);
            DrawText(g, "Ödeme Başarılı", msgFont, Green,
                cx - msgSize.Width / 2, cy - 5);

            // Total
            using var totalFont = new Font("Segoe UI", 34, FontStyle.Bold);
            var totalText = $"₺{_toplam:N2}";
            var totalSize = MeasureText(g, totalText, totalFont);
            DrawText(g, totalText, totalFont, White,
                cx - totalSize.Width / 2, cy + msgSize.Height + 10);
        }

        // ─── SCANNING ─────────────────────────────

        private void PaintScanning(Graphics g, Rectangle r)
        {
            int pad = 30;
            int W = r.Width;
            int y = r.Y + 12;
            int footerH = 80;

            // ── Last scanned item ──
            if (!string.IsNullOrEmpty(_sonUrunAdi))
            {
                int imgSize = _sonUrunResim != null ? 80 : 0;
                int barH = imgSize > 0 ? Math.Max(48, imgSize + 12) : 48;

                using (var barBg = new SolidBrush(BgHighlight))
                    g.FillRectangle(barBg, pad, y, W - pad * 2, barH);
                using (var accentBr = new SolidBrush(Green))
                    g.FillRectangle(accentBr, pad, y, 3, barH);

                int textX = pad + 14;

                // Draw product image if available
                if (_sonUrunResim != null)
                {
                    int imgPad = 6;
                    int imgX = pad + imgPad;
                    int imgY = y + (barH - imgSize) / 2;
                    double ratio = Math.Min((double)imgSize / _sonUrunResim.Width, (double)imgSize / _sonUrunResim.Height);
                    int drawW = (int)(_sonUrunResim.Width * ratio);
                    int drawH = (int)(_sonUrunResim.Height * ratio);
                    int drawX = imgX + (imgSize - drawW) / 2;
                    int drawY = imgY + (imgSize - drawH) / 2;
                    g.DrawImage(_sonUrunResim, drawX, drawY, drawW, drawH);
                    textX = imgX + imgSize + 10;
                }

                using var lnFont = new Font("Segoe UI", 13, FontStyle.Bold);
                int textY = y + (barH - 20) / 2;
                DrawText(g, _sonUrunAdi, lnFont, White, textX, textY);

                using var lpFont = new Font("Segoe UI", 13, FontStyle.Bold);
                var lpText = $"₺{_sonUrunFiyat:N2}";
                var lpSize = MeasureText(g, lpText, lpFont);
                DrawText(g, lpText, lpFont, Green, W - pad - lpSize.Width - 10, textY);

                y += barH + 6;
            }

            // ── Separator ──
            using (var sepPen = new Pen(LineDark))
                g.DrawLine(sepPen, pad, y, W - pad, y);
            y += 6;

            // ── Items list ──
            int rowH = 32;
            int listBottom = r.Bottom - footerH - 8;
            int maxItems = Math.Max(1, (listBottom - y) / rowH);

            using var nFont = new Font("Segoe UI", 11);
            using var qFont = new Font("Segoe UI", 10);
            using var pFont = new Font("Segoe UI", 11);

            var visible = _items.TakeLast(maxItems).Reverse().ToList();

            for (int i = 0; i < visible.Count; i++)
            {
                var item = visible[i];
                var bgColor = i % 2 == 0 ? BgRowEven : BgRowOdd;
                using (var rBrush = new SolidBrush(bgColor))
                    g.FillRectangle(rBrush, pad, y, W - pad * 2, rowH);

                DrawText(g, item.UrunAdi ?? "", nFont, Gray, pad + 10, y + 6);

                DrawText(g, $"x{item.Miktar}", qFont, DimGray, W / 2 + 60, y + 7);

                var pt = $"₺{item.ToplamFiyat:N2}";
                var ps = MeasureText(g, pt, pFont);
                DrawText(g, pt, pFont, White, W - pad - ps.Width - 10, y + 6);

                y += rowH;
            }

            if (_items.Count > maxItems)
            {
                using var mf = new Font("Segoe UI", 9);
                DrawText(g, $"... ve {_items.Count - maxItems} ürün daha", mf, DimGray, pad + 10, y + 2);
            }

            // ── FOOTER — Total ──
            int fy = r.Bottom - footerH;
            using (var fBg = new SolidBrush(BgFooter))
                g.FillRectangle(fBg, 0, fy, W, footerH);
            using (var fLine = new Pen(LineDark))
                g.DrawLine(fLine, 0, fy, W, fy);
            using (var fAccent = new Pen(Accent, 2))
                g.DrawLine(fAccent, W / 4, fy, W * 3 / 4, fy);

            // "TOPLAM" left
            using var tlFont = new Font("Segoe UI", 11, FontStyle.Bold);
            DrawText(g, "TOPLAM", tlFont, DimGray, pad, fy + 28);

            // Total center
            using var tvFont = new Font("Segoe UI", 32, FontStyle.Bold);
            var tvText = $"₺{_toplam:N2}";
            var tvSize = MeasureText(g, tvText, tvFont);
            DrawText(g, tvText, tvFont, White, (W - tvSize.Width) / 2, fy + 16);

            // Item count right
            using var cFont = new Font("Segoe UI", 10);
            var cText = $"{_items.Count} ürün";
            var cSize = MeasureText(g, cText, cFont);
            DrawText(g, cText, cFont, DimGray, W - pad - cSize.Width, fy + 30);
        }

        // ─── TEXT HELPERS ─────────────────────────

        private void DrawText(Graphics g, string text, Font font, Color color, int x, int y)
        {
            TextRenderer.DrawText(g, text, font, new Point(x, y), color,
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
        }

        private Size MeasureText(Graphics g, string text, Font font)
        {
            return TextRenderer.MeasureText(g, text, font,
                new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _resetTimer?.Stop(); _resetTimer?.Dispose();
            _clockTimer?.Stop(); _clockTimer?.Dispose();
            _sonUrunResim?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
