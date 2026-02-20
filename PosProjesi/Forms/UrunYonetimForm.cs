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
        private int _selectedUrunId = 0;

        public UrunYonetimForm()
        {
            InitializeComponent();
            LoadKategoriler();
            LoadUrunler();
        }

        private void InitializeComponent()
        {
            Theme.ApplyFormDefaults(this, "Verimek POS - Ürün Yönetimi");
            this.Size = new Size(1050, 650);
            this.MinimumSize = new Size(900, 550);

            var header = Theme.CreateHeaderBar("Ürün Yönetimi", Theme.AccentBlue);

            // ── LEFT: Form ──
            var leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 310,
                BackColor = Theme.BgCard,
                Padding = new Padding(20, 16, 20, 16)
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
            dgvUrunler.CellClick += DgvUrunler_CellClick;

            rightPanel.Controls.Add(dgvUrunler);
            rightPanel.Controls.Add(searchPanel);

            this.Controls.Add(rightPanel);
            this.Controls.Add(leftPanel);
            this.Controls.Add(header);
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
                dgvUrunler.Rows.Add(u.Id, u.Barkod ?? "—", u.Ad, u.KategoriAdi ?? "—", $"₺{u.AlisFiyati:N2}", $"₺{u.SatisFiyati:N2}", u.Stok);
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

            var urun = new Urun { Id = _selectedUrunId, Barkod = string.IsNullOrWhiteSpace(txtBarkod.Text) ? null : txtBarkod.Text.Trim(), Ad = txtAd.Text.Trim(), KategoriId = kategoriId, AlisFiyati = alisFiyati, SatisFiyati = satisFiyati, Stok = stok };

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
        }

        private void BtnSil_Click(object? sender, EventArgs e)
        {
            if (_selectedUrunId == 0) { MessageBox.Show("Silmek için bir ürün seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (MessageBox.Show($"'{txtAd.Text}' silinsin mi?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try { _urunRepo.Delete(_selectedUrunId); BtnYeni_Click(null, EventArgs.Empty); LoadUrunler(); }
                catch (Exception ex) { MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }
    }
}
