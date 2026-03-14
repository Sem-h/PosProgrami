using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Text;
using PosProjesi.Database;
using PosProjesi.DataAccess;
using PosProjesi.Models;
using PosProjesi.UI;
using PosProjesi.Services;

namespace PosProjesi.Forms
{
    public class AdminPanelForm : Form
    {
        private Panel _contentPanel = null!;
        private readonly List<Panel> _cards = new();

        public AdminPanelForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Theme.ApplyFormDefaults(this, "Verimek POS - Yönetim");
            this.Size = new Size(900, 640);
            this.MinimumSize = new Size(780, 560);

            var header = Theme.CreateHeaderBar("Yönetim Paneli", Theme.AccentOrange);

            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 24, 28, 20),
                BackColor = Theme.BgDark
            };

            var items = new (string title, string desc, string icon, Color accent, Action action)[]
            {
                ("Ürün Yönetimi",      "Ürün ekle, düzenle, stok ve fiyat güncelle",        "📦", Theme.AccentBlue,   () => new UrunYonetimForm().ShowDialog()),
                ("Raporlar",           "Satış raporları, istatistikler",                     "📊", Theme.AccentPurple, () => new RaporForm().ShowDialog()),
                ("Kategoriler",        "Kategori ekle, düzenle ve organize et",              "🏷️", Theme.AccentTeal,   () => new KategoriYonetimForm().ShowDialog()),
                ("DB Yedekleme",       "Veritabanını yedekle ve geri yükle",                 "💾", Color.FromArgb(52, 152, 219), BackupDatabase),
                ("Excel Dışa Aktar",   "Ürünleri Excel dosyasına aktar",                     "📤", Color.FromArgb(46, 204, 113), ExportToExcel),
                ("Excel İçe Aktar",    "Excel dosyasından ürün ekle",                        "📥", Color.FromArgb(231, 76, 60),  ImportFromExcel),
                ("Güncelleme Kontrol", "Yeni sürüm kontrolü yap ve güncelle",                "🔄", Theme.AccentGreen,  CheckForUpdate),
                ("Personel Yönetimi", "Kasa personeli ekle, düzenle ve sil",                "👤", Color.FromArgb(155, 89, 182), () => new PersonelYonetimForm().ShowDialog()),
                ("Masa Yönetimi",     "Masa kategorileri ve masaları yönetin",             "🍽️", Theme.AccentTeal,              () => new MasaYonetimForm().ShowDialog()),
                ("Fiş Ayarları",       "Yazıcı, fiş tasarımı ve işletme bilgileri",        "🖨️",  Color.FromArgb(243, 156, 18), () => new FisAyarlariForm().ShowDialog()),
                ("Geri Dön",           "Ana menüye geri dön",                                "↩️",  Theme.TextMuted,    () => this.Close()),
            };

            foreach (var (title, desc, icon, accent, action) in items)
            {
                var card = CreateCard(title, desc, icon, accent, 310, 150);
                card.Click += (s, e) => action();
                _cards.Add(card);
                _contentPanel.Controls.Add(card);
            }

            _contentPanel.Resize += (s, e) => LayoutCards();

            this.Controls.Add(_contentPanel);
            this.Controls.Add(header);

            this.Load += (s, e) => LayoutCards();
        }

        // ═══════════════════════════════════════════
        //  DB Yedekleme
        // ═══════════════════════════════════════════
        private void BackupDatabase()
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Veritabanı Yedeğini Kaydet",
                Filter = "SQLite Veritabanı|*.db|Tüm Dosyalar|*.*",
                FileName = $"pos_yedek_{DateTime.Now:yyyy-MM-dd_HHmm}.db",
                DefaultExt = "db"
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pos_database.db");
                File.Copy(dbPath, dialog.FileName, true);

                MessageBox.Show(
                    $"Veritabanı başarıyla yedeklendi!\n\n📁 {dialog.FileName}",
                    "Yedekleme Başarılı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Yedekleme sırasında hata oluştu:\n{ex.Message}",
                    "Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // ═══════════════════════════════════════════
        //  Excel Dışa Aktar (CSV)
        // ═══════════════════════════════════════════
        private void ExportToExcel()
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Ürünleri Dışa Aktar",
                Filter = "CSV Dosyası (Excel)|*.csv|Tüm Dosyalar|*.*",
                FileName = $"urunler_{DateTime.Now:yyyy-MM-dd}.csv",
                DefaultExt = "csv"
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var repo = new UrunRepository();
                var urunler = repo.GetAll();

                var sb = new StringBuilder();
                // BOM for Excel Turkish character support
                sb.AppendLine("Barkod;Ürün Adı;Kategori;Alış Fiyatı;Satış Fiyatı;Stok");

                foreach (var u in urunler)
                {
                    sb.AppendLine($"{u.Barkod};{u.Ad};{u.KategoriAdi};{u.AlisFiyati:F2};{u.SatisFiyati:F2};{u.Stok}");
                }

                File.WriteAllText(dialog.FileName, sb.ToString(), new UTF8Encoding(true));

                var result = MessageBox.Show(
                    $"✅ {urunler.Count} ürün başarıyla dışa aktarıldı!\n\n📁 {dialog.FileName}\n\nDosyayı açmak ister misiniz?",
                    "Dışa Aktarma Başarılı",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = dialog.FileName,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Dışa aktarma sırasında hata:\n{ex.Message}",
                    "Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // ═══════════════════════════════════════════
        //  Excel İçe Aktar (CSV)
        // ═══════════════════════════════════════════
        private void ImportFromExcel()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Ürünleri İçe Aktar",
                Filter = "CSV Dosyası|*.csv|Tüm Dosyalar|*.*",
                DefaultExt = "csv"
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var lines = File.ReadAllLines(dialog.FileName, Encoding.UTF8);
                if (lines.Length < 2)
                {
                    MessageBox.Show("Dosyada veri bulunamadı.\n\nBeklenen format:\nBarkod;Ürün Adı;Kategori;Alış Fiyatı;Satış Fiyatı;Stok",
                        "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var repo = new UrunRepository();
                var katRepo = new KategoriRepository();
                var kategoriler = katRepo.GetAll();

                int eklenen = 0;
                int guncellenen = 0;
                int hatali = 0;
                var hatalar = new StringBuilder();

                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split(';');
                    if (parts.Length < 5)
                    {
                        hatali++;
                        hatalar.AppendLine($"Satır {i + 1}: Eksik sütun");
                        continue;
                    }

                    try
                    {
                        var barkod = parts[0].Trim();
                        var ad = parts[1].Trim();
                        var kategoriAd = parts.Length > 2 ? parts[2].Trim() : "";
                        var alisFiyat = decimal.Parse(parts[3].Trim().Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture);
                        var satisFiyat = decimal.Parse(parts[4].Trim().Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture);
                        var stok = parts.Length > 5 ? int.Parse(parts[5].Trim()) : 0;

                        // Find or default category
                        int? kategoriId = null;
                        if (!string.IsNullOrEmpty(kategoriAd))
                        {
                            var kat = kategoriler.FirstOrDefault(k => k.Ad.Equals(kategoriAd, StringComparison.OrdinalIgnoreCase));
                            if (kat != null) kategoriId = kat.Id;
                        }

                        // Check if product exists by barkod
                        var existing = !string.IsNullOrEmpty(barkod) ? repo.GetByBarkod(barkod) : null;

                        if (existing != null)
                        {
                            existing.Ad = ad;
                            existing.AlisFiyati = alisFiyat;
                            existing.SatisFiyati = satisFiyat;
                            existing.Stok = stok;
                            if (kategoriId.HasValue) existing.KategoriId = kategoriId;
                            repo.Update(existing);
                            guncellenen++;
                        }
                        else
                        {
                            repo.Add(new Urun
                            {
                                Barkod = string.IsNullOrEmpty(barkod) ? null : barkod,
                                Ad = ad,
                                KategoriId = kategoriId,
                                AlisFiyati = alisFiyat,
                                SatisFiyati = satisFiyat,
                                Stok = stok
                            });
                            eklenen++;
                        }
                    }
                    catch (Exception ex)
                    {
                        hatali++;
                        hatalar.AppendLine($"Satır {i + 1}: {ex.Message}");
                    }
                }

                var msg = $"İçe aktarma tamamlandı!\n\n" +
                          $"✅ Yeni eklenen: {eklenen}\n" +
                          $"🔄 Güncellenen: {guncellenen}\n" +
                          (hatali > 0 ? $"❌ Hatalı satırlar: {hatali}\n\n{hatalar}" : "");

                MessageBox.Show(msg,
                    "İçe Aktarma Sonucu",
                    MessageBoxButtons.OK,
                    hatali > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"İçe aktarma sırasında hata:\n{ex.Message}",
                    "Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // ═══════════════════════════════════════════
        //  Güncelleme Kontrol
        // ═══════════════════════════════════════════
        private async void CheckForUpdate()
        {
            using var service = new UpdateService();
            var info = await service.CheckOnceAsync();

            if (info == null)
            {
                MessageBox.Show(
                    $"Mevcut sürüm: v{UpdateService.CurrentVersion}\n\nGüncelsiniz! Yeni güncelleme bulunamadı.",
                    "Güncelleme Kontrolü",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Yeni sürüm: v{info.Version}\nMevcut sürüm: v{UpdateService.CurrentVersion}\n\n{info.Notes}\n\nŞimdi güncellemek ister misiniz?",
                "Güncelleme Mevcut",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                var toast = new UpdateToastPanel(info);
                toast.ShowIn(this);
            }
        }

        // ═══════════════════════════════════════════
        //  Layout
        // ═══════════════════════════════════════════
        private void LayoutCards()
        {
            if (_contentPanel == null || _cards.Count == 0) return;

            int padX = _contentPanel.Padding.Left;
            int padY = _contentPanel.Padding.Top;
            int availW = _contentPanel.ClientSize.Width - padX - _contentPanel.Padding.Right;
            int availH = _contentPanel.ClientSize.Height - padY - _contentPanel.Padding.Bottom;

            int cols = 4;
            int gap = 14;
            int totalRows = (int)Math.Ceiling(_cards.Count / (double)cols);

            int cardW = (availW - gap * (cols - 1)) / cols;
            cardW = Math.Max(cardW, 160);
            int cardH = (availH - gap * (totalRows - 1)) / totalRows;
            cardH = Math.Clamp(cardH, 130, 200);

            for (int i = 0; i < _cards.Count; i++)
            {
                int row = i / cols;
                int col = i % cols;
                _cards[i].Size = new Size(cardW, cardH);
                _cards[i].Location = new Point(padX + col * (cardW + gap), padY + row * (cardH + gap));
                _cards[i].Invalidate();
            }
        }

        private Panel CreateCard(string title, string desc, string icon, Color accent, int w, int h)
        {
            var card = new Panel
            {
                Size = new Size(w, h),
                Cursor = Cursors.Hand
            };

            bool isHover = false;

            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using var path = Theme.RoundedRect(rect, 14);

                // Background with subtle gradient on hover
                if (isHover)
                {
                    using var bgBrush = new LinearGradientBrush(
                        rect,
                        Color.FromArgb(36, 38, 52),
                        Color.FromArgb(28, 30, 42),
                        LinearGradientMode.Vertical);
                    g.FillPath(bgBrush, path);
                }
                else
                {
                    using var bgBrush = new SolidBrush(Theme.BgCard);
                    g.FillPath(bgBrush, path);
                }

                // Border
                using var borderPen = new Pen(isHover ? accent : Theme.Border, isHover ? 1.5f : 1f);
                g.DrawPath(borderPen, path);

                // Top accent gradient stripe
                g.SetClip(path);
                using var accentBrush = new LinearGradientBrush(
                    new Rectangle(0, 0, card.Width, 3),
                    accent, Theme.Darken(accent, 40),
                    LinearGradientMode.Horizontal);
                g.FillRectangle(accentBrush, 0, 0, card.Width, 3);
                g.ResetClip();

                // Dynamic Y positions
                int ch = card.Height;
                int iconY = 16;
                int titleY = iconY + 46;
                int descY = titleY + 26;

                // Icon circle background
                var iconBgRect = new Rectangle(18, iconY, 40, 40);
                using var iconBgBrush = new SolidBrush(Color.FromArgb(25, accent.R, accent.G, accent.B));
                g.FillEllipse(iconBgBrush, iconBgRect);
                using var iconBorderPen = new Pen(Color.FromArgb(40, accent.R, accent.G, accent.B), 1);
                g.DrawEllipse(iconBorderPen, iconBgRect);

                // Icon emoji centered in circle
                using var iconFont = new Font("Segoe UI Emoji", 16);
                var iconSize = TextRenderer.MeasureText(icon, iconFont);
                int iconX = iconBgRect.X + (iconBgRect.Width - iconSize.Width) / 2 + 2;
                int iconTextY = iconBgRect.Y + (iconBgRect.Height - iconSize.Height) / 2 + 1;
                TextRenderer.DrawText(g, icon, iconFont, new Point(iconX, iconTextY), accent,
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

                // Title
                using var titleFont = new Font("Segoe UI", 12, FontStyle.Bold);
                TextRenderer.DrawText(g, title, titleFont, new Point(20, titleY),
                    Theme.TextPrimary, TextFormatFlags.NoPadding);

                // Description
                using var descFont = new Font("Segoe UI", 8.5f);
                var descRect = new Rectangle(20, descY, card.Width - 40, ch - descY - 8);
                TextRenderer.DrawText(g, desc, descFont, descRect,
                    Theme.TextSecondary,
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);

                // Hover arrow
                if (isHover && accent != Theme.TextMuted)
                {
                    using var arrowFont = new Font("Segoe UI", 14, FontStyle.Bold);
                    TextRenderer.DrawText(g, "→", arrowFont,
                        new Point(card.Width - 34, ch / 2 - 10), accent,
                        TextFormatFlags.NoPadding);
                }
            };

            card.MouseEnter += (s, e) => { isHover = true; card.Invalidate(); };
            card.MouseLeave += (s, e) => { isHover = false; card.Invalidate(); };

            return card;
        }
    }
}
