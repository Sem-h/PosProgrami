using PosProjesi.DataAccess;
using PosProjesi.Models;
using PosProjesi.UI;

namespace PosProjesi.Forms
{
    public class KategoriYonetimForm : Form
    {
        private readonly KategoriRepository _kategoriRepo = new();
        private DataGridView dgvKategoriler = null!;
        private TextBox txtAd = null!;
        private int _selectedId = 0;

        public KategoriYonetimForm()
        {
            InitializeComponent();
            LoadKategoriler();
        }

        private void InitializeComponent()
        {
            Theme.ApplyFormDefaults(this, "Verimek POS - Kategoriler");
            this.Size = new Size(500, 450);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var header = Theme.CreateHeaderBar("Kategori Yönetimi", Theme.AccentTeal);

            // Input area
            var inputPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Theme.BgCard,
                Padding = new Padding(16, 10, 16, 10)
            };
            inputPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border);
                e.Graphics.DrawLine(pen, 0, inputPanel.Height - 1, inputPanel.Width, inputPanel.Height - 1);
            };

            var lblAd = Theme.CreateLabel("Ad:");
            lblAd.Location = new Point(16, 18);

            txtAd = Theme.CreateTextBox(200);
            txtAd.Location = new Point(45, 14);

            var btnKaydet = Theme.CreateButton("Kaydet", Theme.AccentTeal, 80, 30);
            btnKaydet.Location = new Point(260, 13);
            btnKaydet.Font = Theme.FontBody;
            btnKaydet.Click += BtnKaydet_Click;

            var btnSil = Theme.CreateButton("Sil", Theme.AccentRed, 65, 30);
            btnSil.Location = new Point(348, 13);
            btnSil.Font = Theme.FontBody;
            btnSil.Click += BtnSil_Click;

            inputPanel.Controls.AddRange(new Control[] { lblAd, txtAd, btnKaydet, btnSil });

            dgvKategoriler = Theme.CreateGrid();
            dgvKategoriler.Columns.Add("Id", "ID");
            dgvKategoriler.Columns.Add("Ad", "Kategori Adı");
            dgvKategoriler.Columns["Id"]!.Width = 60;
            dgvKategoriler.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                _selectedId = Convert.ToInt32(dgvKategoriler.Rows[e.RowIndex].Cells["Id"].Value);
                txtAd.Text = dgvKategoriler.Rows[e.RowIndex].Cells["Ad"].Value?.ToString();
            };

            this.Controls.Add(dgvKategoriler);
            this.Controls.Add(inputPanel);
            this.Controls.Add(header);
        }

        private void LoadKategoriler()
        {
            var kategoriler = _kategoriRepo.GetAll();
            dgvKategoriler.Rows.Clear();
            foreach (var k in kategoriler) dgvKategoriler.Rows.Add(k.Id, k.Ad);
        }

        private void BtnKaydet_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtAd.Text)) { MessageBox.Show("Ad boş olamaz!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            try
            {
                if (_selectedId == 0) _kategoriRepo.Add(new Kategori { Ad = txtAd.Text.Trim() });
                else _kategoriRepo.Update(new Kategori { Id = _selectedId, Ad = txtAd.Text.Trim() });
                _selectedId = 0; txtAd.Clear(); LoadKategoriler();
            }
            catch (Exception ex) { MessageBox.Show($"Hata: {ex.Message}"); }
        }

        private void BtnSil_Click(object? sender, EventArgs e)
        {
            if (_selectedId == 0) { MessageBox.Show("Bir kategori seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (MessageBox.Show($"'{txtAd.Text}' silinsin mi?", "Onay", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try { _kategoriRepo.Delete(_selectedId); _selectedId = 0; txtAd.Clear(); LoadKategoriler(); }
                catch (Exception ex) { MessageBox.Show($"Hata: {ex.Message}"); }
            }
        }
    }
}
