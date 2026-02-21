using System.Drawing.Drawing2D;
using PosProjesi.UI;

namespace PosProjesi.Forms
{
    public class AdminLoginForm : Form
    {
        private TextBox txtSifre = null!;
        public bool GirisBasarili { get; private set; } = false;
        private const string ADMIN_SIFRE = "1234";

        public AdminLoginForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Admin Girişi";
            this.Size = new Size(380, 260);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.BgDark;
            this.ForeColor = Theme.TextPrimary;
            this.Font = Theme.FontBody;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.KeyPreview = true;
            this.DoubleBuffered = true;

            this.Paint += (s, e) =>
            {
                var g = e.Graphics;
                // Top accent
                using var lineBrush = new LinearGradientBrush(
                    new Rectangle(0, 0, this.ClientSize.Width, 2),
                    Theme.AccentOrange,
                    Color.FromArgb(60, Theme.AccentOrange),
                    LinearGradientMode.Horizontal);
                g.FillRectangle(lineBrush, 0, 0, this.ClientSize.Width, 2);
            };

            var lblTitle = new Label
            {
                Text = "Admin Girişi",
                Font = Theme.FontSubtitle,
                ForeColor = Theme.TextPrimary,
                Location = new Point(30, 24),
                AutoSize = true
            };

            var lblDesc = new Label
            {
                Text = "Devam etmek için yönetici şifresini girin",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                Location = new Point(30, 50),
                AutoSize = true
            };

            var lblSifre = Theme.CreateLabel("Şifre");
            lblSifre.Location = new Point(30, 86);

            txtSifre = Theme.CreateTextBox(300);
            txtSifre.Location = new Point(30, 108);
            txtSifre.UseSystemPasswordChar = true;
            txtSifre.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; DoLogin(); }
            };

            var btnGiris = Theme.CreateButton("Giriş Yap", Theme.AccentOrange, 145, 38);
            btnGiris.Location = new Point(30, 158);
            btnGiris.Click += (s, e) => DoLogin();

            var btnIptal = Theme.CreateButton("İptal", Theme.BgInput, 145, 38);
            btnIptal.Location = new Point(185, 158);
            btnIptal.ForeColor = Theme.TextSecondary;
            btnIptal.Click += (s, e) => { GirisBasarili = false; this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.AddRange(new Control[] { lblTitle, lblDesc, lblSifre, txtSifre, btnGiris, btnIptal });
            this.Shown += (s, e) => txtSifre.Focus();
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) { this.DialogResult = DialogResult.Cancel; this.Close(); } };
        }

        private void DoLogin()
        {
            if (txtSifre.Text == ADMIN_SIFRE)
            {
                GirisBasarili = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Yanlış şifre. Tekrar deneyin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSifre.Clear();
                txtSifre.Focus();
            }
        }
    }
}
