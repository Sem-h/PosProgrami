using System.Drawing.Drawing2D;
using System.Drawing.Text;
using PosProjesi.DataAccess;
using PosProjesi.Models;
using PosProjesi.UI;

namespace PosProjesi.Forms
{
    public class PersonelYonetimForm : Form
    {
        private readonly PersonelRepository _repo = new();
        private DataGridView _dgv = null!;
        private TextBox _txtAd = null!, _txtSoyad = null!, _txtSifre = null!;
        private int? _editingId = null;

        public PersonelYonetimForm()
        {
            this.Text = "👤 Personel Yönetimi";
            this.Size = new Size(750, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.BgDark;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Font = new Font("Segoe UI", 10);

            InitUI();
            LoadData();
        }

        private void InitUI()
        {
            // ── Header ──
            var header = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.Transparent };
            header.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using var bgBrush = new LinearGradientBrush(
                    new Rectangle(0, 0, header.Width, header.Height),
                    Color.FromArgb(30, 32, 50), Theme.BgDark, LinearGradientMode.Vertical);
                g.FillRectangle(bgBrush, 0, 0, header.Width, header.Height);

                using var accentBrush = new LinearGradientBrush(
                    new Rectangle(0, 0, header.Width, 3),
                    Theme.AccentBlue, Theme.AccentTeal, LinearGradientMode.Horizontal);
                g.FillRectangle(accentBrush, 0, 0, header.Width, 3);

                using var titleFont = new Font("Segoe UI", 16, FontStyle.Bold);
                TextRenderer.DrawText(g, "👤 Personel Yönetimi", titleFont, new Point(20, 16), Theme.TextPrimary, TextFormatFlags.NoPadding);
            };

            // ── Input panel ──
            var inputPanel = new Panel { Dock = DockStyle.Top, Height = 70, Padding = new Padding(20, 10, 20, 10), BackColor = Color.FromArgb(24, 26, 40) };

            var lblAd = new Label { Text = "Ad:", ForeColor = Theme.TextMuted, Location = new Point(20, 18), AutoSize = true };
            _txtAd = new TextBox { Location = new Point(45, 14), Width = 140, BackColor = Theme.BgInput, ForeColor = Theme.TextPrimary, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };

            var lblSoyad = new Label { Text = "Soyad:", ForeColor = Theme.TextMuted, Location = new Point(200, 18), AutoSize = true };
            _txtSoyad = new TextBox { Location = new Point(255, 14), Width = 140, BackColor = Theme.BgInput, ForeColor = Theme.TextPrimary, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };

            var lblSifre = new Label { Text = "Şifre:", ForeColor = Theme.TextMuted, Location = new Point(410, 18), AutoSize = true };
            _txtSifre = new TextBox { Location = new Point(460, 14), Width = 100, BackColor = Theme.BgInput, ForeColor = Theme.TextPrimary, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };

            var btnKaydet = new Button
            {
                Text = "💾 Kaydet",
                Location = new Point(580, 12),
                Size = new Size(120, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.AccentBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnKaydet.FlatAppearance.BorderSize = 0;
            btnKaydet.Click += BtnKaydet_Click;

            inputPanel.Controls.AddRange(new Control[] { lblAd, _txtAd, lblSoyad, _txtSoyad, lblSifre, _txtSifre, btnKaydet });

            // ── DataGridView ──
            _dgv = Theme.CreateGrid();
            _dgv.Dock = DockStyle.Fill;
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Width = 50 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ad", HeaderText = "Ad", Width = 150 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Soyad", HeaderText = "Soyad", Width = 150 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sifre", HeaderText = "Şifre", Width = 100 });
            _dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tarih", HeaderText = "Kayıt Tarihi", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

            // Action buttons column
            var btnEditCol = new DataGridViewButtonColumn { Name = "Duzenle", HeaderText = "", Text = "✏️ Düzenle", UseColumnTextForButtonValue = true, Width = 90 };
            var btnDelCol = new DataGridViewButtonColumn { Name = "Sil", HeaderText = "", Text = "🗑️ Sil", UseColumnTextForButtonValue = true, Width = 70 };
            _dgv.Columns.Add(btnEditCol);
            _dgv.Columns.Add(btnDelCol);

            _dgv.CellClick += Dgv_CellClick;

            // ── Bottom panel ──
            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 50, BackColor = Color.FromArgb(24, 26, 40) };
            var lblInfo = new Label
            {
                Text = "💡 Varsayılan şifre: 1234",
                ForeColor = Theme.TextMuted,
                Font = new Font("Segoe UI", 9),
                Location = new Point(20, 16),
                AutoSize = true
            };
            bottomPanel.Controls.Add(lblInfo);

            this.Controls.Add(_dgv);
            this.Controls.Add(inputPanel);
            this.Controls.Add(header);
            this.Controls.Add(bottomPanel);
        }

        private void LoadData()
        {
            _dgv.Rows.Clear();
            var personeller = _repo.GetAll();
            foreach (var p in personeller)
            {
                _dgv.Rows.Add(p.Id, p.Ad, p.Soyad, p.Sifre, p.OlusturmaTarihi);
            }
        }

        private void BtnKaydet_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtAd.Text) || string.IsNullOrWhiteSpace(_txtSoyad.Text))
            {
                MessageBox.Show("Ad ve Soyad alanları zorunludur!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var sifre = string.IsNullOrWhiteSpace(_txtSifre.Text) ? "1234" : _txtSifre.Text;

            if (_editingId.HasValue)
            {
                _repo.Update(new Personel { Id = _editingId.Value, Ad = _txtAd.Text.Trim(), Soyad = _txtSoyad.Text.Trim(), Sifre = sifre });
                _editingId = null;
            }
            else
            {
                _repo.Add(new Personel { Ad = _txtAd.Text.Trim(), Soyad = _txtSoyad.Text.Trim(), Sifre = sifre });
            }

            _txtAd.Clear(); _txtSoyad.Clear(); _txtSifre.Clear();
            LoadData();
        }

        private void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = _dgv.Rows[e.RowIndex];
            int id = Convert.ToInt32(row.Cells["Id"].Value);

            if (e.ColumnIndex == _dgv.Columns["Duzenle"]!.Index)
            {
                _editingId = id;
                _txtAd.Text = row.Cells["Ad"].Value?.ToString();
                _txtSoyad.Text = row.Cells["Soyad"].Value?.ToString();
                _txtSifre.Text = row.Cells["Sifre"].Value?.ToString();
                _txtAd.Focus();
            }
            else if (e.ColumnIndex == _dgv.Columns["Sil"]!.Index)
            {
                if (MessageBox.Show("Bu personeli silmek istediğinize emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _repo.Delete(id);
                    LoadData();
                }
            }
        }
    }
}
