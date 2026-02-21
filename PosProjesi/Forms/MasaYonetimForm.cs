using PosProjesi.DataAccess;
using PosProjesi.Models;
using PosProjesi.UI;

namespace PosProjesi.Forms
{
    public class MasaYonetimForm : Form
    {
        private readonly MasaRepository _masaRepo = new();

        // Left: Kategori
        private ListBox lstKategoriler = null!;
        private TextBox txtKategoriAd = null!;
        private int _selectedKategoriId = 0;

        // Right: Masalar
        private DataGridView dgvMasalar = null!;
        private TextBox txtMasaAd = null!;
        private ComboBox cmbMasaKategori = null!;
        private int _selectedMasaId = 0;

        public MasaYonetimForm()
        {
            InitializeComponent();
            LoadKategoriler();
            LoadMasalar();
        }

        private void InitializeComponent()
        {
            Theme.ApplyFormDefaults(this, "Verimek POS - Masa Yönetimi");
            this.Size = new Size(900, 550);
            this.MinimumSize = new Size(750, 450);

            var header = Theme.CreateHeaderBar("Masa Yönetimi", Theme.AccentTeal);

            // ── LEFT PANEL: Kategoriler ──
            var leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 300,
                BackColor = Theme.BgCard,
                Padding = new Padding(16, 12, 16, 12)
            };
            var leftSep = new Panel { Dock = DockStyle.Right, Width = 1, BackColor = Theme.Border };
            leftPanel.Controls.Add(leftSep);

            int y = 12;
            var lblKatTitle = new Label
            {
                Text = "📂  MASA KATEGORİLERİ",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(16, y),
                AutoSize = true
            };
            leftPanel.Controls.Add(lblKatTitle);
            y += 30;

            var lblKatAd = Theme.CreateLabel("Kategori Adı");
            lblKatAd.Location = new Point(16, y);
            leftPanel.Controls.Add(lblKatAd);
            y += 22;

            txtKategoriAd = Theme.CreateTextBox(240);
            txtKategoriAd.Location = new Point(16, y);
            leftPanel.Controls.Add(txtKategoriAd);
            y += 38;

            var btnKatEkle = Theme.CreateButton("Ekle", Theme.AccentGreen, 75, 32);
            btnKatEkle.Location = new Point(16, y);
            btnKatEkle.Click += BtnKatEkle_Click;

            var btnKatGuncelle = Theme.CreateButton("Güncelle", Theme.AccentBlue, 80, 32);
            btnKatGuncelle.Location = new Point(96, y);
            btnKatGuncelle.Click += BtnKatGuncelle_Click;

            var btnKatSil = Theme.CreateButton("Sil", Theme.AccentRed, 75, 32);
            btnKatSil.Location = new Point(181, y);
            btnKatSil.Click += BtnKatSil_Click;

            leftPanel.Controls.AddRange(new Control[] { btnKatEkle, btnKatGuncelle, btnKatSil });
            y += 42;

            lstKategoriler = new ListBox
            {
                Location = new Point(16, y),
                Size = new Size(240, 300),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextPrimary,
                Font = Theme.FontBody,
                BorderStyle = BorderStyle.None
            };
            lstKategoriler.SelectedIndexChanged += LstKategoriler_SelectedIndexChanged;
            leftPanel.Controls.Add(lstKategoriler);

            // ── RIGHT PANEL: Masalar ──
            var rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 12, 16, 12)
            };

            int ry = 12;
            var lblMasaTitle = new Label
            {
                Text = "🍽️  MASALAR",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(16, ry),
                AutoSize = true
            };
            rightPanel.Controls.Add(lblMasaTitle);
            ry += 30;

            // Masa form fields
            var lblMasaAd = Theme.CreateLabel("Masa Adı");
            lblMasaAd.Location = new Point(16, ry);
            rightPanel.Controls.Add(lblMasaAd);

            var lblMasaKat = Theme.CreateLabel("Kategori");
            lblMasaKat.Location = new Point(200, ry);
            rightPanel.Controls.Add(lblMasaKat);
            ry += 22;

            txtMasaAd = Theme.CreateTextBox(170);
            txtMasaAd.Location = new Point(16, ry);
            rightPanel.Controls.Add(txtMasaAd);

            cmbMasaKategori = new ComboBox
            {
                Location = new Point(200, ry),
                Size = new Size(180, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextPrimary,
                Font = Theme.FontBody,
                FlatStyle = FlatStyle.Flat
            };
            rightPanel.Controls.Add(cmbMasaKategori);
            ry += 38;

            var btnMasaEkle = Theme.CreateButton("Ekle", Theme.AccentGreen, 80, 32);
            btnMasaEkle.Location = new Point(16, ry);
            btnMasaEkle.Click += BtnMasaEkle_Click;

            var btnMasaGuncelle = Theme.CreateButton("Güncelle", Theme.AccentBlue, 90, 32);
            btnMasaGuncelle.Location = new Point(101, ry);
            btnMasaGuncelle.Click += BtnMasaGuncelle_Click;

            var btnMasaSil = Theme.CreateButton("Sil", Theme.AccentRed, 80, 32);
            btnMasaSil.Location = new Point(196, ry);
            btnMasaSil.Click += BtnMasaSil_Click;

            var btnMasaYeni = Theme.CreateButton("Yeni", Theme.BgInput, 80, 32);
            btnMasaYeni.ForeColor = Theme.TextSecondary;
            btnMasaYeni.Location = new Point(281, ry);
            btnMasaYeni.Click += (s, e) => { _selectedMasaId = 0; txtMasaAd.Clear(); cmbMasaKategori.SelectedIndex = 0; };

            rightPanel.Controls.AddRange(new Control[] { btnMasaEkle, btnMasaGuncelle, btnMasaSil, btnMasaYeni });
            ry += 42;

            dgvMasalar = Theme.CreateGrid();
            dgvMasalar.Location = new Point(16, ry);
            dgvMasalar.Size = new Size(540, 300);
            dgvMasalar.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            dgvMasalar.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Width = 50 });
            dgvMasalar.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ad", HeaderText = "Masa Adı", Width = 150, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvMasalar.Columns.Add(new DataGridViewTextBoxColumn { Name = "Kategori", HeaderText = "Kategori", Width = 120 });
            dgvMasalar.Columns.Add(new DataGridViewTextBoxColumn { Name = "Durum", HeaderText = "Durum", Width = 80 });
            dgvMasalar.CellClick += DgvMasalar_CellClick;
            rightPanel.Controls.Add(dgvMasalar);

            this.Controls.Add(rightPanel);
            this.Controls.Add(leftPanel);
            this.Controls.Add(header);
        }

        // ─── Kategori İşlemleri ──────────────────────

        private void LoadKategoriler()
        {
            var kategoriler = _masaRepo.GetKategoriler();
            lstKategoriler.Items.Clear();
            foreach (var k in kategoriler)
                lstKategoriler.Items.Add(k);
            lstKategoriler.DisplayMember = "Ad";

            // Also refresh combo
            cmbMasaKategori.Items.Clear();
            cmbMasaKategori.Items.Add("— Seçiniz —");
            foreach (var k in kategoriler) cmbMasaKategori.Items.Add(k);
            cmbMasaKategori.DisplayMember = "Ad";
            cmbMasaKategori.SelectedIndex = 0;
        }

        private void LstKategoriler_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (lstKategoriler.SelectedItem is MasaKategori k)
            {
                _selectedKategoriId = k.Id;
                txtKategoriAd.Text = k.Ad;
            }
        }

        private void BtnKatEkle_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtKategoriAd.Text))
            { MessageBox.Show("Kategori adı boş olamaz!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            _masaRepo.AddKategori(new MasaKategori { Ad = txtKategoriAd.Text.Trim() });
            txtKategoriAd.Clear(); _selectedKategoriId = 0;
            LoadKategoriler(); LoadMasalar();
        }

        private void BtnKatGuncelle_Click(object? sender, EventArgs e)
        {
            if (_selectedKategoriId == 0 || string.IsNullOrWhiteSpace(txtKategoriAd.Text))
            { MessageBox.Show("Bir kategori seçin ve ad girin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            _masaRepo.UpdateKategori(new MasaKategori { Id = _selectedKategoriId, Ad = txtKategoriAd.Text.Trim() });
            txtKategoriAd.Clear(); _selectedKategoriId = 0;
            LoadKategoriler(); LoadMasalar();
        }

        private void BtnKatSil_Click(object? sender, EventArgs e)
        {
            if (_selectedKategoriId == 0) { MessageBox.Show("Silmek için kategori seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (MessageBox.Show($"'{txtKategoriAd.Text}' kategorisi silinsin mi?\n\nBu kategorideki masalar da silinecektir!", "Onay",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                // Delete tables in this category first
                var masalar = _masaRepo.GetByKategori(_selectedKategoriId);
                foreach (var m in masalar) _masaRepo.Delete(m.Id);
                _masaRepo.DeleteKategori(_selectedKategoriId);
                txtKategoriAd.Clear(); _selectedKategoriId = 0;
                LoadKategoriler(); LoadMasalar();
            }
        }

        // ─── Masa İşlemleri ──────────────────────────

        private void LoadMasalar()
        {
            var masalar = _masaRepo.GetAll();
            dgvMasalar.Rows.Clear();
            foreach (var m in masalar)
                dgvMasalar.Rows.Add(m.Id, m.Ad, m.MasaKategoriAdi ?? "—", m.Durum);
        }

        private void DgvMasalar_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvMasalar.Rows[e.RowIndex];
            _selectedMasaId = Convert.ToInt32(row.Cells["Id"].Value);
            txtMasaAd.Text = row.Cells["Ad"].Value?.ToString();

            var kategoriAdi = row.Cells["Kategori"].Value?.ToString();
            for (int i = 1; i < cmbMasaKategori.Items.Count; i++)
                if (cmbMasaKategori.Items[i] is MasaKategori k && k.Ad == kategoriAdi)
                { cmbMasaKategori.SelectedIndex = i; break; }
        }

        private void BtnMasaEkle_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMasaAd.Text))
            { MessageBox.Show("Masa adı boş olamaz!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (cmbMasaKategori.SelectedIndex <= 0)
            { MessageBox.Show("Bir kategori seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            var kategori = (MasaKategori)cmbMasaKategori.SelectedItem!;
            _masaRepo.Add(new Masa { Ad = txtMasaAd.Text.Trim(), MasaKategoriId = kategori.Id });
            txtMasaAd.Clear(); cmbMasaKategori.SelectedIndex = 0; _selectedMasaId = 0;
            LoadMasalar();
        }

        private void BtnMasaGuncelle_Click(object? sender, EventArgs e)
        {
            if (_selectedMasaId == 0 || string.IsNullOrWhiteSpace(txtMasaAd.Text))
            { MessageBox.Show("Bir masa seçin ve ad girin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (cmbMasaKategori.SelectedIndex <= 0)
            { MessageBox.Show("Bir kategori seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            var kategori = (MasaKategori)cmbMasaKategori.SelectedItem!;
            _masaRepo.Update(new Masa { Id = _selectedMasaId, Ad = txtMasaAd.Text.Trim(), MasaKategoriId = kategori.Id });
            txtMasaAd.Clear(); cmbMasaKategori.SelectedIndex = 0; _selectedMasaId = 0;
            LoadMasalar();
        }

        private void BtnMasaSil_Click(object? sender, EventArgs e)
        {
            if (_selectedMasaId == 0) { MessageBox.Show("Silmek için masa seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (MessageBox.Show($"'{txtMasaAd.Text}' silinsin mi?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _masaRepo.Delete(_selectedMasaId);
                txtMasaAd.Clear(); cmbMasaKategori.SelectedIndex = 0; _selectedMasaId = 0;
                LoadMasalar();
            }
        }
    }
}
