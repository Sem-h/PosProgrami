using System.Drawing.Drawing2D;
using PosProjesi.DataAccess;
using PosProjesi.UI;

namespace PosProjesi.Forms
{
    public class RaporForm : Form
    {
        private readonly SatisRepository _satisRepo = new();
        private DateTimePicker dtpBaslangic = null!;
        private DateTimePicker dtpBitis = null!;
        private DataGridView dgvSatislar = null!;
        private DataGridView dgvDetaylar = null!;
        private Label lblGunlukVal = null!;
        private Label lblDonemVal = null!;
        private Label lblSayiVal = null!;
        private DataGridView dgvEnCokSatan = null!;

        public RaporForm()
        {
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Theme.ApplyFormDefaults(this, "Verimek POS - Raporlar");
            this.WindowState = FormWindowState.Maximized;
            this.MinimumSize = new Size(950, 600);

            // ── Header with gradient ──
            var header = new Panel { Dock = DockStyle.Top, Height = 56 };
            header.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new LinearGradientBrush(
                    new Rectangle(0, 0, header.Width, header.Height),
                    Color.FromArgb(40, Theme.AccentPurple.R, Theme.AccentPurple.G, Theme.AccentPurple.B),
                    Theme.BgDark, LinearGradientMode.Horizontal);
                g.FillRectangle(brush, 0, 0, header.Width, header.Height);

                // Accent line
                using var accentBrush = new SolidBrush(Theme.AccentPurple);
                g.FillRectangle(accentBrush, 0, header.Height - 3, header.Width, 3);

                // Title
                using var titleFont = new Font("Segoe UI", 18, FontStyle.Bold);
                TextRenderer.DrawText(g, "📊  Satış Raporları", titleFont,
                    new Point(24, 12), Theme.TextPrimary, TextFormatFlags.NoPadding);
            };

            // ── Filter bar ──
            var filterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Theme.BgCard,
                Padding = new Padding(24, 14, 24, 14)
            };
            filterPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border);
                e.Graphics.DrawLine(pen, 0, filterPanel.Height - 1, filterPanel.Width, filterPanel.Height - 1);
            };

            var lblBas = new Label { Text = "Başlangıç", ForeColor = Theme.TextSecondary, Font = new Font("Segoe UI", 9.5f), Location = new Point(24, 20), AutoSize = true };
            dtpBaslangic = new DateTimePicker { Location = new Point(100, 16), Size = new Size(160, 30), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };

            var lblBit = new Label { Text = "Bitiş", ForeColor = Theme.TextSecondary, Font = new Font("Segoe UI", 9.5f), Location = new Point(280, 20), AutoSize = true };
            dtpBitis = new DateTimePicker { Location = new Point(320, 16), Size = new Size(160, 30), Format = DateTimePickerFormat.Short, Value = DateTime.Today };

            var btnFiltre = Theme.CreateButton("🔍 Filtrele", Theme.AccentPurple, 110, 32);
            btnFiltre.Location = new Point(506, 13);
            btnFiltre.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnFiltre.Click += (s, e) => LoadData();

            var btnBugun = Theme.CreateButton("📅 Bugün", Theme.BgInput, 100, 32);
            btnBugun.Location = new Point(626, 13);
            btnBugun.ForeColor = Theme.TextSecondary;
            btnBugun.Font = new Font("Segoe UI", 9.5f);
            btnBugun.Click += (s, e) => { dtpBaslangic.Value = DateTime.Today; dtpBitis.Value = DateTime.Today; LoadData(); };

            var btnHafta = Theme.CreateButton("📆 Bu Hafta", Theme.BgInput, 110, 32);
            btnHafta.Location = new Point(736, 13);
            btnHafta.ForeColor = Theme.TextSecondary;
            btnHafta.Font = new Font("Segoe UI", 9.5f);
            btnHafta.Click += (s, e) =>
            {
                var today = DateTime.Today;
                var diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                dtpBaslangic.Value = today.AddDays(-diff);
                dtpBitis.Value = today;
                LoadData();
            };

            var btnAy = Theme.CreateButton("🗓️ Bu Ay", Theme.BgInput, 100, 32);
            btnAy.Location = new Point(856, 13);
            btnAy.ForeColor = Theme.TextSecondary;
            btnAy.Font = new Font("Segoe UI", 9.5f);
            btnAy.Click += (s, e) =>
            {
                dtpBaslangic.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                dtpBitis.Value = DateTime.Today;
                LoadData();
            };

            filterPanel.Controls.AddRange(new Control[] { lblBas, dtpBaslangic, lblBit, dtpBitis, btnFiltre, btnBugun, btnHafta, btnAy });

            // ── Stats row ──
            var statsPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = Theme.BgDark,
                Padding = new Padding(20, 12, 20, 12)
            };
            statsPanel.Resize += (s, e) => ArrangeStatBoxes(statsPanel);

            lblGunlukVal = CreateStatBox("💰 Bugün", "₺0,00", Theme.AccentGreen, statsPanel);
            lblDonemVal = CreateStatBox("📈 Dönem Toplam", "₺0,00", Theme.AccentPurple, statsPanel);
            lblSayiVal = CreateStatBox("🧾 Satış Sayısı", "0", Theme.AccentBlue, statsPanel);

            // ── Main content ──
            var mainPanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgDark, Padding = new Padding(16, 12, 16, 12) };

            // Left panel: Sales list
            var leftCard = CreateSectionCard("📋 Satış Listesi", Theme.AccentPurple);
            leftCard.Dock = DockStyle.Fill;

            dgvSatislar = Theme.CreateGrid();
            dgvSatislar.Dock = DockStyle.Fill;
            dgvSatislar.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "Fiş No", Width = 70, MinimumWidth = 50, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvSatislar.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tarih", HeaderText = "Tarih", Width = 160, MinimumWidth = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvSatislar.Columns.Add(new DataGridViewTextBoxColumn { Name = "ToplamTutar", HeaderText = "Toplam ₺", Width = 110, MinimumWidth = 80, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgvSatislar.Columns.Add(new DataGridViewTextBoxColumn { Name = "OdemeTipi", HeaderText = "Ödeme", Width = 90, MinimumWidth = 65 });
            dgvSatislar.CellClick += DgvSatislar_CellClick;
            leftCard.Controls.Add(dgvSatislar);

            // Right panel: split between details & top sellers
            var rightPanel = new Panel { Dock = DockStyle.Right, Width = 420, Padding = new Padding(8, 0, 0, 0) };

            // Detail card
            var detailCard = CreateSectionCard("🔎 Satış Detayı", Theme.AccentBlue);
            detailCard.Dock = DockStyle.Fill;

            dgvDetaylar = Theme.CreateGrid();
            dgvDetaylar.Dock = DockStyle.Fill;
            dgvDetaylar.Columns.Add(new DataGridViewTextBoxColumn { Name = "UrunAdi", HeaderText = "Ürün", MinimumWidth = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvDetaylar.Columns.Add(new DataGridViewTextBoxColumn { Name = "Miktar", HeaderText = "Adet", Width = 60, MinimumWidth = 45, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvDetaylar.Columns.Add(new DataGridViewTextBoxColumn { Name = "BirimFiyat", HeaderText = "Birim ₺", Width = 85, MinimumWidth = 65, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgvDetaylar.Columns.Add(new DataGridViewTextBoxColumn { Name = "ToplamFiyat", HeaderText = "Toplam ₺", Width = 90, MinimumWidth = 70, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            detailCard.Controls.Add(dgvDetaylar);

            // Top sellers card
            var topCard = CreateSectionCard("🏆 En Çok Satanlar", Theme.AccentOrange);
            topCard.Dock = DockStyle.Bottom;
            topCard.Height = 240;

            dgvEnCokSatan = Theme.CreateGrid();
            dgvEnCokSatan.Dock = DockStyle.Fill;
            dgvEnCokSatan.Columns.Add(new DataGridViewTextBoxColumn { Name = "UrunAdi", HeaderText = "Ürün", MinimumWidth = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvEnCokSatan.Columns.Add(new DataGridViewTextBoxColumn { Name = "ToplamMiktar", HeaderText = "Adet", Width = 60, MinimumWidth = 45, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvEnCokSatan.Columns.Add(new DataGridViewTextBoxColumn { Name = "ToplamTutar", HeaderText = "Toplam ₺", Width = 100, MinimumWidth = 75, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            topCard.Controls.Add(dgvEnCokSatan);

            // Splitter
            var rightSplitter = new Splitter { Dock = DockStyle.Bottom, Height = 6, BackColor = Theme.BgDark };

            rightPanel.Controls.Add(detailCard);
            rightPanel.Controls.Add(rightSplitter);
            rightPanel.Controls.Add(topCard);

            // Splitter for left/right
            var mainSplitter = new Splitter { Dock = DockStyle.Right, Width = 6, BackColor = Theme.BgDark };

            mainPanel.Controls.Add(leftCard);
            mainPanel.Controls.Add(mainSplitter);
            mainPanel.Controls.Add(rightPanel);

            // Add everything
            this.Controls.Add(mainPanel);
            this.Controls.Add(statsPanel);
            this.Controls.Add(filterPanel);
            this.Controls.Add(header);
        }

        private Panel CreateSectionCard(string title, Color accent)
        {
            var card = new Panel { BackColor = Theme.BgCard, Padding = new Padding(1) };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                // Border
                using var borderPen = new Pen(Theme.Border);
                g.DrawRectangle(borderPen, 0, 0, card.Width - 1, card.Height - 1);
                // Top accent
                using var accentBrush = new SolidBrush(accent);
                g.FillRectangle(accentBrush, 0, 0, card.Width, 3);
            };

            var titleBar = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Color.FromArgb(20, accent.R, accent.G, accent.B) };
            titleBar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using var font = new Font("Segoe UI", 11, FontStyle.Bold);
                TextRenderer.DrawText(g, title, font, new Point(12, 8), Theme.TextPrimary, TextFormatFlags.NoPadding);
                using var pen = new Pen(Theme.Border);
                g.DrawLine(pen, 0, titleBar.Height - 1, titleBar.Width, titleBar.Height - 1);
            };

            card.Controls.Add(titleBar);
            return card;
        }

        private Label CreateStatBox(string title, string value, Color accent, Panel parent)
        {
            var panel = new Panel
            {
                Size = new Size(240, 66),
                BackColor = Theme.BgCard
            };
            panel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                // Border
                using var borderPen = new Pen(Theme.Border);
                g.DrawRectangle(borderPen, 0, 0, panel.Width - 1, panel.Height - 1);
                // Left accent
                using var accentBrush = new SolidBrush(accent);
                g.FillRectangle(accentBrush, 0, 0, 4, panel.Height);
                // Subtle gradient bg
                using var gradBrush = new LinearGradientBrush(
                    new Rectangle(0, 0, panel.Width, panel.Height),
                    Color.FromArgb(15, accent.R, accent.G, accent.B),
                    Theme.BgCard, LinearGradientMode.Horizontal);
                g.FillRectangle(gradBrush, 4, 1, panel.Width - 5, panel.Height - 2);
            };

            var lblTitle = new Label
            {
                Text = title,
                Location = new Point(16, 8),
                AutoSize = true,
                ForeColor = Theme.TextMuted,
                Font = new Font("Segoe UI", 9)
            };

            var lblValue = new Label
            {
                Text = value,
                Location = new Point(16, 30),
                AutoSize = true,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                Tag = title
            };

            panel.Controls.AddRange(new Control[] { lblTitle, lblValue });
            parent.Controls.Add(panel);
            return lblValue;
        }

        private void ArrangeStatBoxes(Panel container)
        {
            int gap = 20;
            int count = container.Controls.Count;
            if (count == 0) return;
            int boxWidth = (container.Width - container.Padding.Horizontal - (count - 1) * gap) / count;
            int x = container.Padding.Left;
            int y = container.Padding.Top;
            foreach (Control c in container.Controls)
            {
                c.Location = new Point(x, y);
                c.Size = new Size(boxWidth, container.Height - container.Padding.Vertical);
                x += boxWidth + gap;
            }
        }

        private void LoadData()
        {
            var baslangic = dtpBaslangic.Value.Date;
            var bitis = dtpBitis.Value.Date;

            var satislar = _satisRepo.GetSatislar(baslangic, bitis);
            dgvSatislar.Rows.Clear();
            foreach (var s in satislar)
                dgvSatislar.Rows.Add(s.Id, s.SatisTarihi, $"₺{s.ToplamTutar:N2}", s.OdemeTipi);

            var gunlukToplam = _satisRepo.GetGunlukToplam(DateTime.Today);
            var donemToplam = satislar.Sum(s => s.ToplamTutar);

            lblGunlukVal.Text = $"₺{gunlukToplam:N2}";
            lblDonemVal.Text = $"₺{donemToplam:N2}";
            lblSayiVal.Text = satislar.Count.ToString();

            var enCokSatan = _satisRepo.GetEnCokSatanUrunler(10, baslangic, bitis);
            dgvEnCokSatan.Rows.Clear();
            foreach (var item in enCokSatan)
                dgvEnCokSatan.Rows.Add(item.UrunAdi, item.ToplamMiktar, $"₺{item.ToplamTutar:N2}");

            dgvDetaylar.Rows.Clear();
        }

        private void DgvSatislar_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var satisId = Convert.ToInt32(dgvSatislar.Rows[e.RowIndex].Cells["Id"].Value);
            var detaylar = _satisRepo.GetSatisDetaylari(satisId);
            dgvDetaylar.Rows.Clear();
            foreach (var d in detaylar)
                dgvDetaylar.Rows.Add(d.UrunAdi, d.Miktar, $"₺{d.BirimFiyat:N2}", $"₺{d.ToplamFiyat:N2}");
        }
    }
}
