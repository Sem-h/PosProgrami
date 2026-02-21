using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Text.Json;
using PosProjesi.DataAccess;
using PosProjesi.Models;
using PosProjesi.UI;

namespace PosProjesi.Forms
{
    public class MasaSecimForm : Form
    {
        private readonly MasaRepository _masaRepo = new();
        private FlowLayoutPanel pnlKategoriler = null!;
        private FlowLayoutPanel pnlMasalar = null!;
        private Label lblSecilenKategori = null!;
        private System.Windows.Forms.Timer? _refreshTimer;

        // Tile colors
        private static readonly Color BosMasaColor = Color.FromArgb(34, 120, 60);
        private static readonly Color DoluMasaColor = Color.FromArgb(180, 100, 30);
        private static readonly Color HizliSatisColor = Color.FromArgb(55, 120, 200);

        public MasaSecimForm()
        {
            InitializeComponent();
            LoadKategoriler();

            // Auto-refresh every 5 seconds to reflect changes from other stations
            _refreshTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            _refreshTimer.Tick += (s, e) => RefreshMasalar();
            _refreshTimer.Start();
        }

        private int _currentKategoriId = -1;

        private void InitializeComponent()
        {
            Theme.ApplyFormDefaults(this, "Verimek POS - Masa Seçimi");
            this.WindowState = FormWindowState.Maximized;
            this.MinimumSize = new Size(900, 600);
            this.DoubleBuffered = true;

            // ── HEADER ──
            var header = Theme.CreateHeaderBar("🍽️  Masa Seçimi", Theme.AccentTeal);

            // ── TOP: Category tabs + Quick sale button ──
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                Padding = new Padding(12, 8, 12, 4)
            };
            topPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border);
                e.Graphics.DrawLine(pen, 0, topPanel.Height - 1, topPanel.Width, topPanel.Height - 1);
            };

            // Quick sale button (no table)
            var btnHizliSatis = new Panel
            {
                Size = new Size(130, 48),
                Location = new Point(12, 10),
                Cursor = Cursors.Hand
            };
            bool hizliHover = false;
            btnHizliSatis.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                var rect = new Rectangle(0, 0, btnHizliSatis.Width - 1, btnHizliSatis.Height - 1);
                using var path = Theme.RoundedRect(rect, 8);
                var bg = hizliHover ? Color.FromArgb(70, 140, 220) : HizliSatisColor;
                using var bgBrush = new SolidBrush(bg);
                g.FillPath(bgBrush, path);

                using var font = new Font("Segoe UI", 10, FontStyle.Bold);
                var text = "⚡ Hızlı Satış";
                var textSize = TextRenderer.MeasureText(text, font);
                TextRenderer.DrawText(g, text, font,
                    new Point((btnHizliSatis.Width - textSize.Width) / 2, (btnHizliSatis.Height - textSize.Height) / 2),
                    Color.White, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            };
            btnHizliSatis.MouseEnter += (s, e) => { hizliHover = true; btnHizliSatis.Invalidate(); };
            btnHizliSatis.MouseLeave += (s, e) => { hizliHover = false; btnHizliSatis.Invalidate(); };
            btnHizliSatis.Click += (s, e) =>
            {
                new SatisForm(null).ShowDialog();
                RefreshMasalar();
            };
            topPanel.Controls.Add(btnHizliSatis);

            pnlKategoriler = new FlowLayoutPanel
            {
                Location = new Point(155, 10),
                Size = new Size(600, 50),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                AutoScroll = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                Padding = new Padding(2)
            };
            topPanel.Controls.Add(pnlKategoriler);

            // ── CENTER: Tables ──
            var centerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 8, 16, 16)
            };

            lblSecilenKategori = new Label
            {
                Text = "  Tüm Masalar",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Top,
                Height = 28
            };

            pnlMasalar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                Padding = new Padding(4)
            };

            centerPanel.Controls.Add(pnlMasalar);
            centerPanel.Controls.Add(lblSecilenKategori);

            this.Controls.Add(centerPanel);
            this.Controls.Add(topPanel);
            this.Controls.Add(header);

            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) this.Close();
            };
        }

        private void LoadKategoriler()
        {
            pnlKategoriler.Controls.Clear();
            var kategoriler = _masaRepo.GetKategoriler();

            // "Tümü" button
            var btnAll = CreateKatTile("Tümü", Color.FromArgb(70, 75, 90), -1);
            pnlKategoriler.Controls.Add(btnAll);

            var colors = new[] {
                Color.FromArgb(50, 170, 155),
                Color.FromArgb(140, 80, 190),
                Color.FromArgb(180, 90, 50),
                Color.FromArgb(55, 120, 200),
                Color.FromArgb(190, 60, 60),
            };

            int ci = 0;
            foreach (var k in kategoriler)
            {
                var color = colors[ci % colors.Length];
                var tile = CreateKatTile(k.Ad, color, k.Id);
                pnlKategoriler.Controls.Add(tile);
                ci++;
            }

            LoadMasalar(-1, "Tüm Masalar");
        }

        private Panel CreateKatTile(string text, Color color, int katId)
        {
            using var measureFont = new Font("Segoe UI", 9, FontStyle.Bold);
            var textSize = TextRenderer.MeasureText(text, measureFont);
            int tileW = Math.Max(80, textSize.Width + 28);

            var tile = new Panel
            {
                Size = new Size(tileW, 44),
                Margin = new Padding(3),
                Cursor = Cursors.Hand,
                Tag = katId
            };

            tile.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                var rect = new Rectangle(0, 0, tile.Width - 1, tile.Height - 1);
                using var path = Theme.RoundedRect(rect, 8);
                var bgColor = _currentKategoriId == katId ? color : Color.FromArgb(color.A, Math.Max(0, color.R - 30), Math.Max(0, color.G - 30), Math.Max(0, color.B - 30));
                using var bgBrush = new SolidBrush(bgColor);
                g.FillPath(bgBrush, path);

                if (_currentKategoriId == katId)
                {
                    using var borderPen = new Pen(Color.White, 2);
                    g.DrawPath(borderPen, path);
                }

                using var font = new Font("Segoe UI", 9, FontStyle.Bold);
                var ts = TextRenderer.MeasureText(text, font);
                TextRenderer.DrawText(g, text, font,
                    new Point((tile.Width - ts.Width) / 2, (tile.Height - ts.Height) / 2),
                    Color.White, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            };

            tile.Click += (s, e) =>
            {
                _currentKategoriId = katId;
                LoadMasalar(katId, katId == -1 ? "Tüm Masalar" : text);
                // Refresh category highlight
                foreach (Control c in pnlKategoriler.Controls) c.Invalidate();
            };

            return tile;
        }

        private void LoadMasalar(int kategoriId, string kategoriAdi)
        {
            _currentKategoriId = kategoriId;
            pnlMasalar.Controls.Clear();
            lblSecilenKategori.Text = $"  {kategoriAdi}";

            List<Masa> masalar;
            if (kategoriId == -1)
                masalar = _masaRepo.GetAll();
            else
                masalar = _masaRepo.GetByKategori(kategoriId);

            if (masalar.Count == 0)
            {
                var lbl = new Label
                {
                    Text = "Bu kategoride masa yok.\nYönetim Paneli'nden masa ekleyebilirsiniz.",
                    ForeColor = Theme.TextMuted,
                    Font = Theme.FontBody,
                    AutoSize = true,
                    Padding = new Padding(20)
                };
                pnlMasalar.Controls.Add(lbl);
                return;
            }

            foreach (var masa in masalar)
            {
                var tile = CreateMasaTile(masa);
                pnlMasalar.Controls.Add(tile);
            }
        }

        private void RefreshMasalar()
        {
            LoadMasalar(_currentKategoriId, lblSecilenKategori.Text.Trim());
        }

        private Panel CreateMasaTile(Masa masa)
        {
            bool isDolu = masa.Durum == "Dolu";
            decimal masaToplam = 0;

            if (isDolu && !string.IsNullOrEmpty(masa.AktifSepet))
            {
                try
                {
                    var items = JsonSerializer.Deserialize<List<SatisDetay>>(masa.AktifSepet);
                    if (items != null) masaToplam = items.Sum(i => i.ToplamFiyat);
                }
                catch { }
            }

            var tile = new Panel
            {
                Size = new Size(170, 140),
                Margin = new Padding(6),
                Cursor = Cursors.Hand,
                Tag = masa
            };

            bool isHover = false;

            tile.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                var rect = new Rectangle(0, 0, tile.Width - 1, tile.Height - 1);
                using var path = Theme.RoundedRect(rect, 12);

                var mainColor = isDolu ? DoluMasaColor : BosMasaColor;
                var bgColor = isHover
                    ? Color.FromArgb(mainColor.A, Math.Min(255, mainColor.R + 20), Math.Min(255, mainColor.G + 20), Math.Min(255, mainColor.B + 20))
                    : mainColor;

                // Gradient background
                using var bgBrush = new LinearGradientBrush(rect,
                    bgColor,
                    Color.FromArgb(bgColor.A, Math.Max(0, bgColor.R - 25), Math.Max(0, bgColor.G - 25), Math.Max(0, bgColor.B - 25)),
                    LinearGradientMode.Vertical);
                g.FillPath(bgBrush, path);

                // Border
                if (isHover)
                {
                    using var borderPen = new Pen(Color.FromArgb(200, 255, 255, 255), 2);
                    g.DrawPath(borderPen, path);
                }

                // Status indicator dot + text
                int dotY = 14;
                var statusColor = isDolu ? Color.FromArgb(255, 200, 80) : Color.FromArgb(80, 220, 100);
                using var dotBrush = new SolidBrush(statusColor);
                g.FillEllipse(dotBrush, 14, dotY, 10, 10);

                using var statusFont = new Font("Segoe UI", 7.5f, FontStyle.Bold);
                var statusText = isDolu ? "DOLU" : "BOŞ";
                TextRenderer.DrawText(g, statusText, statusFont, new Point(28, dotY - 1),
                    Color.FromArgb(200, 255, 255, 255), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

                // Table name (big, centered)
                using var nameFont = new Font("Segoe UI", 14, FontStyle.Bold);
                var nameSize = TextRenderer.MeasureText(masa.Ad, nameFont);
                TextRenderer.DrawText(g, masa.Ad, nameFont,
                    new Point((tile.Width - nameSize.Width) / 2, 36),
                    Color.White, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

                // Category label
                using var catFont = new Font("Segoe UI", 8);
                var catText = masa.MasaKategoriAdi ?? "";
                var catSize = TextRenderer.MeasureText(catText, catFont);
                TextRenderer.DrawText(g, catText, catFont,
                    new Point((tile.Width - catSize.Width) / 2, 60),
                    Color.FromArgb(180, 255, 255, 255), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

                // Bottom section
                if (isDolu && masaToplam > 0)
                {
                    // Show total
                    using var totalFont = new Font("Segoe UI", 12, FontStyle.Bold);
                    var totalText = $"₺{masaToplam:N2}";
                    var totalSize = TextRenderer.MeasureText(totalText, totalFont);
                    TextRenderer.DrawText(g, totalText, totalFont,
                        new Point((tile.Width - totalSize.Width) / 2, tile.Height - 35),
                        Color.FromArgb(255, 255, 220, 100), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
                }
                else if (!isDolu)
                {
                    // "Sipariş Al" hint
                    using var hintFont = new Font("Segoe UI", 8);
                    var hintText = "Sipariş Al →";
                    var hintSize = TextRenderer.MeasureText(hintText, hintFont);
                    TextRenderer.DrawText(g, hintText, hintFont,
                        new Point((tile.Width - hintSize.Width) / 2, tile.Height - 30),
                        Color.FromArgb(140, 255, 255, 255), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
                }
            };

            tile.MouseEnter += (s, e) => { isHover = true; tile.Invalidate(); };
            tile.MouseLeave += (s, e) => { isHover = false; tile.Invalidate(); };

            tile.Click += (s, e) =>
            {
                OpenMasaSatis(masa);
            };

            return tile;
        }

        private void OpenMasaSatis(Masa masa)
        {
            // Re-fetch fresh data
            var freshMasa = _masaRepo.GetById(masa.Id);
            if (freshMasa == null) return;

            new SatisForm(freshMasa).ShowDialog();
            RefreshMasalar();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
