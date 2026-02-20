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
            this.Size = new Size(1100, 700);
            this.MinimumSize = new Size(950, 600);

            var header = Theme.CreateHeaderBar("Satış Raporları", Theme.AccentPurple);

            // ── Filter bar ──
            var filterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Theme.BgCard,
                Padding = new Padding(16, 10, 16, 10)
            };
            filterPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border);
                e.Graphics.DrawLine(pen, 0, filterPanel.Height - 1, filterPanel.Width, filterPanel.Height - 1);
            };

            var lblBas = Theme.CreateLabel("Başlangıç");
            lblBas.Location = new Point(16, 16);
            dtpBaslangic = new DateTimePicker { Location = new Point(85, 12), Size = new Size(150, 28), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
            var lblBit = Theme.CreateLabel("Bitiş");
            lblBit.Location = new Point(250, 16);
            dtpBitis = new DateTimePicker { Location = new Point(290, 12), Size = new Size(150, 28), Format = DateTimePickerFormat.Short, Value = DateTime.Today };

            var btnFiltre = Theme.CreateButton("Filtrele", Theme.AccentPurple, 90, 30);
            btnFiltre.Location = new Point(460, 10);
            btnFiltre.Font = Theme.FontBody;
            btnFiltre.Click += (s, e) => LoadData();

            var btnBugun = Theme.CreateButton("Bugün", Theme.BgInput, 70, 30);
            btnBugun.Location = new Point(560, 10);
            btnBugun.ForeColor = Theme.TextSecondary;
            btnBugun.Font = Theme.FontBody;
            btnBugun.Click += (s, e) => { dtpBaslangic.Value = DateTime.Today; dtpBitis.Value = DateTime.Today; LoadData(); };

            filterPanel.Controls.AddRange(new Control[] { lblBas, dtpBaslangic, lblBit, dtpBitis, btnFiltre, btnBugun });

            // ── Stats row ──
            var statsPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Theme.BgDark,
                Padding = new Padding(16, 10, 16, 10)
            };

            lblGunlukVal = CreateStatBox("Bugün", "₺0,00", Theme.AccentGreen, 16);
            lblDonemVal = CreateStatBox("Dönem Toplam", "₺0,00", Theme.AccentPurple, 230);
            lblSayiVal = CreateStatBox("Satış Sayısı", "0", Theme.AccentBlue, 444);

            statsPanel.Controls.AddRange(new Control[] { lblGunlukVal.Parent!, lblDonemVal.Parent!, lblSayiVal.Parent! });

            // ── Main split ──
            var mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                BackColor = Theme.BgDark,
                SplitterDistance = 520,
                SplitterWidth = 4
            };

            // Left: Sales list
            var leftPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            var lblSatislar = Theme.CreateLabel("Satış Listesi");
            lblSatislar.Font = Theme.FontBodyBold;
            lblSatislar.ForeColor = Theme.TextPrimary;
            lblSatislar.Dock = DockStyle.Top;
            lblSatislar.Height = 28;

            dgvSatislar = Theme.CreateGrid();
            dgvSatislar.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "Fiş No", Width = 65, MinimumWidth = 50, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvSatislar.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tarih", HeaderText = "Tarih", Width = 140, MinimumWidth = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvSatislar.Columns.Add(new DataGridViewTextBoxColumn { Name = "ToplamTutar", HeaderText = "Toplam", Width = 100, MinimumWidth = 80, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgvSatislar.Columns.Add(new DataGridViewTextBoxColumn { Name = "OdemeTipi", HeaderText = "Ödeme", Width = 85, MinimumWidth = 65 });
            dgvSatislar.CellClick += DgvSatislar_CellClick;

            leftPanel.Controls.Add(dgvSatislar);
            leftPanel.Controls.Add(lblSatislar);
            mainSplit.Panel1.Controls.Add(leftPanel);

            // Right: Details + Top selling
            var rightSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 240,
                BackColor = Theme.BgDark,
                SplitterWidth = 4
            };

            var detailPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            var lblDetay = Theme.CreateLabel("Satış Detayı");
            lblDetay.Font = Theme.FontBodyBold;
            lblDetay.ForeColor = Theme.TextPrimary;
            lblDetay.Dock = DockStyle.Top;
            lblDetay.Height = 28;

            dgvDetaylar = Theme.CreateGrid();
            dgvDetaylar.Columns.Add(new DataGridViewTextBoxColumn { Name = "UrunAdi", HeaderText = "Ürün", MinimumWidth = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvDetaylar.Columns.Add(new DataGridViewTextBoxColumn { Name = "Miktar", HeaderText = "Adet", Width = 55, MinimumWidth = 45, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvDetaylar.Columns.Add(new DataGridViewTextBoxColumn { Name = "BirimFiyat", HeaderText = "Birim ₺", Width = 85, MinimumWidth = 65, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgvDetaylar.Columns.Add(new DataGridViewTextBoxColumn { Name = "ToplamFiyat", HeaderText = "Toplam ₺", Width = 90, MinimumWidth = 70, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });

            detailPanel.Controls.Add(dgvDetaylar);
            detailPanel.Controls.Add(lblDetay);
            rightSplit.Panel1.Controls.Add(detailPanel);

            var topPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            var lblTop = Theme.CreateLabel("En Çok Satanlar");
            lblTop.Font = Theme.FontBodyBold;
            lblTop.ForeColor = Theme.AccentOrange;
            lblTop.Dock = DockStyle.Top;
            lblTop.Height = 28;

            dgvEnCokSatan = Theme.CreateGrid();
            dgvEnCokSatan.Columns.Add(new DataGridViewTextBoxColumn { Name = "UrunAdi", HeaderText = "Ürün", MinimumWidth = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvEnCokSatan.Columns.Add(new DataGridViewTextBoxColumn { Name = "ToplamMiktar", HeaderText = "Adet", Width = 60, MinimumWidth = 45, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            dgvEnCokSatan.Columns.Add(new DataGridViewTextBoxColumn { Name = "ToplamTutar", HeaderText = "Toplam ₺", Width = 100, MinimumWidth = 75, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });

            topPanel.Controls.Add(dgvEnCokSatan);
            topPanel.Controls.Add(lblTop);
            rightSplit.Panel2.Controls.Add(topPanel);

            mainSplit.Panel2.Controls.Add(rightSplit);

            this.Controls.Add(mainSplit);
            this.Controls.Add(statsPanel);
            this.Controls.Add(filterPanel);
            this.Controls.Add(header);
        }

        private Label CreateStatBox(string title, string value, Color accent, int x)
        {
            var panel = new Panel
            {
                Location = new Point(x, 4),
                Size = new Size(200, 55),
                BackColor = Theme.BgCard
            };
            panel.Paint += (s, e) =>
            {
                using var accentBrush = new SolidBrush(accent);
                e.Graphics.FillRectangle(accentBrush, 0, 0, 3, panel.Height);
                using var borderPen = new Pen(Theme.Border);
                e.Graphics.DrawRectangle(borderPen, 0, 0, panel.Width - 1, panel.Height - 1);
            };

            var lblTitle = new Label
            {
                Text = title,
                Location = new Point(14, 6),
                AutoSize = true,
                ForeColor = Theme.TextMuted,
                Font = Theme.FontSmall
            };

            var lblValue = new Label
            {
                Text = value,
                Location = new Point(14, 24),
                AutoSize = true,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Tag = title
            };

            panel.Controls.AddRange(new Control[] { lblTitle, lblValue });
            return lblValue;
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
