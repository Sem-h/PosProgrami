using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Diagnostics;
using PosProjesi.UI;
using PosProjesi.Services;

namespace PosProjesi.Forms
{
    public class HakkindaForm : Form
    {
        public HakkindaForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Theme.ApplyFormDefaults(this, "Verimek POS - Hakkında");
            this.Size = new Size(640, 760);
            this.MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgDark
            };

            mainPanel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                int w = mainPanel.Width;
                int margin = 40;
                int textW = w - margin * 2;

                // ── Top banner ──
                var bannerRect = new Rectangle(0, 0, w, 160);
                using var bannerBrush = new LinearGradientBrush(
                    bannerRect,
                    Color.FromArgb(15, 40, 85),
                    Color.FromArgb(32, 38, 58),
                    LinearGradientMode.Vertical);
                g.FillRectangle(bannerBrush, bannerRect);

                // Accent line
                using var accBrush = new LinearGradientBrush(
                    new Rectangle(0, 157, w, 3), Theme.AccentOrange,
                    Theme.Darken(Theme.AccentOrange, 30), LinearGradientMode.Horizontal);
                g.FillRectangle(accBrush, 0, 157, w, 3);

                // Logo
                using var logoFont = new Font("Segoe UI", 36, FontStyle.Bold);
                TextRenderer.DrawText(g, "Verimek", logoFont,
                    new Rectangle(0, -4, w, 80), Theme.AccentOrange,
                    TextFormatFlags.HorizontalCenter);

                // Subtitle
                using var subFont = new Font("Segoe UI", 13);
                TextRenderer.DrawText(g, "POS Satış Sistemi", subFont,
                    new Rectangle(0, 88, w, 24), Color.FromArgb(180, 190, 210),
                    TextFormatFlags.HorizontalCenter);

                // Version badge
                string ver = $"v{UpdateService.CurrentVersion}";
                using var verFont = new Font("Segoe UI", 10, FontStyle.Bold);
                var verSz = TextRenderer.MeasureText(ver, verFont);
                int bw = verSz.Width + 24;
                var bRect = new Rectangle((w - bw) / 2, 112, bw, 28);
                using var bPath = Theme.RoundedRect(bRect, 14);
                using var bBg = new SolidBrush(Color.FromArgb(30, 80, 200, 120));
                g.FillPath(bBg, bPath);
                using var bBorder = new Pen(Color.FromArgb(60, 80, 200, 120));
                g.DrawPath(bBorder, bPath);
                TextRenderer.DrawText(g, ver, verFont, bRect, Theme.AccentGreen,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                // ── About text ──
                int y = 184;
                using var bodyFont = new Font("Segoe UI", 11f);

                string aboutText =
                    "Bu yazılım, Verimek Yazılım Ekibi tarafından geliştirilmiştir " +
                    "ve aktif olarak geliştirilmeye devam etmektedir.\n\n" +
                    "Verimek POS, işletmenizin satış, stok ve raporlama " +
                    "ihtiyaçlarını karşılamak üzere tasarlanmış modern bir " +
                    "satış noktası çözümüdür.";

                var aboutRect = new Rectangle(margin, y, textW, 160);
                TextRenderer.DrawText(g, aboutText, bodyFont, aboutRect,
                    Theme.TextSecondary, TextFormatFlags.WordBreak);

                // ── Separator ──
                y += 168;
                using var sepPen = new Pen(Theme.Border);
                g.DrawLine(sepPen, margin, y, w - margin, y);

                // ── Support header ──
                y += 24;
                using var headFont = new Font("Segoe UI", 14, FontStyle.Bold);
                TextRenderer.DrawText(g, "Destek & İletişim", headFont,
                    new Point(margin, y), Theme.TextPrimary);

                // Info rows
                y += 48;
                DrawInfoRow(g, "📞", "Telefon", "0224 323 00 27", Theme.AccentBlue, margin, y);
                y += 58;
                DrawInfoRow(g, "✉️", "E-posta", "bilgi@verimek.com", Theme.AccentPurple, margin, y);
                y += 58;
                DrawInfoRow(g, "🌐", "Web", "verimek.com", Theme.AccentTeal, margin, y);

                // ── Footer ──
                int footY = mainPanel.Height - 42;
                g.DrawLine(sepPen, margin, footY - 12, w - margin, footY - 12);
                using var footFont = new Font("Segoe UI", 9f);
                TextRenderer.DrawText(g, "© 2025 Verimek Yazılım  •  Tüm hakları saklıdır.",
                    footFont, new Rectangle(0, footY, w, 22), Theme.TextMuted,
                    TextFormatFlags.HorizontalCenter);
            };

            // Clickable web link
            var webLink = new Label
            {
                Text = "", Size = new Size(200, 40),
                Location = new Point(80, 530),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            webLink.Click += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo("https://verimek.com") { UseShellExecute = true }); }
                catch { }
            };
            mainPanel.Controls.Add(webLink);

            // Close button
            var closeBtn = new Panel { Size = new Size(160, 44), Cursor = Cursors.Hand };
            bool hover = false;
            closeBtn.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, closeBtn.Width - 1, closeBtn.Height - 1);
                using var path = Theme.RoundedRect(rect, 10);
                if (hover)
                {
                    using var bg = new LinearGradientBrush(rect, Theme.AccentOrange,
                        Theme.Darken(Theme.AccentOrange, 20), LinearGradientMode.Vertical);
                    g.FillPath(bg, path);
                }
                else
                {
                    using var bg = new SolidBrush(Color.FromArgb(44, 46, 60));
                    g.FillPath(bg, path);
                    using var border = new Pen(Theme.Border);
                    g.DrawPath(border, path);
                }
                using var font = new Font("Segoe UI", 11f, FontStyle.Bold);
                TextRenderer.DrawText(g, "Tamam", font, rect,
                    hover ? Color.White : Theme.TextPrimary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            closeBtn.MouseEnter += (s, e) => { hover = true; closeBtn.Invalidate(); };
            closeBtn.MouseLeave += (s, e) => { hover = false; closeBtn.Invalidate(); };
            closeBtn.Click += (s, e) => this.Close();
            mainPanel.Controls.Add(closeBtn);

            this.Controls.Add(mainPanel);
            this.Load += (s, e) =>
            {
                closeBtn.Location = new Point(
                    (mainPanel.Width - closeBtn.Width) / 2,
                    mainPanel.Height - 96);
            };
        }

        private void DrawInfoRow(Graphics g, string icon, string label, string value,
            Color accent, int x, int y)
        {
            // Icon circle
            var circle = new Rectangle(x, y, 38, 38);
            using var bg = new SolidBrush(Color.FromArgb(25, accent.R, accent.G, accent.B));
            g.FillEllipse(bg, circle);
            using var border = new Pen(Color.FromArgb(40, accent.R, accent.G, accent.B));
            g.DrawEllipse(border, circle);

            using var iconFont = new Font("Segoe UI Emoji", 13);
            var iconSz = TextRenderer.MeasureText(icon, iconFont);
            TextRenderer.DrawText(g, icon, iconFont,
                new Point(circle.X + (circle.Width - iconSz.Width) / 2 + 2,
                           circle.Y + (circle.Height - iconSz.Height) / 2),
                accent, TextFormatFlags.NoPadding);

            // Label
            using var labelFont = new Font("Segoe UI", 9f, FontStyle.Bold);
            TextRenderer.DrawText(g, label, labelFont,
                new Point(x + 50, y + 2), Theme.TextMuted);

            // Value
            using var valFont = new Font("Segoe UI", 12f);
            TextRenderer.DrawText(g, value, valFont,
                new Point(x + 50, y + 19), Theme.TextPrimary);
        }
    }
}
