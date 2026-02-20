using System.Drawing.Drawing2D;
using System.Drawing.Text;
using PosProjesi.Services;

namespace PosProjesi.UI
{
    /// <summary>
    /// A custom styled update prompt dialog that replaces the plain MessageBox at startup.
    /// </summary>
    public class UpdatePromptDialog : Form
    {
        public UpdatePromptDialog(UpdateInfo info)
        {
            this.Text = "Güncelleme Bildirimi";
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(480, 340);
            this.BackColor = Theme.BgDark;
            this.DoubleBuffered = true;
            this.ShowInTaskbar = true;
            this.TopMost = true;

            this.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                var rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
                using var path = Theme.RoundedRect(rect, 16);

                // Background
                using var bgBrush = new LinearGradientBrush(rect,
                    Color.FromArgb(24, 26, 42), Color.FromArgb(18, 20, 34),
                    LinearGradientMode.Vertical);
                g.FillPath(bgBrush, path);

                // Border
                using var borderPen = new Pen(Color.FromArgb(80, Theme.AccentBlue.R, Theme.AccentBlue.G, Theme.AccentBlue.B), 1.5f);
                g.DrawPath(borderPen, path);

                // Top accent stripe
                g.SetClip(path);
                using var accentGrad = new LinearGradientBrush(
                    new Rectangle(0, 0, this.Width, 5),
                    Theme.AccentBlue, Theme.AccentTeal,
                    LinearGradientMode.Horizontal);
                g.FillRectangle(accentGrad, 0, 0, this.Width, 5);

                // Subtle glow behind icon
                using var glowPath = new GraphicsPath();
                glowPath.AddEllipse(this.Width / 2 - 80, 30, 160, 100);
                using var glowBrush = new PathGradientBrush(glowPath)
                {
                    CenterColor = Color.FromArgb(20, Theme.AccentBlue),
                    SurroundColors = new[] { Color.Transparent }
                };
                g.FillPath(glowBrush, glowPath);
                g.ResetClip();

                int centerX = this.Width / 2;

                // ── Big icon ──
                using var iconFont = new Font("Segoe UI Emoji", 36);
                var iconText = "🔄";
                var iconSize = TextRenderer.MeasureText(iconText, iconFont);
                TextRenderer.DrawText(g, iconText, iconFont,
                    new Point(centerX - iconSize.Width / 2, 28), Theme.AccentBlue,
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

                // ── Title ──
                using var titleFont = new Font("Segoe UI", 16, FontStyle.Bold);
                var titleText = "Yeni Güncelleme Mevcut!";
                var titleSize = TextRenderer.MeasureText(titleText, titleFont);
                TextRenderer.DrawText(g, titleText, titleFont,
                    new Point(centerX - titleSize.Width / 2, 92), Theme.TextPrimary,
                    TextFormatFlags.NoPadding);

                // ── Version badges ──
                using var badgeFont = new Font("Segoe UI", 10, FontStyle.Bold);
                var oldVer = $"v{UpdateService.CurrentVersion}";
                var newVer = $"v{info.Version}";

                var oldSize = TextRenderer.MeasureText(oldVer, badgeFont);
                var newSize = TextRenderer.MeasureText(newVer, badgeFont);
                int totalBadgeW = oldSize.Width + 16 + 36 + newSize.Width + 16;
                int badgeStartX = centerX - totalBadgeW / 2;
                int badgeY = 130;

                // Old badge
                var oldRect = new Rectangle(badgeStartX, badgeY, oldSize.Width + 16, 30);
                using var oldPath = Theme.RoundedRect(oldRect, 7);
                using var oldBg = new SolidBrush(Color.FromArgb(35, Theme.TextMuted.R, Theme.TextMuted.G, Theme.TextMuted.B));
                g.FillPath(oldBg, oldPath);
                TextRenderer.DrawText(g, oldVer, badgeFont,
                    new Rectangle(oldRect.X, oldRect.Y, oldRect.Width, oldRect.Height),
                    Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                // Arrow
                using var arrowFont = new Font("Segoe UI", 14, FontStyle.Bold);
                TextRenderer.DrawText(g, "→", arrowFont,
                    new Point(oldRect.Right + 8, badgeY + 2), Theme.AccentBlue, TextFormatFlags.NoPadding);

                // New badge
                var newRect = new Rectangle(oldRect.Right + 36, badgeY, newSize.Width + 16, 30);
                using var newPath = Theme.RoundedRect(newRect, 7);
                using var newBgBrush = new LinearGradientBrush(newRect,
                    Color.FromArgb(40, Theme.AccentGreen.R, Theme.AccentGreen.G, Theme.AccentGreen.B),
                    Color.FromArgb(15, Theme.AccentGreen.R, Theme.AccentGreen.G, Theme.AccentGreen.B),
                    LinearGradientMode.Vertical);
                g.FillPath(newBgBrush, newPath);
                using var newBorderPen = new Pen(Color.FromArgb(80, Theme.AccentGreen.R, Theme.AccentGreen.G, Theme.AccentGreen.B), 1);
                g.DrawPath(newBorderPen, newPath);
                TextRenderer.DrawText(g, newVer, badgeFont,
                    new Rectangle(newRect.X, newRect.Y, newRect.Width, newRect.Height),
                    Theme.AccentGreen, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                // ── Notes ──
                using var notesFont = new Font("Segoe UI", 9.5f);
                var notesRect = new Rectangle(40, 176, this.Width - 80, 40);
                TextRenderer.DrawText(g, info.Notes, notesFont, notesRect,
                    Theme.TextSecondary,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
            };

            // ── Buttons ──
            int bW = 160, bH = 42, bY = 240, gap = 20;
            int totalW = bW * 2 + gap;
            int startX = (this.Width - totalW) / 2;

            // "Sonra" button
            var btnLater = new Panel { Size = new Size(bW, bH), Location = new Point(startX, bY), Cursor = Cursors.Hand };
            bool laterHover = false;
            btnLater.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(0, 0, btnLater.Width - 1, btnLater.Height - 1);
                using var p = Theme.RoundedRect(r, 10);
                using var bg = new SolidBrush(laterHover ? Color.FromArgb(40, 42, 58) : Theme.BgCard);
                g.FillPath(bg, p);
                using var bp = new Pen(Theme.Border, 1);
                g.DrawPath(bp, p);
                using var f = new Font("Segoe UI", 11, FontStyle.Bold);
                TextRenderer.DrawText(g, "Sonra", f,
                    new Rectangle(0, 0, btnLater.Width, btnLater.Height),
                    Theme.TextSecondary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btnLater.MouseEnter += (s, e) => { laterHover = true; btnLater.Invalidate(); };
            btnLater.MouseLeave += (s, e) => { laterHover = false; btnLater.Invalidate(); };
            btnLater.Click += (s, e) => { this.DialogResult = DialogResult.No; this.Close(); };

            // "Güncelle" button
            var btnUpdate = new Panel { Size = new Size(bW, bH), Location = new Point(startX + bW + gap, bY), Cursor = Cursors.Hand };
            bool updateHover = false;
            btnUpdate.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(0, 0, btnUpdate.Width - 1, btnUpdate.Height - 1);
                using var p = Theme.RoundedRect(r, 10);
                using var bg = updateHover
                    ? new LinearGradientBrush(r, Theme.AccentTeal, Theme.AccentBlue, LinearGradientMode.Horizontal)
                    : new LinearGradientBrush(r, Theme.AccentBlue, Theme.Darken(Theme.AccentBlue, 15), LinearGradientMode.Vertical);
                g.FillPath(bg, p);
                using var f = new Font("Segoe UI", 11, FontStyle.Bold);
                TextRenderer.DrawText(g, "⬇ Güncelle", f,
                    new Rectangle(0, 0, btnUpdate.Width, btnUpdate.Height),
                    Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btnUpdate.MouseEnter += (s, e) => { updateHover = true; btnUpdate.Invalidate(); };
            btnUpdate.MouseLeave += (s, e) => { updateHover = false; btnUpdate.Invalidate(); };
            btnUpdate.Click += (s, e) => { this.DialogResult = DialogResult.Yes; this.Close(); };

            this.Controls.Add(btnLater);
            this.Controls.Add(btnUpdate);

            // Close on Escape
            this.KeyPreview = true;
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) { this.DialogResult = DialogResult.No; this.Close(); } };
        }
    }
}
