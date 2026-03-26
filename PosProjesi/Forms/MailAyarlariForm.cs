using PosProjesi.DataAccess;
using PosProjesi.Services;
using PosProjesi.UI;

namespace PosProjesi.Forms
{
    public class MailAyarlariForm : Form
    {
        private readonly AyarlarRepository _ayarRepo = new();
        private TextBox txtSmtpSunucu = null!;
        private TextBox txtSmtpPort = null!;
        private TextBox txtSmtpKullanici = null!;
        private TextBox txtSmtpSifre = null!;
        private CheckBox chkSsl = null!;
        private TextBox txtGonderici = null!;
        private TextBox txtAlici = null!;
        private CheckBox chkOtomatik = null!;
        private ComboBox cmbSaat = null!;

        public MailAyarlariForm()
        {
            InitializeComponent();
            LoadAyarlar();
        }

        private void InitializeComponent()
        {
            Theme.ApplyFormDefaults(this, "Verimek POS - E-posta Ayarları");
            this.Size = new Size(540, 680);
            this.MinimumSize = new Size(480, 640);
            this.StartPosition = FormStartPosition.CenterParent;

            var header = Theme.CreateHeaderBar("📧  E-posta Rapor Ayarları", Theme.AccentBlue);

            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(28, 20, 28, 20),
                BackColor = Theme.BgDark
            };

            int y = 0;

            // ═══ SMTP Ayarları ═══
            scrollPanel.Controls.Add(CreateSectionLabel("SMTP SUNUCU AYARLARI", y));
            y += 30;

            scrollPanel.Controls.Add(CreateFieldLabel("SMTP Sunucu:", y));
            y += 22;
            txtSmtpSunucu = CreateStyledTextBox(y, 460);
            txtSmtpSunucu.PlaceholderText = "smtp.gmail.com";
            scrollPanel.Controls.Add(txtSmtpSunucu);
            y += 36;

            scrollPanel.Controls.Add(CreateFieldLabel("Port:", y));
            y += 22;
            txtSmtpPort = CreateStyledTextBox(y, 100);
            txtSmtpPort.PlaceholderText = "587";
            scrollPanel.Controls.Add(txtSmtpPort);

            chkSsl = new CheckBox
            {
                Text = "  SSL/TLS kullan",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextPrimary,
                Location = new Point(120, y + 2),
                AutoSize = true,
                Checked = true
            };
            scrollPanel.Controls.Add(chkSsl);
            y += 36;

            scrollPanel.Controls.Add(CreateFieldLabel("Kullanıcı Adı (E-posta):", y));
            y += 22;
            txtSmtpKullanici = CreateStyledTextBox(y, 460);
            txtSmtpKullanici.PlaceholderText = "ornek@gmail.com";
            scrollPanel.Controls.Add(txtSmtpKullanici);
            y += 36;

            scrollPanel.Controls.Add(CreateFieldLabel("Şifre (Uygulama Şifresi):", y));
            y += 22;
            txtSmtpSifre = CreateStyledTextBox(y, 460);
            txtSmtpSifre.PlaceholderText = "••••••••";
            txtSmtpSifre.UseSystemPasswordChar = true;
            scrollPanel.Controls.Add(txtSmtpSifre);
            y += 40;

            // Separator
            scrollPanel.Controls.Add(CreateSeparator(y, 460));
            y += 14;

            // ═══ Gönderim Ayarları ═══
            scrollPanel.Controls.Add(CreateSectionLabel("GÖNDERİM AYARLARI", y));
            y += 30;

            scrollPanel.Controls.Add(CreateFieldLabel("Gönderici E-posta (opsiyonel):", y));
            y += 22;
            txtGonderici = CreateStyledTextBox(y, 460);
            txtGonderici.PlaceholderText = "Boş bırakılırsa kullanıcı adı kullanılır";
            scrollPanel.Controls.Add(txtGonderici);
            y += 36;

            scrollPanel.Controls.Add(CreateFieldLabel("Alıcı E-posta:", y));
            y += 22;
            txtAlici = CreateStyledTextBox(y, 460);
            txtAlici.PlaceholderText = "rapor@isletme.com";
            scrollPanel.Controls.Add(txtAlici);
            y += 36;

            // Test butonu
            var btnTest = Theme.CreateButton("📧  Test Maili Gönder", Theme.AccentBlue, 200, 36);
            btnTest.Location = new Point(0, y);
            btnTest.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnTest.Click += BtnTest_Click;
            scrollPanel.Controls.Add(btnTest);
            y += 50;

            // Separator
            scrollPanel.Controls.Add(CreateSeparator(y, 460));
            y += 14;

            // ═══ Otomatik Rapor ═══
            scrollPanel.Controls.Add(CreateSectionLabel("OTOMATİK GÜNLÜK RAPOR", y));
            y += 30;

            chkOtomatik = new CheckBox
            {
                Text = "  Günlük raporu otomatik gönder",
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.TextPrimary,
                Location = new Point(0, y),
                AutoSize = true,
                Checked = false
            };
            scrollPanel.Controls.Add(chkOtomatik);
            y += 34;

            scrollPanel.Controls.Add(CreateFieldLabel("Gönderim Saati:", y));
            y += 22;
            cmbSaat = new ComboBox
            {
                Location = new Point(0, y),
                Size = new Size(120, 30),
                Font = new Font("Segoe UI", 10),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextPrimary,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            for (int h = 0; h < 24; h++)
                cmbSaat.Items.Add($"{h:D2}:00");
            cmbSaat.SelectedIndex = 23; // Default 23:00
            scrollPanel.Controls.Add(cmbSaat);
            y += 38;

            // Manuel gönder butonu
            var btnGonder = Theme.CreateButton("📊  Bugünün Raporunu Gönder", Theme.AccentGreen, 250, 36);
            btnGonder.Location = new Point(0, y);
            btnGonder.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnGonder.Click += BtnGonder_Click;
            scrollPanel.Controls.Add(btnGonder);

            // ═══ Bottom Buttons ═══
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

            this.Controls.Add(scrollPanel);
            this.Controls.Add(bottomPanel);
            this.Controls.Add(header);
        }

        private void LoadAyarlar()
        {
            txtSmtpSunucu.Text = _ayarRepo.GetAyar(AyarlarRepository.SmtpSunucu);
            txtSmtpPort.Text = _ayarRepo.GetAyar(AyarlarRepository.SmtpPort, "587");
            txtSmtpKullanici.Text = _ayarRepo.GetAyar(AyarlarRepository.SmtpKullanici);
            txtSmtpSifre.Text = _ayarRepo.GetAyar(AyarlarRepository.SmtpSifre);
            chkSsl.Checked = _ayarRepo.GetAyar(AyarlarRepository.SmtpSsl, "1") == "1";
            txtGonderici.Text = _ayarRepo.GetAyar(AyarlarRepository.MailGonderici);
            txtAlici.Text = _ayarRepo.GetAyar(AyarlarRepository.MailAlici);
            chkOtomatik.Checked = _ayarRepo.GetAyar(AyarlarRepository.OtomatikRaporAktif, "0") == "1";

            var saat = _ayarRepo.GetAyar(AyarlarRepository.OtomatikRaporSaat, "23:00");
            int saatIdx = cmbSaat.Items.IndexOf(saat);
            if (saatIdx >= 0) cmbSaat.SelectedIndex = saatIdx;
        }

        private void BtnKaydet_Click(object? sender, EventArgs e)
        {
            try
            {
                _ayarRepo.SetAyar(AyarlarRepository.SmtpSunucu, txtSmtpSunucu.Text.Trim());
                _ayarRepo.SetAyar(AyarlarRepository.SmtpPort, txtSmtpPort.Text.Trim());
                _ayarRepo.SetAyar(AyarlarRepository.SmtpKullanici, txtSmtpKullanici.Text.Trim());
                _ayarRepo.SetAyar(AyarlarRepository.SmtpSifre, txtSmtpSifre.Text.Trim());
                _ayarRepo.SetAyar(AyarlarRepository.SmtpSsl, chkSsl.Checked ? "1" : "0");
                _ayarRepo.SetAyar(AyarlarRepository.MailGonderici, txtGonderici.Text.Trim());
                _ayarRepo.SetAyar(AyarlarRepository.MailAlici, txtAlici.Text.Trim());
                _ayarRepo.SetAyar(AyarlarRepository.OtomatikRaporAktif, chkOtomatik.Checked ? "1" : "0");
                _ayarRepo.SetAyar(AyarlarRepository.OtomatikRaporSaat, cmbSaat.SelectedItem?.ToString() ?? "23:00");

                MessageBox.Show("E-posta ayarları kaydedildi!", "Başarılı",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnTest_Click(object? sender, EventArgs e)
        {
            try
            {
                // Save settings first
                BtnKaydet_Click(null, EventArgs.Empty);

                this.Cursor = Cursors.WaitCursor;
                var service = new EmailRaporService();
                service.TestMailGonder();
                this.Cursor = Cursors.Default;

                MessageBox.Show("Test e-postası başarıyla gönderildi!\nLütfen gelen kutunuzu kontrol edin.",
                    "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show($"E-posta gönderilemedi:\n{ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnGonder_Click(object? sender, EventArgs e)
        {
            try
            {
                // Save settings first
                BtnKaydet_Click(null, EventArgs.Empty);

                this.Cursor = Cursors.WaitCursor;
                var service = new EmailRaporService();
                service.GunlukRaporGonder(DateTime.Today);
                this.Cursor = Cursors.Default;

                MessageBox.Show($"Bugünün ({DateTime.Today:dd.MM.yyyy}) satış raporu başarıyla gönderildi!",
                    "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show($"Rapor gönderilemedi:\n{ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region UI Helpers

        private Label CreateSectionLabel(string text, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Theme.AccentBlue,
                Location = new Point(0, y),
                AutoSize = true
            };
        }

        private Label CreateFieldLabel(string text, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Theme.TextSecondary,
                Location = new Point(0, y),
                AutoSize = true
            };
        }

        private TextBox CreateStyledTextBox(int y, int width)
        {
            var txt = Theme.CreateTextBox(width);
            txt.Location = new Point(0, y);
            txt.Size = new Size(width, 28);
            txt.Font = new Font("Segoe UI", 10);
            txt.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            return txt;
        }

        private Panel CreateSeparator(int y, int width)
        {
            return new Panel
            {
                Location = new Point(0, y),
                Size = new Size(width, 1),
                BackColor = Theme.Border,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
        }

        #endregion
    }
}
