using System.Text.Json;
using PosProjesi.DataAccess;
using PosProjesi.Models;
using PosProjesi.UI;

namespace PosProjesi.Forms
{
    public class SatisForm : Form
    {
        private readonly UrunRepository _urunRepo = new();
        private readonly SatisRepository _satisRepo = new();
        private readonly KategoriRepository _kategoriRepo = new();
        private readonly MasaRepository _masaRepo = new();
        private readonly List<SatisDetay> _sepet = new();
        private Masa? _masa;

        private FlowLayoutPanel pnlKategoriler = null!;
        private FlowLayoutPanel pnlUrunler = null!;
        private DataGridView dgvSepet = null!;
        private Label lblToplam = null!;
        private Label lblKategoriBaslik = null!;
        private TextBox txtBarkod = null!;
        private Panel pnlUrunArea = null!;
        private MusteriEkranForm? _musteriEkran;

        // Image cache to avoid repeated disk reads
        private static readonly Dictionary<string, Image?> _imageCache = new();

        // Tile colors for categories
        private static readonly Color[] KategoriRenkleri = new[]
        {
            Color.FromArgb(55, 120, 200),  // Blue
            Color.FromArgb(46, 160, 80),   // Green
            Color.FromArgb(180, 90, 50),   // Orange
            Color.FromArgb(140, 80, 190),  // Purple
            Color.FromArgb(50, 170, 155),  // Teal
            Color.FromArgb(190, 60, 60),   // Red
            Color.FromArgb(160, 145, 50),  // Gold
            Color.FromArgb(80, 140, 60),   // Forest
            Color.FromArgb(100, 100, 180), // Indigo
            Color.FromArgb(170, 80, 130),  // Pink
        };

        public SatisForm(Masa? masa = null)
        {
            _masa = masa;
            InitializeComponent();
            LoadKategoriler();

            // Load existing cart from table if available
            if (_masa != null && !string.IsNullOrEmpty(_masa.AktifSepet))
            {
                try
                {
                    var items = JsonSerializer.Deserialize<List<SatisDetay>>(_masa.AktifSepet);
                    if (items != null) { _sepet.AddRange(items); RefreshSepet(); }
                }
                catch { }
            }
        }

        private void InitializeComponent()
        {
            Theme.ApplyFormDefaults(this, "Verimek POS - Satış");
            this.WindowState = FormWindowState.Maximized;
            this.MinimumSize = new Size(1024, 600);
            this.KeyPreview = true;
            this.DoubleBuffered = true;

            // ── HEADER ──
            var headerText = "Satış Ekranı";
            if (_masa != null)
                headerText = $"🍽️  {_masa.MasaKategoriAdi} — {_masa.Ad}";
            var header = Theme.CreateHeaderBar(headerText, _masa != null ? Theme.AccentTeal : Theme.AccentGreen);

            // ══════════════════════════════════════════════
            //   LAYOUT:  [Left: Categories+Products]  [Right: Cart]
            // ══════════════════════════════════════════════

            // ── RIGHT PANEL: Cart ──
            var rightPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 340,
                BackColor = Theme.BgCard
            };
            var rightSep = new Panel { Dock = DockStyle.Left, Width = 1, BackColor = Theme.Border };
            rightPanel.Controls.Add(rightSep);

            // Cart header with barcode input
            var cartHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Theme.BgCard,
                Padding = new Padding(12, 8, 12, 4)
            };
            cartHeader.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border);
                e.Graphics.DrawLine(pen, 0, cartHeader.Height - 1, cartHeader.Width, cartHeader.Height - 1);
            };

            var lblSepet = new Label
            {
                Text = "🛒  SEPET",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(12, 8),
                AutoSize = true
            };

            txtBarkod = Theme.CreateTextBox(300);
            txtBarkod.Location = new Point(12, 38);
            txtBarkod.Size = new Size(rightPanel.Width - 38, 28);
            txtBarkod.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            txtBarkod.PlaceholderText = "Barkod okutun...";
            txtBarkod.Font = new Font("Segoe UI", 10);
            txtBarkod.KeyDown += TxtBarkod_KeyDown;

            cartHeader.Controls.AddRange(new Control[] { lblSepet, txtBarkod });

            // Cart grid
            dgvSepet = Theme.CreateGrid();
            dgvSepet.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "UrunAdi", HeaderText = "Ürün",
                MinimumWidth = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            dgvSepet.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Miktar", HeaderText = "Ad.",
                Width = 40, MinimumWidth = 35,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            dgvSepet.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ToplamFiyat", HeaderText = "Tutar",
                Width = 75, MinimumWidth = 65,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight,
                                     Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) }
            });
            dgvSepet.RowTemplate.Height = 28;

            // Cart bottom: total + payment
            var cartBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 150,
                BackColor = Theme.BgCard
            };
            cartBottom.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border);
                e.Graphics.DrawLine(pen, 0, 0, cartBottom.Width, 0);
            };

            lblToplam = new Label
            {
                Text = "₺0,00",
                Font = new Font("Segoe UI", 30, FontStyle.Bold),
                ForeColor = Theme.AccentGreen,
                AutoSize = true,
                Location = new Point(12, 8)
            };

            var lblToplamTitle = new Label
            {
                Text = "TOPLAM",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(14, 50)
            };

            var btnNakit = Theme.CreateButton("NAKİT [F5]", Theme.AccentGreen, 148, 40);
            btnNakit.Location = new Point(12, 72);
            btnNakit.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btnNakit.Click += (s, e) => TamamlaSatis("Nakit");

            var btnKart = Theme.CreateButton("KART [F6]", Theme.AccentBlue, 148, 40);
            btnKart.Location = new Point(168, 72);
            btnKart.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btnKart.Click += (s, e) => TamamlaSatis("Kredi Kartı");

            var btnTemizle = Theme.CreateButton("Sepeti Temizle", Theme.BgInput, 148, 30);
            btnTemizle.Location = new Point(12, 116);
            btnTemizle.ForeColor = Theme.TextSecondary;
            btnTemizle.Click += BtnTemizle_Click;

            var btnSil = Theme.CreateButton("Seçili Sil [Del]", Theme.AccentRed, 148, 30);
            btnSil.Location = new Point(168, 116);
            btnSil.Click += BtnSil_Click;

            cartBottom.Controls.AddRange(new Control[] { lblToplam, lblToplamTitle, btnNakit, btnKart, btnTemizle, btnSil });

            rightPanel.Controls.Add(dgvSepet);
            rightPanel.Controls.Add(cartHeader);
            rightPanel.Controls.Add(cartBottom);

            // ── LEFT PANEL: Categories + Products ──
            var leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(8, 8, 4, 8)
            };

            // Category tiles (top row, scrollable horizontally)
            var kategoriContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                Padding = new Padding(0)
            };

            var lblKatTitle = new Label
            {
                Text = "KATEGORİLER",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Theme.TextMuted,
                Location = new Point(4, 2),
                AutoSize = true
            };

            pnlKategoriler = new FlowLayoutPanel
            {
                Location = new Point(0, 20),
                Size = new Size(kategoriContainer.Width, 78),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                AutoScroll = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                Padding = new Padding(2)
            };

            kategoriContainer.Controls.Add(pnlKategoriler);
            kategoriContainer.Controls.Add(lblKatTitle);

            // Product tiles area
            pnlUrunArea = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 4, 0, 0)
            };

            lblKategoriBaslik = new Label
            {
                Text = "Kategori seçin...",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Top,
                Height = 22
            };

            pnlUrunler = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                Padding = new Padding(2)
            };

            pnlUrunArea.Controls.Add(pnlUrunler);
            pnlUrunArea.Controls.Add(lblKategoriBaslik);

            leftPanel.Controls.Add(pnlUrunArea);
            leftPanel.Controls.Add(kategoriContainer);

            this.Controls.Add(leftPanel);
            this.Controls.Add(rightPanel);
            this.Controls.Add(header);

            // Keyboard shortcuts
            this.KeyDown += (s, e) =>
            {
                switch (e.KeyCode)
                {
                    case Keys.F5: TamamlaSatis("Nakit"); e.Handled = true; break;
                    case Keys.F6: TamamlaSatis("Kredi Kartı"); e.Handled = true; break;
                    case Keys.Delete: BtnSil_Click(null, EventArgs.Empty); e.Handled = true; break;
                    case Keys.Escape: this.Close(); e.Handled = true; break;
                }
            };

            this.Shown += (s, e) =>
            {
                txtBarkod.Focus();
                _musteriEkran = new MusteriEkranForm();
                _musteriEkran.Show();
            };

            this.FormClosing += (s, e) =>
            {
                // Save cart to table if there's an active table with items
                if (_masa != null && _sepet.Count > 0)
                {
                    try
                    {
                        var json = JsonSerializer.Serialize(_sepet);
                        _masaRepo.SaveSepet(_masa.Id, json);
                    }
                    catch { }
                }
                else if (_masa != null && _sepet.Count == 0)
                {
                    _masaRepo.ClearMasa(_masa.Id);
                }

                if (_musteriEkran != null && !_musteriEkran.IsDisposed)
                { _musteriEkran.Close(); _musteriEkran = null; }
                // Dispose cached images
                foreach (var img in _imageCache.Values) img?.Dispose();
                _imageCache.Clear();
            };
        }

        // ─── CATEGORY & PRODUCT TILES ─────────────

        private void LoadKategoriler()
        {
            pnlKategoriler.Controls.Clear();
            var kategoriler = _kategoriRepo.GetAll();

            // "Tümü" button first
            var btnAll = CreateKategoriTile("Tümü", Color.FromArgb(70, 75, 90), -1);
            pnlKategoriler.Controls.Add(btnAll);

            int colorIndex = 0;
            foreach (var k in kategoriler)
            {
                var color = KategoriRenkleri[colorIndex % KategoriRenkleri.Length];
                var tile = CreateKategoriTile(k.Ad, color, k.Id);
                pnlKategoriler.Controls.Add(tile);
                colorIndex++;
            }

            // Load all products initially
            LoadUrunler(-1, "Tüm Ürünler");
        }

        private Panel CreateKategoriTile(string text, Color color, int kategoriId)
        {
            // Measure text to auto-size width
            using var measureFont = new Font("Segoe UI", 9, FontStyle.Bold);
            var textSize = TextRenderer.MeasureText(text, measureFont);
            int tileWidth = Math.Max(80, textSize.Width + 28);

            var tile = new Panel
            {
                Size = new Size(tileWidth, 50),
                Margin = new Padding(3),
                Cursor = Cursors.Hand,
                Tag = kategoriId
            };

            tile.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // Background with rounded corners
                var rect = new Rectangle(0, 0, tile.Width - 1, tile.Height - 1);
                using var path = Theme.RoundedRect(rect, 8);
                using var bgBrush = new SolidBrush(color);
                g.FillPath(bgBrush, path);

                // Text centered
                using var font = new Font("Segoe UI", 9, FontStyle.Bold);
                var textSize = TextRenderer.MeasureText(text, font);
                TextRenderer.DrawText(g, text, font,
                    new Point((tile.Width - textSize.Width) / 2, (tile.Height - textSize.Height) / 2),
                    Color.White, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            };

            tile.MouseEnter += (s, e) => { tile.Invalidate(); };
            tile.MouseLeave += (s, e) => { tile.Invalidate(); };

            tile.Click += (s, e) =>
            {
                LoadUrunler(kategoriId, text);
            };

            return tile;
        }

        private void LoadUrunler(int kategoriId, string kategoriAdi)
        {
            pnlUrunler.Controls.Clear();
            lblKategoriBaslik.Text = $"  {kategoriAdi}";

            List<Urun> urunler;
            if (kategoriId == -1)
                urunler = _urunRepo.GetAll();
            else
                urunler = _urunRepo.GetByKategori(kategoriId);

            if (urunler.Count == 0)
            {
                var lblBos = new Label
                {
                    Text = "Bu kategoride ürün yok.",
                    ForeColor = Theme.TextMuted,
                    Font = Theme.FontBody,
                    AutoSize = true,
                    Padding = new Padding(20)
                };
                pnlUrunler.Controls.Add(lblBos);
                return;
            }

            int colorIdx = 0;
            foreach (var urun in urunler)
            {
                var tile = CreateUrunTile(urun, colorIdx);
                pnlUrunler.Controls.Add(tile);
                colorIdx++;
            }
        }

        private static Image? GetCachedImage(string? resimYolu)
        {
            if (string.IsNullOrEmpty(resimYolu)) return null;
            if (_imageCache.TryGetValue(resimYolu, out var cached)) return cached;

            var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, resimYolu);
            if (File.Exists(fullPath))
            {
                try
                {
                    using var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                    var img = Image.FromStream(fs);
                    _imageCache[resimYolu] = img;
                    return img;
                }
                catch { _imageCache[resimYolu] = null; return null; }
            }
            _imageCache[resimYolu] = null;
            return null;
        }

        private Panel CreateUrunTile(Urun urun, int index)
        {
            bool hasImage = !string.IsNullOrEmpty(urun.ResimYolu);
            int tileHeight = hasImage ? 160 : 110;

            var tile = new Panel
            {
                Size = new Size(155, tileHeight),
                Margin = new Padding(4),
                Cursor = Cursors.Hand,
                Tag = urun
            };

            bool isHover = false;

            tile.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var rect = new Rectangle(0, 0, tile.Width - 1, tile.Height - 1);
                using var path = Theme.RoundedRect(rect, 8);

                bool outOfStock = urun.Stok <= 0;
                bool lowStock = urun.Stok > 0 && urun.Stok <= 5;

                // Background
                var bgColor = isHover ? Theme.BgHover : Theme.BgCard;
                using var bgBrush = new SolidBrush(bgColor);
                g.FillPath(bgBrush, path);

                // Border
                var borderColor = outOfStock ? Theme.AccentRed :
                                  isHover ? Theme.AccentGreen : Theme.Border;
                using var borderPen = new Pen(borderColor, 1);
                g.DrawPath(borderPen, path);

                int contentY = 0; // tracks vertical offset

                // Stock status strip at top (if applicable)
                if (outOfStock || lowStock)
                {
                    var stripColor = outOfStock
                        ? Color.FromArgb(200, 180, 50, 50)
                        : Color.FromArgb(200, 170, 120, 30);
                    var stripText = outOfStock ? "TÜKENDİ" : $"STOK AZ: {urun.Stok}";

                    using var stripBrush = new SolidBrush(stripColor);
                    var stripRect = new Rectangle(1, 1, tile.Width - 3, 20);
                    using var topPath = Theme.RoundedRect(new Rectangle(0, 0, tile.Width - 1, tile.Height - 1), 8);
                    g.SetClip(topPath);
                    g.FillRectangle(stripBrush, stripRect);
                    g.ResetClip();

                    using var stripFont = new Font("Segoe UI", 7.5f, FontStyle.Bold);
                    var textSize = TextRenderer.MeasureText(stripText, stripFont);
                    TextRenderer.DrawText(g, stripText, stripFont,
                        new Point((tile.Width - textSize.Width) / 2, 2),
                        Color.White, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

                    contentY = 22;
                }

                // Product image
                var img = GetCachedImage(urun.ResimYolu);
                if (img != null)
                {
                    int imgAreaH = 70;
                    int imgY = contentY + 4;
                    // Center the image in the available width
                    double ratio = Math.Min((double)(tile.Width - 20) / img.Width, (double)imgAreaH / img.Height);
                    int drawW = (int)(img.Width * ratio);
                    int drawH = (int)(img.Height * ratio);
                    int drawX = (tile.Width - drawW) / 2;
                    int drawY = imgY + (imgAreaH - drawH) / 2;

                    // Clip to rounded rect
                    using var clipPath = Theme.RoundedRect(new Rectangle(0, 0, tile.Width - 1, tile.Height - 1), 8);
                    g.SetClip(clipPath);
                    g.DrawImage(img, drawX, drawY, drawW, drawH);
                    g.ResetClip();

                    contentY = imgY + imgAreaH + 2;
                }
                else
                {
                    contentY = contentY == 0 ? 10 : contentY + 4;
                }

                // Product name
                using var nameFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                var nameColor = outOfStock ? Theme.TextMuted : Theme.TextPrimary;
                int nameHeight = hasImage ? 34 : 44;
                var nameRect2 = new Rectangle(8, contentY, tile.Width - 16, nameHeight);
                TextRenderer.DrawText(g, urun.Ad, nameFont, nameRect2, nameColor,
                    TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

                // Price at bottom
                using var priceFont = new Font("Segoe UI", 11, FontStyle.Bold);
                var priceText = $"₺{urun.SatisFiyati:N2}";
                var priceColor = outOfStock ? Theme.TextMuted : Theme.AccentGreen;
                TextRenderer.DrawText(g, priceText, priceFont,
                    new Point(8, tile.Height - 28), priceColor,
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            };

            tile.MouseEnter += (s, e) => { isHover = true; tile.Invalidate(); };
            tile.MouseLeave += (s, e) => { isHover = false; tile.Invalidate(); };

            tile.Click += (s, e) => AddUrunToSepet(urun, 1);

            return tile;
        }

        // ─── CART OPERATIONS ──────────────────────

        private void AddUrunToSepet(Urun urun, int miktar)
        {
            if (urun.Stok < miktar)
            {
                MessageBox.Show($"Yetersiz stok! Mevcut: {urun.Stok}", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var existing = _sepet.FirstOrDefault(d => d.UrunId == urun.Id);
            if (existing != null)
            {
                existing.Miktar += miktar;
                existing.ToplamFiyat = existing.Miktar * existing.BirimFiyat;
            }
            else
            {
                _sepet.Add(new SatisDetay
                {
                    UrunId = urun.Id,
                    UrunAdi = urun.Ad,
                    Miktar = miktar,
                    BirimFiyat = urun.SatisFiyati,
                    ToplamFiyat = miktar * urun.SatisFiyati
                });
            }

            RefreshSepet();
            _musteriEkran?.UrunEklendi(urun.Ad, urun.SatisFiyati, miktar, urun.ResimYolu);
            txtBarkod.Focus();
        }

        private void TxtBarkod_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter || string.IsNullOrWhiteSpace(txtBarkod.Text)) return;
            e.SuppressKeyPress = true;
            var searchTerm = txtBarkod.Text.Trim();

            var urun = _urunRepo.GetByBarkod(searchTerm);
            if (urun == null)
            {
                var results = _urunRepo.Search(searchTerm);
                if (results.Count == 1)
                    urun = results[0];
                else if (results.Count > 1)
                {
                    var selectForm = new Form
                    {
                        Text = "Ürün Seçin",
                        Size = new Size(420, 300),
                        StartPosition = FormStartPosition.CenterParent,
                        BackColor = Theme.BgDark,
                        ForeColor = Theme.TextPrimary
                    };
                    var listBox = new ListBox
                    {
                        Dock = DockStyle.Fill,
                        BackColor = Theme.BgCard,
                        ForeColor = Theme.TextPrimary,
                        Font = Theme.FontBody,
                        BorderStyle = BorderStyle.None
                    };
                    foreach (var u in results)
                        listBox.Items.Add($"{u.Ad}  —  ₺{u.SatisFiyati:N2}  (Stok: {u.Stok})");
                    listBox.DoubleClick += (s2, e2) =>
                    {
                        if (listBox.SelectedIndex >= 0)
                        { urun = results[listBox.SelectedIndex]; selectForm.DialogResult = DialogResult.OK; selectForm.Close(); }
                    };
                    selectForm.Controls.Add(listBox);
                    if (selectForm.ShowDialog() != DialogResult.OK) { urun = null; }
                }
            }

            if (urun != null)
                AddUrunToSepet(urun, 1);
            else if (!string.IsNullOrEmpty(searchTerm))
                MessageBox.Show("Ürün bulunamadı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);

            txtBarkod.Clear();
        }

        private void BtnSil_Click(object? sender, EventArgs e)
        {
            if (dgvSepet.SelectedRows.Count > 0)
            {
                var idx = dgvSepet.SelectedRows[0].Index;
                if (idx >= 0 && idx < _sepet.Count) { _sepet.RemoveAt(idx); RefreshSepet(); }
            }
        }

        private void BtnTemizle_Click(object? sender, EventArgs e)
        {
            if (_sepet.Count == 0) return;
            if (MessageBox.Show("Sepeti temizle?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            { _sepet.Clear(); RefreshSepet(); }
        }

        private void RefreshSepet()
        {
            dgvSepet.Rows.Clear();
            foreach (var item in _sepet)
                dgvSepet.Rows.Add(item.UrunAdi, item.Miktar, $"₺{item.ToplamFiyat:N2}");
            var toplam = _sepet.Sum(d => d.ToplamFiyat);
            lblToplam.Text = $"₺{toplam:N2}";
            _musteriEkran?.SepetGuncelle(_sepet.ToList(), toplam);
        }

        private void TamamlaSatis(string odemeTipi)
        {
            if (_sepet.Count == 0) { MessageBox.Show("Sepet boş!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            var toplam = _sepet.Sum(d => d.ToplamFiyat);

            if (MessageBox.Show($"Ödeme: {odemeTipi}\nToplam: ₺{toplam:N2}\n\nOnayla?",
                "Satış Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    var satis = new Satis
                    {
                        ToplamTutar = toplam,
                        OdemeTipi = odemeTipi,
                        KasiyerAdi = Program.ActivePersonel?.TamAd ?? "Kasiyer",
                        PersonelId = Program.ActivePersonel?.Id,
                        MasaId = _masa?.Id,
                        MasaAdi = _masa != null ? $"{_masa.MasaKategoriAdi} - {_masa.Ad}" : null
                    };
                    var satisId = _satisRepo.CreateSatis(satis, _sepet.ToList());
                    MessageBox.Show($"Satış tamamlandı!\nFiş No: {satisId}  Toplam: ₺{toplam:N2}",
                        "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _musteriEkran?.SatisTamamlandi(toplam);

                    // Clear table
                    if (_masa != null)
                    {
                        _masaRepo.ClearMasa(_masa.Id);
                        _masa = null; // Prevent double-save on FormClosing
                    }

                    _sepet.Clear();
                    RefreshSepet();
                    txtBarkod.Clear();
                    txtBarkod.Focus();

                    // If table-based, close form after sale
                    if (satis.MasaId != null)
                        this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
