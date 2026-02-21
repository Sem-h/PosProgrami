using PosProjesi.DataAccess;
using PosProjesi.Models;
using PosProjesi.UI;

namespace PosProjesi.Forms
{
    public class UrunYonetimForm : Form
    {
        private readonly UrunRepository _urunRepo = new();
        private readonly KategoriRepository _kategoriRepo = new();

        private DataGridView dgvUrunler = null!;
        private TextBox txtAd = null!;
        private TextBox txtBarkod = null!;
        private TextBox txtAlisFiyati = null!;
        private TextBox txtSatisFiyati = null!;
        private TextBox txtStok = null!;
        private ComboBox cmbKategori = null!;
        private TextBox txtArama = null!;
        private PictureBox picUrunResim = null!;
        private int _selectedUrunId = 0;
        private string? _selectedResimYolu = null;

        private static readonly string ResimKlasoru = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "UrunResimleri");

        public UrunYonetimForm()
        {
            InitializeComponent();
            LoadKategoriler();
            LoadUrunler();
        }

        private void InitializeComponent()
        {
            Theme.ApplyFormDefaults(this, "Verimek POS - Ürün Yönetimi");
            this.Size = new Size(1050, 700);
            this.MinimumSize = new Size(900, 600);

            var header = Theme.CreateHeaderBar("Ürün Yönetimi", Theme.AccentBlue);

            // ── LEFT: Form ──
            var leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 310,
                BackColor = Theme.BgCard,
                Padding = new Padding(20, 16, 20, 16),
                AutoScroll = true
            };
            var leftSep = new Panel { Dock = DockStyle.Right, Width = 1, BackColor = Theme.Border };
            leftPanel.Controls.Add(leftSep);

            int y = 16;
            void AddField(string label, Control control, int controlWidth = 250)
            {
                var lbl = Theme.CreateLabel(label);
                lbl.Location = new Point(20, y);
                leftPanel.Controls.Add(lbl);
                y += 22;
                control.Location = new Point(20, y);
                if (control is TextBox) control.Size = new Size(controlWidth, 30);
                leftPanel.Controls.Add(control);
                y += 38;
            }

            txtAd = Theme.CreateTextBox(250);
            AddField("Ürün Adı", txtAd);

            txtBarkod = Theme.CreateTextBox(250);
            AddField("Barkod", txtBarkod);

            cmbKategori = new ComboBox
            {
                Size = new Size(250, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextPrimary,
                Font = Theme.FontBody,
                FlatStyle = FlatStyle.Flat
            };
            AddField("Kategori", cmbKategori);

            // Price row (side by side)
            var lblAlis = Theme.CreateLabel("Alış ₺");
            lblAlis.Location = new Point(20, y);
            var lblSatis = Theme.CreateLabel("Satış ₺");
            lblSatis.Location = new Point(145, y);
            leftPanel.Controls.AddRange(new Control[] { lblAlis, lblSatis });
            y += 22;

            txtAlisFiyati = Theme.CreateTextBox(115);
            txtAlisFiyati.Location = new Point(20, y);
            txtSatisFiyati = Theme.CreateTextBox(115);
            txtSatisFiyati.Location = new Point(145, y);
            leftPanel.Controls.AddRange(new Control[] { txtAlisFiyati, txtSatisFiyati });
            y += 38;

            txtStok = Theme.CreateTextBox(115);
            txtStok.Text = "0";
            AddField("Stok", txtStok, 115);

            // ── Product Image Section ──
            var lblResim = Theme.CreateLabel("Ürün Resmi");
            lblResim.Location = new Point(20, y);
            leftPanel.Controls.Add(lblResim);
            y += 22;

            picUrunResim = new PictureBox
            {
                Location = new Point(20, y),
                Size = new Size(140, 140),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Theme.BgInput,
                BorderStyle = BorderStyle.None,
                Cursor = Cursors.Hand
            };
            picUrunResim.Paint += (s, e) =>
            {
                if (picUrunResim.Image == null)
                {
                    var g = e.Graphics;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    // Draw dashed border
                    using var pen = new Pen(Theme.TextMuted, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
                    g.DrawRectangle(pen, 1, 1, picUrunResim.Width - 3, picUrunResim.Height - 3);

                    // Draw camera icon text
                    using var iconFont = new Font("Segoe UI", 28);
                    var iconText = "📷";
                    var iconSize = TextRenderer.MeasureText(iconText, iconFont);
                    TextRenderer.DrawText(g, iconText, iconFont,
                        new Point((picUrunResim.Width - iconSize.Width) / 2, (picUrunResim.Height - iconSize.Height) / 2 - 12),
                        Theme.TextMuted);

                    using var hintFont = new Font("Segoe UI", 8);
                    var hintText = "Resim seç...";
                    var hintSize = TextRenderer.MeasureText(hintText, hintFont);
                    TextRenderer.DrawText(g, hintText, hintFont,
                        new Point((picUrunResim.Width - hintSize.Width) / 2, (picUrunResim.Height - hintSize.Height) / 2 + 22),
                        Theme.TextMuted);
                }
            };
            picUrunResim.Click += (s, e) => ResimSec();
            leftPanel.Controls.Add(picUrunResim);

            var btnResimSec = Theme.CreateButton("📁 Resim Seç", Theme.AccentBlue, 120, 30);
            btnResimSec.Location = new Point(20, y + 145);
            btnResimSec.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnResimSec.Click += (s, e) => ResimSec();

            var btnResimSil = Theme.CreateButton("✕", Theme.AccentRed, 30, 30);
            btnResimSil.Location = new Point(145, y + 145);
            btnResimSil.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnResimSil.Click += (s, e) => ResimTemizle();

            leftPanel.Controls.AddRange(new Control[] { btnResimSec, btnResimSil });
            y += 185;

            var btnKaydet = Theme.CreateButton("Kaydet", Theme.AccentGreen, 120, 38);
            btnKaydet.Location = new Point(20, y);
            btnKaydet.Click += BtnKaydet_Click;

            var btnYeni = Theme.CreateButton("Yeni", Theme.AccentBlue, 120, 38);
            btnYeni.Location = new Point(150, y);
            btnYeni.Click += BtnYeni_Click;
            y += 48;

            var btnSil = Theme.CreateButton("Sil", Theme.AccentRed, 250, 34);
            btnSil.Location = new Point(20, y);
            btnSil.Font = Theme.FontBody;
            btnSil.Click += BtnSil_Click;

            leftPanel.Controls.AddRange(new Control[] { btnKaydet, btnYeni, btnSil });

            // ── RIGHT: List ──
            var rightPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };

            var searchPanel = new Panel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(0, 4, 0, 4) };
            txtArama = Theme.CreateTextBox(500);
            txtArama.Dock = DockStyle.Fill;
            txtArama.PlaceholderText = "Ürün veya barkod ara...";
            txtArama.TextChanged += (s, e) => LoadUrunler(txtArama.Text);
            searchPanel.Controls.Add(txtArama);

            dgvUrunler = Theme.CreateGrid();
            dgvUrunler.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Width = 50, MinimumWidth = 40 });
            dgvUrunler.Columns.Add(new DataGridViewTextBoxColumn { Name = "Barkod", HeaderText = "Barkod", Width = 110, MinimumWidth = 80 });
            dgvUrunler.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ad", HeaderText = "Ürün Adı", MinimumWidth = 120, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvUrunler.Columns.Add(new DataGridViewTextBoxColumn { Name = "Kategori", HeaderText = "Kategori", Width = 100, MinimumWidth = 70 });
            dgvUrunler.Columns.Add(new DataGridViewTextBoxColumn { Name = "AlisFiyati", HeaderText = "Alış ₺", Width = 90, MinimumWidth = 70, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgvUrunler.Columns.Add(new DataGridViewTextBoxColumn { Name = "SatisFiyati", HeaderText = "Satış ₺", Width = 90, MinimumWidth = 70, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgvUrunler.Columns.Add(new DataGridViewTextBoxColumn { Name = "Stok", HeaderText = "Stok", Width = 60, MinimumWidth = 45, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvUrunler.Columns.Add(new DataGridViewTextBoxColumn { Name = "ResimYolu", HeaderText = "Resim", Visible = false });
            dgvUrunler.CellClick += DgvUrunler_CellClick;

            rightPanel.Controls.Add(dgvUrunler);
            rightPanel.Controls.Add(searchPanel);

            this.Controls.Add(rightPanel);
            this.Controls.Add(leftPanel);
            this.Controls.Add(header);
        }

        private void ResimSec()
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Ürün Resmi Seçin",
                Filter = "Resim Dosyaları|*.jpg;*.jpeg;*.png;*.bmp;*.webp|Tüm Dosyalar|*.*"
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // Ensure directory exists
                    if (!Directory.Exists(ResimKlasoru))
                        Directory.CreateDirectory(ResimKlasoru);

                    // Generate unique filename
                    var ext = Path.GetExtension(dlg.FileName);
                    var yeniDosyaAdi = $"urun_{DateTime.Now:yyyyMMddHHmmssfff}{ext}";
                    var hedefYol = Path.Combine(ResimKlasoru, yeniDosyaAdi);

                    // Load, resize, and save
                    using var orijinal = Image.FromFile(dlg.FileName);
                    using var kucuk = ResizeImage(orijinal, 200, 200);
                    kucuk.Save(hedefYol);

                    // Delete old image if replacing
                    if (!string.IsNullOrEmpty(_selectedResimYolu))
                    {
                        var eskiYol = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _selectedResimYolu);
                        if (File.Exists(eskiYol)) try { File.Delete(eskiYol); } catch { }
                    }

                    _selectedResimYolu = Path.Combine("UrunResimleri", yeniDosyaAdi);
                    LoadResimOnizleme(_selectedResimYolu);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Resim yüklenirken hata: {ex.Message}", "Hata",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ResimTemizle()
        {
            _selectedResimYolu = null;
            picUrunResim.Image?.Dispose();
            picUrunResim.Image = null;
            picUrunResim.Invalidate();
        }

        private void LoadResimOnizleme(string? resimYolu)
        {
            picUrunResim.Image?.Dispose();
            picUrunResim.Image = null;

            if (!string.IsNullOrEmpty(resimYolu))
            {
                var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, resimYolu);
                if (File.Exists(fullPath))
                {
                    try
                    {
                        using var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                        picUrunResim.Image = Image.FromStream(fs);
                    }
                    catch { }
                }
            }
            picUrunResim.Invalidate();
        }

        private static Bitmap ResizeImage(Image source, int maxWidth, int maxHeight)
        {
            double ratio = Math.Min((double)maxWidth / source.Width, (double)maxHeight / source.Height);
            int newW = (int)(source.Width * ratio);
            int newH = (int)(source.Height * ratio);

            var bmp = new Bitmap(newW, newH);
            using var g = Graphics.FromImage(bmp);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.DrawImage(source, 0, 0, newW, newH);
            return bmp;
        }

        private void LoadKategoriler()
        {
            var kategoriler = _kategoriRepo.GetAll();
            cmbKategori.Items.Clear();
            cmbKategori.Items.Add("— Seçiniz —");
            foreach (var k in kategoriler) cmbKategori.Items.Add(k);
            cmbKategori.DisplayMember = "Ad";
            cmbKategori.SelectedIndex = 0;
        }

        private void LoadUrunler(string? search = null)
        {
            var urunler = string.IsNullOrWhiteSpace(search) ? _urunRepo.GetAll() : _urunRepo.Search(search);
            dgvUrunler.Rows.Clear();
            foreach (var u in urunler)
                dgvUrunler.Rows.Add(u.Id, u.Barkod ?? "—", u.Ad, u.KategoriAdi ?? "—", $"₺{u.AlisFiyati:N2}", $"₺{u.SatisFiyati:N2}", u.Stok, u.ResimYolu ?? "");
        }

        private void DgvUrunler_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvUrunler.Rows[e.RowIndex];
            _selectedUrunId = Convert.ToInt32(row.Cells["Id"].Value);
            txtBarkod.Text = row.Cells["Barkod"].Value?.ToString() == "—" ? "" : row.Cells["Barkod"].Value?.ToString();
            txtAd.Text = row.Cells["Ad"].Value?.ToString();
            txtAlisFiyati.Text = row.Cells["AlisFiyati"].Value?.ToString()?.Replace("₺", "").Trim();
            txtSatisFiyati.Text = row.Cells["SatisFiyati"].Value?.ToString()?.Replace("₺", "").Trim();
            txtStok.Text = row.Cells["Stok"].Value?.ToString();
            var kategoriAdi = row.Cells["Kategori"].Value?.ToString();
            for (int i = 1; i < cmbKategori.Items.Count; i++)
                if (cmbKategori.Items[i] is Kategori k && k.Ad == kategoriAdi) { cmbKategori.SelectedIndex = i; break; }

            // Load image
            _selectedResimYolu = row.Cells["ResimYolu"].Value?.ToString();
            if (string.IsNullOrEmpty(_selectedResimYolu)) _selectedResimYolu = null;
            LoadResimOnizleme(_selectedResimYolu);
        }

        private void BtnKaydet_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtAd.Text)) { MessageBox.Show("Ürün adı boş olamaz!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (!decimal.TryParse(txtSatisFiyati.Text.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var satisFiyati))
            { MessageBox.Show("Geçerli bir satış fiyatı girin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            decimal.TryParse(txtAlisFiyati.Text.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var alisFiyati);
            int.TryParse(txtStok.Text, out var stok);
            int? kategoriId = null;
            if (cmbKategori.SelectedIndex > 0 && cmbKategori.SelectedItem is Kategori selectedKategori) kategoriId = selectedKategori.Id;

            var urun = new Urun
            {
                Id = _selectedUrunId,
                Barkod = string.IsNullOrWhiteSpace(txtBarkod.Text) ? null : txtBarkod.Text.Trim(),
                Ad = txtAd.Text.Trim(),
                KategoriId = kategoriId,
                AlisFiyati = alisFiyati,
                SatisFiyati = satisFiyati,
                Stok = stok,
                ResimYolu = _selectedResimYolu
            };

            try
            {
                if (_selectedUrunId == 0)
                { _urunRepo.Add(urun); MessageBox.Show("Ürün eklendi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information); }
                else
                { _urunRepo.Update(urun); MessageBox.Show("Ürün güncellendi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information); }
                BtnYeni_Click(null, EventArgs.Empty); LoadUrunler();
            }
            catch (Exception ex) { MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void BtnYeni_Click(object? sender, EventArgs e)
        {
            _selectedUrunId = 0;
            txtAd.Clear(); txtBarkod.Clear(); txtAlisFiyati.Clear(); txtSatisFiyati.Clear();
            txtStok.Text = "0"; cmbKategori.SelectedIndex = 0; txtAd.Focus();
            ResimTemizle();
        }

        private void BtnSil_Click(object? sender, EventArgs e)
        {
            if (_selectedUrunId == 0) { MessageBox.Show("Silmek için bir ürün seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (MessageBox.Show($"'{txtAd.Text}' silinsin mi?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    // Delete image file too
                    if (!string.IsNullOrEmpty(_selectedResimYolu))
                    {
                        var resimPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _selectedResimYolu);
                        if (File.Exists(resimPath)) try { File.Delete(resimPath); } catch { }
                    }
                    _urunRepo.Delete(_selectedUrunId); BtnYeni_Click(null, EventArgs.Empty); LoadUrunler();
                }
                catch (Exception ex) { MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }
    }
}
