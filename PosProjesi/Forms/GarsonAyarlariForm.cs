using PosProjesi.DataAccess;
using PosProjesi.Services;
using PosProjesi.UI;

namespace PosProjesi.Forms
{
    public class GarsonAyarlariForm : Form
    {
        private readonly AyarlarRepository _ayarRepo = new();
        private CheckBox chkAktif = null!;
        private TextBox txtPort = null!;
        private TextBox txtPin = null!;
        private Label lblDurum = null!;
        private Label lblIP = null!;

        public GarsonAyarlariForm()
        {
            InitializeComponent();
            LoadAyarlar();
        }

        private void InitializeComponent()
        {
            Theme.ApplyFormDefaults(this, "Verimek POS - Garson Sipariş Ayarları");
            this.Size = new Size(480, 440);
            this.MinimumSize = new Size(420, 400);
            this.StartPosition = FormStartPosition.CenterParent;

            var header = Theme.CreateHeaderBar("📱  Garson Sipariş Sistemi", Theme.AccentGreen);

            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 24, 28, 20),
                BackColor = Theme.BgDark
            };

            int y = 0;

            // Active checkbox
            chkAktif = new CheckBox
            {
                Text = "  Garson sipariş sistemini aktifleştir",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(0, y),
                AutoSize = true,
                Checked = false
            };
            contentPanel.Controls.Add(chkAktif);
            y += 40;

            // Port
            var lblPort = new Label
            {
                Text = "Port Numarası:",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextSecondary,
                Location = new Point(0, y),
                AutoSize = true
            };
            contentPanel.Controls.Add(lblPort);
            y += 22;

            txtPort = Theme.CreateTextBox(100);
            txtPort.Location = new Point(0, y);
            txtPort.Size = new Size(100, 28);
            txtPort.Font = new Font("Segoe UI", 10);
            txtPort.Text = "5555";
            contentPanel.Controls.Add(txtPort);
            y += 38;

            // PIN
            var lblPinTitle = new Label
            {
                Text = "Garson Giriş PIN Kodu:",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextSecondary,
                Location = new Point(0, y),
                AutoSize = true
            };
            contentPanel.Controls.Add(lblPinTitle);
            y += 22;

            txtPin = Theme.CreateTextBox(150);
            txtPin.Location = new Point(0, y);
            txtPin.Size = new Size(150, 28);
            txtPin.Font = new Font("Segoe UI", 10);
            txtPin.Text = "1234";
            contentPanel.Controls.Add(txtPin);
            y += 44;

            // Separator
            var sep = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(400, 1),
                BackColor = Theme.Border
            };
            contentPanel.Controls.Add(sep);
            y += 16;

            // Status
            var lblInfo = new Label
            {
                Text = "BAĞLANTI BİLGİSİ",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Theme.AccentBlue,
                Location = new Point(0, y),
                AutoSize = true
            };
            contentPanel.Controls.Add(lblInfo);
            y += 24;

            var ip = GarsonWebServer.GetLocalIP();
            lblIP = new Label
            {
                Text = $"Garsonlar şu adresi açmalı:  http://{ip}:5555",
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.AccentGreen,
                Location = new Point(0, y),
                AutoSize = true
            };
            contentPanel.Controls.Add(lblIP);
            y += 24;

            lblDurum = new Label
            {
                Text = "Durum: Devre dışı",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextMuted,
                Location = new Point(0, y),
                AutoSize = true
            };
            contentPanel.Controls.Add(lblDurum);

            // Update IP label when port changes
            txtPort.TextChanged += (s, e) =>
            {
                lblIP.Text = $"Garsonlar şu adresi açmalı:  http://{ip}:{txtPort.Text.Trim()}";
            };

            // Bottom buttons
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Theme.BgCard,
                Padding = new Padding(20, 10, 20, 10)
            };
            bottomPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border);
                e.Graphics.DrawLine(pen, 0, 0, bottomPanel.Width, 0);
            };

            var btnKaydet = Theme.CreateButton("💾  Kaydet", Theme.AccentGreen, 160, 38);
            btnKaydet.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btnKaydet.Dock = DockStyle.Right;
            btnKaydet.Click += BtnKaydet_Click;

            var btnIptal = Theme.CreateButton("İptal", Theme.BgInput, 100, 38);
            btnIptal.ForeColor = Theme.TextSecondary;
            btnIptal.Dock = DockStyle.Left;
            btnIptal.Click += (s, e) => this.Close();

            bottomPanel.Controls.Add(btnKaydet);
            bottomPanel.Controls.Add(btnIptal);

            this.Controls.Add(contentPanel);
            this.Controls.Add(bottomPanel);
            this.Controls.Add(header);
        }

        private void LoadAyarlar()
        {
            chkAktif.Checked = _ayarRepo.GetAyar(AyarlarRepository.GarsonAktif, "0") == "1";
            txtPort.Text = _ayarRepo.GetAyar(AyarlarRepository.GarsonPort, "5555");
            txtPin.Text = _ayarRepo.GetAyar(AyarlarRepository.GarsonPin, "1234");

            UpdateDurum();
        }

        private void UpdateDurum()
        {
            var aktif = _ayarRepo.GetAyar(AyarlarRepository.GarsonAktif, "0") == "1";
            lblDurum.Text = aktif ? "Durum: ✅ Aktif" : "Durum: ❌ Devre dışı";
            lblDurum.ForeColor = aktif ? Theme.AccentGreen : Theme.TextMuted;
        }

        private void BtnKaydet_Click(object? sender, EventArgs e)
        {
            if (!int.TryParse(txtPort.Text.Trim(), out var port) || port < 1000 || port > 65535)
            {
                MessageBox.Show("Port numarası 1000-65535 arasında olmalıdır.", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPin.Text))
            {
                MessageBox.Show("PIN kodu boş olamaz.", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _ayarRepo.SetAyar(AyarlarRepository.GarsonAktif, chkAktif.Checked ? "1" : "0");
                _ayarRepo.SetAyar(AyarlarRepository.GarsonPort, port.ToString());
                _ayarRepo.SetAyar(AyarlarRepository.GarsonPin, txtPin.Text.Trim());

                UpdateDurum();

                MessageBox.Show(
                    "Garson sipariş ayarları kaydedildi!\n\n" +
                    (chkAktif.Checked
                        ? "Sistem aktif. Değişikliklerin geçerli olması için uygulamayı yeniden başlatın."
                        : "Sistem devre dışı."),
                    "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
