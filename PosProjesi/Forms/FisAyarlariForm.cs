using System.Drawing.Drawing2D;
using System.Drawing.Text;
using PosProjesi.DataAccess;
using PosProjesi.Services;
using PosProjesi.UI;

namespace PosProjesi.Forms
{
    public class FisAyarlariForm : Form
    {
        private readonly AyarlarRepository _ayarRepo = new();
        private ComboBox cmbYazici = null!;
        private ComboBox cmbKagit = null!;
        private TextBox txtIsletmeAdi = null!;
        private TextBox txtIsletmeAdres = null!;
        private TextBox txtIsletmeTelefon = null!;
        private TextBox txtAltMesaj = null!;
        private CheckBox chkFisAktif = null!;

        public FisAyarlariForm()
        {
            InitializeComponent();
            LoadAyarlar();
        }

        private void InitializeComponent()
        {
            Theme.ApplyFormDefaults(this, "Verimek POS - Fiş Ayarları");
            this.Size = new Size(560, 620);
            this.MinimumSize = new Size(500, 580);
            this.StartPosition = FormStartPosition.CenterParent;

            var header = Theme.CreateHeaderBar("🖨️  Fiş & Yazıcı Ayarları", Theme.AccentOrange);

            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(28, 20, 28, 20),
                BackColor = Theme.BgDark
            };

            int y = 0;

            // ═══ Yazıcı Ayarları Bölümü ═══
            var lblSection1 = CreateSectionLabel("YAZICI AYARLARI", y);
            scrollPanel.Controls.Add(lblSection1);
            y += 30;

            // Fiş Yazdırma Aktif
            chkFisAktif = new CheckBox
            {
                Text = "  Fiş yazdırmayı etkinleştir",
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.TextPrimary,
                Location = new Point(0, y),
                AutoSize = true,
                Checked = true
            };
            scrollPanel.Controls.Add(chkFisAktif);
            y += 36;

            // Yazıcı Seçimi
            scrollPanel.Controls.Add(CreateFieldLabel("Yazıcı:", y));
            y += 22;
            cmbYazici = new ComboBox
            {
                Location = new Point(0, y),
                Size = new Size(480, 30),
                Font = new Font("Segoe UI", 10),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextPrimary,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            // Populate installed printers
            var printers = FisYaziciService.GetInstalledPrinters();
            cmbYazici.Items.Add("(Varsayılan Yazıcı)");
            foreach (var p in printers) cmbYazici.Items.Add(p);
            if (cmbYazici.Items.Count > 0) cmbYazici.SelectedIndex = 0;
            scrollPanel.Controls.Add(cmbYazici);
            y += 38;

            // Kağıt Genişliği
            scrollPanel.Controls.Add(CreateFieldLabel("Kağıt Genişliği:", y));
            y += 22;
            cmbKagit = new ComboBox
            {
                Location = new Point(0, y),
                Size = new Size(200, 30),
                Font = new Font("Segoe UI", 10),
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextPrimary,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            cmbKagit.Items.AddRange(new object[] { "80mm (48 karakter)", "58mm (32 karakter)" });
            cmbKagit.SelectedIndex = 0;
            scrollPanel.Controls.Add(cmbKagit);
            y += 38;

            // Test Fişi Butonu
            var btnTest = Theme.CreateButton("🖨️  Test Fişi Yazdır", Theme.AccentTeal, 200, 36);
            btnTest.Location = new Point(0, y);
            btnTest.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnTest.Click += BtnTest_Click;
            scrollPanel.Controls.Add(btnTest);
            y += 52;

            // ═══ İşletme Bilgileri Bölümü ═══
            var sep1 = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(480, 1),
                BackColor = Theme.Border,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            scrollPanel.Controls.Add(sep1);
            y += 14;

            var lblSection2 = CreateSectionLabel("FİŞ TASARIMI", y);
            scrollPanel.Controls.Add(lblSection2);
            y += 30;

            // İşletme Adı
            scrollPanel.Controls.Add(CreateFieldLabel("İşletme Adı:", y));
            y += 22;
            txtIsletmeAdi = CreateStyledTextBox(y, 480);
            txtIsletmeAdi.PlaceholderText = "VERİMEK POS";
            scrollPanel.Controls.Add(txtIsletmeAdi);
            y += 38;

            // İşletme Adresi
            scrollPanel.Controls.Add(CreateFieldLabel("İşletme Adresi:", y));
            y += 22;
            txtIsletmeAdres = CreateStyledTextBox(y, 480);
            txtIsletmeAdres.PlaceholderText = "Adres bilgisi (opsiyonel)";
            scrollPanel.Controls.Add(txtIsletmeAdres);
            y += 38;

            // İşletme Telefonu
            scrollPanel.Controls.Add(CreateFieldLabel("Telefon:", y));
            y += 22;
            txtIsletmeTelefon = CreateStyledTextBox(y, 480);
            txtIsletmeTelefon.PlaceholderText = "Telefon (opsiyonel)";
            scrollPanel.Controls.Add(txtIsletmeTelefon);
            y += 38;

            // Alt Mesaj
            scrollPanel.Controls.Add(CreateFieldLabel("Fiş Alt Mesajı:", y));
            y += 22;
            txtAltMesaj = CreateStyledTextBox(y, 480);
            txtAltMesaj.PlaceholderText = "Bizi tercih ettiğiniz için teşekkürler!";
            scrollPanel.Controls.Add(txtAltMesaj);
            y += 46;

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
            // Yazıcı
            var yaziciAdi = _ayarRepo.GetAyar(AyarlarRepository.YaziciAdi);
            if (!string.IsNullOrEmpty(yaziciAdi))
            {
                int idx = cmbYazici.Items.IndexOf(yaziciAdi);
                if (idx >= 0) cmbYazici.SelectedIndex = idx;
            }

            // Kağıt genişliği
            var kagit = _ayarRepo.GetAyar(AyarlarRepository.KagitGenisligi, "80");
            cmbKagit.SelectedIndex = kagit == "58" ? 1 : 0;

            // İşletme bilgileri
            txtIsletmeAdi.Text = _ayarRepo.GetAyar(AyarlarRepository.IsletmeAdi);
            txtIsletmeAdres.Text = _ayarRepo.GetAyar(AyarlarRepository.IsletmeAdres);
            txtIsletmeTelefon.Text = _ayarRepo.GetAyar(AyarlarRepository.IsletmeTelefon);
            txtAltMesaj.Text = _ayarRepo.GetAyar(AyarlarRepository.FisAltMesaj);

            // Fiş aktif
            var aktif = _ayarRepo.GetAyar(AyarlarRepository.FisYazdirmaAktif, "1");
            chkFisAktif.Checked = aktif == "1";
        }

        private void BtnKaydet_Click(object? sender, EventArgs e)
        {
            try
            {
                // Yazıcı adı
                var selectedPrinter = cmbYazici.SelectedItem?.ToString() ?? "";
                if (selectedPrinter == "(Varsayılan Yazıcı)") selectedPrinter = "";
                _ayarRepo.SetAyar(AyarlarRepository.YaziciAdi, selectedPrinter);

                // Kağıt genişliği
                var kagit = cmbKagit.SelectedIndex == 1 ? "58" : "80";
                _ayarRepo.SetAyar(AyarlarRepository.KagitGenisligi, kagit);

                // İşletme bilgileri
                _ayarRepo.SetAyar(AyarlarRepository.IsletmeAdi, txtIsletmeAdi.Text.Trim());
                _ayarRepo.SetAyar(AyarlarRepository.IsletmeAdres, txtIsletmeAdres.Text.Trim());
                _ayarRepo.SetAyar(AyarlarRepository.IsletmeTelefon, txtIsletmeTelefon.Text.Trim());
                _ayarRepo.SetAyar(AyarlarRepository.FisAltMesaj, txtAltMesaj.Text.Trim());

                // Fiş aktif
                _ayarRepo.SetAyar(AyarlarRepository.FisYazdirmaAktif, chkFisAktif.Checked ? "1" : "0");

                MessageBox.Show("Ayarlar kaydedildi!", "Başarılı",
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
                // Save current settings first
                BtnKaydet_Click(null, EventArgs.Empty);

                var service = new FisYaziciService();
                service.TestFisiYazdir();
                MessageBox.Show("Test fişi yazıcıya gönderildi!", "Başarılı",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yazıcı hatası:\n{ex.Message}", "Yazıcı Hatası",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region UI Helpers

        private Label CreateSectionLabel(string text, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Theme.AccentOrange,
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

        #endregion
    }
}
