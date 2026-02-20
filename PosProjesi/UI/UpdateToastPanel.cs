using System.Drawing.Drawing2D;
using System.Drawing.Text;
using PosProjesi.Services;

namespace PosProjesi.UI
{
    /// <summary>
    /// Animated toast notification panel that slides in from the bottom-right corner.
    /// Includes "Güncelle" button to download and apply the update.
    /// </summary>
    public class UpdateToastPanel : Panel
    {
        private readonly UpdateInfo _updateInfo;
        private readonly System.Windows.Forms.Timer _slideTimer;
        private readonly System.Windows.Forms.Timer _autoHideTimer;
        private int _targetY;
        private bool _isClosing;
        private bool _isDownloading;
        private int _downloadProgress;
        private string _statusText;
        private bool _btnHover;

        public UpdateToastPanel(UpdateInfo info)
        {
            _updateInfo = info;
            _statusText = info.Notes;

            this.Size = new Size(400, 180);
            this.BackColor = Color.Transparent;
            this.Cursor = Cursors.Default;

            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer |
                ControlStyles.SupportsTransparentBackColor, true);

            _slideTimer = new System.Windows.Forms.Timer { Interval = 12 };
            _slideTimer.Tick += SlideTimer_Tick;

            _autoHideTimer = new System.Windows.Forms.Timer { Interval = 30000 };
            _autoHideTimer.Tick += (s, e) => { _autoHideTimer.Stop(); SlideOut(); };

            this.Paint += Toast_Paint;
            this.MouseClick += OnToastClick;
            this.MouseMove += OnToastMouseMove;
        }

        public void ShowIn(Form parent)
        {
            int margin = 20;
            int startX = parent.ClientSize.Width - this.Width - margin;
            int startY = parent.ClientSize.Height;
            _targetY = parent.ClientSize.Height - this.Height - margin;

            this.Location = new Point(startX, startY);
            parent.Controls.Add(this);
            this.BringToFront();

            _isClosing = false;
            _slideTimer.Start();
            _autoHideTimer.Start();
        }

        private void OnToastMouseMove(object? sender, MouseEventArgs e)
        {
            bool wasBtnHover = _btnHover;
            int btnW = 130; int btnH = 34;
            int btnX = this.Width - btnW - 20;
            int btnY = this.Height - btnH - 16;
            _btnHover = e.X >= btnX && e.X <= btnX + btnW && e.Y >= btnY && e.Y <= btnY + btnH;
            if (_btnHover != wasBtnHover) this.Invalidate();
        }

        private void OnToastClick(object? sender, MouseEventArgs e)
        {
            // Close button area
            if (e.X >= this.Width - 36 && e.Y <= 30)
            {
                SlideOut();
                return;
            }

            // "Güncelle" button area
            int btnW = 130; int btnH = 34;
            int btnX = this.Width - btnW - 20;
            int btnY = this.Height - btnH - 16;
            if (e.X >= btnX && e.X <= btnX + btnW && e.Y >= btnY && e.Y <= btnY + btnH && !_isDownloading)
            {
                StartUpdate();
            }
        }

        private async void StartUpdate()
        {
            _isDownloading = true;
            _autoHideTimer.Stop();
            _statusText = "İndiriliyor...";
            this.Invalidate();

            var service = new UpdateService();
            var success = await service.DownloadAndApplyAsync(_updateInfo, progress =>
            {
                _downloadProgress = progress;
                _statusText = $"İndiriliyor... %{progress}";
                this.Invoke(() => this.Invalidate());
            });

            if (success)
            {
                _statusText = "Güncelleme uygulanıyor, uygulama yeniden başlatılacak...";
                this.Invalidate();

                await Task.Delay(500);
                Application.Exit();
            }
            else
            {
                _statusText = "İndirme başarısız! Tekrar deneyin.";
                _isDownloading = false;
                this.Invalidate();
            }
            service.Dispose();
        }

        private void SlideOut()
        {
            if (_isClosing || _isDownloading) return;
            _isClosing = true;
            _autoHideTimer.Stop();

            if (this.Parent != null)
                _targetY = this.Parent.ClientSize.Height + 10;

            _slideTimer.Start();
        }

        private void SlideTimer_Tick(object? sender, EventArgs e)
        {
            int current = this.Top;
            int diff = _targetY - current;

            if (Math.Abs(diff) <= 2)
            {
                this.Top = _targetY;
                _slideTimer.Stop();

                if (_isClosing)
                {
                    this.Parent?.Controls.Remove(this);
                    this.Dispose();
                }
            }
            else
            {
                this.Top += (int)(diff * 0.18);
            }
        }

        private void Toast_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            using var path = Theme.RoundedRect(rect, 14);

            // Shadow effect
            using var shadowPath = Theme.RoundedRect(new Rectangle(3, 3, this.Width - 1, this.Height - 1), 14);
            using var shadowBrush = new SolidBrush(Color.FromArgb(40, 0, 0, 0));
            g.FillPath(shadowBrush, shadowPath);

            // Background gradient
            using var bgBrush = new LinearGradientBrush(
                rect,
                Color.FromArgb(26, 28, 44),
                Color.FromArgb(20, 22, 36),
                LinearGradientMode.Vertical);
            g.FillPath(bgBrush, path);

            // Border with glow
            using var borderPen = new Pen(Color.FromArgb(100, Theme.AccentBlue.R, Theme.AccentBlue.G, Theme.AccentBlue.B), 1.5f);
            g.DrawPath(borderPen, path);

            // Top accent gradient stripe
            g.SetClip(path);
            using var accentBrush = new LinearGradientBrush(
                new Rectangle(0, 0, this.Width, 4),
                Theme.AccentBlue, Theme.AccentTeal,
                LinearGradientMode.Horizontal);
            g.FillRectangle(accentBrush, 0, 0, this.Width, 4);
            g.ResetClip();

            // ── Icon circle ──
            var iconCircle = new Rectangle(16, 16, 48, 48);
            using var iconBg = new SolidBrush(Color.FromArgb(25, Theme.AccentBlue.R, Theme.AccentBlue.G, Theme.AccentBlue.B));
            g.FillEllipse(iconBg, iconCircle);
            using var iconBorderPen = new Pen(Color.FromArgb(50, Theme.AccentBlue.R, Theme.AccentBlue.G, Theme.AccentBlue.B), 1.2f);
            g.DrawEllipse(iconBorderPen, iconCircle);

            using var iconFont = new Font("Segoe UI Emoji", 22);
            TextRenderer.DrawText(g, "🔄", iconFont, new Point(21, 23), Theme.AccentBlue,
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

            // ── Title ──
            using var titleFont = new Font("Segoe UI", 12, FontStyle.Bold);
            TextRenderer.DrawText(g, "Güncelleme Mevcut!", titleFont,
                new Point(74, 16), Theme.TextPrimary, TextFormatFlags.NoPadding);

            // ── Version badges ──
            int badgeY = 42;
            // Current version badge
            using var badgeFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            var curText = $"v{UpdateService.CurrentVersion}";
            var curSize = TextRenderer.MeasureText(curText, badgeFont);
            var curBadge = new Rectangle(74, badgeY, curSize.Width + 12, 22);
            using var curPath = Theme.RoundedRect(curBadge, 5);
            using var curBg = new SolidBrush(Color.FromArgb(30, Theme.TextMuted.R, Theme.TextMuted.G, Theme.TextMuted.B));
            g.FillPath(curBg, curPath);
            TextRenderer.DrawText(g, curText, badgeFont,
                new Rectangle(curBadge.X, curBadge.Y, curBadge.Width, curBadge.Height),
                Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            // Arrow
            using var arrowFont = new Font("Segoe UI", 10, FontStyle.Bold);
            TextRenderer.DrawText(g, "→", arrowFont,
                new Point(curBadge.Right + 6, badgeY + 1), Theme.AccentBlue, TextFormatFlags.NoPadding);

            // New version badge
            var newText = $"v{_updateInfo.Version}";
            var newSize = TextRenderer.MeasureText(newText, badgeFont);
            int newX = curBadge.Right + 30;
            var newBadge = new Rectangle(newX, badgeY, newSize.Width + 12, 22);
            using var newPath = Theme.RoundedRect(newBadge, 5);
            using var newBg = new SolidBrush(Color.FromArgb(30, Theme.AccentGreen.R, Theme.AccentGreen.G, Theme.AccentGreen.B));
            g.FillPath(newBg, newPath);
            using var newBorderPen = new Pen(Color.FromArgb(60, Theme.AccentGreen.R, Theme.AccentGreen.G, Theme.AccentGreen.B), 1);
            g.DrawPath(newBorderPen, newPath);
            TextRenderer.DrawText(g, newText, badgeFont,
                new Rectangle(newBadge.X, newBadge.Y, newBadge.Width, newBadge.Height),
                Theme.AccentGreen, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            // ── Status / Notes ──
            using var notesFont = new Font("Segoe UI", 9f);
            var notesRect = new Rectangle(20, 76, this.Width - 40, 30);
            TextRenderer.DrawText(g, _statusText, notesFont, notesRect, Theme.TextSecondary,
                TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

            // ── Close button ──
            using var closeFont = new Font("Segoe UI", 11, FontStyle.Bold);
            TextRenderer.DrawText(g, "✕", closeFont,
                new Point(this.Width - 30, 10), Theme.TextMuted, TextFormatFlags.NoPadding);

            // ── Download progress bar ──
            if (_isDownloading && _downloadProgress > 0)
            {
                int barY = this.Height - 54;
                int barW = this.Width - 40;
                int barH = 8;

                using var barBg = new SolidBrush(Theme.BgInput);
                var barBgRect = new Rectangle(20, barY, barW, barH);
                using var barBgPath = Theme.RoundedRect(barBgRect, 4);
                g.FillPath(barBg, barBgPath);

                int fillW = Math.Max(8, (int)(barW * _downloadProgress / 100.0));
                var fillRect = new Rectangle(20, barY, fillW, barH);
                using var fillBrush = new LinearGradientBrush(
                    fillRect, Theme.AccentBlue, Theme.AccentTeal,
                    LinearGradientMode.Horizontal);
                using var fillPath = Theme.RoundedRect(fillRect, 4);
                g.FillPath(fillBrush, fillPath);

                // Percentage text
                using var pctFont = new Font("Segoe UI", 8, FontStyle.Bold);
                TextRenderer.DrawText(g, $"%{_downloadProgress}", pctFont,
                    new Point(20 + fillW + 6, barY - 2), Theme.AccentBlue, TextFormatFlags.NoPadding);
            }
            else if (!_isDownloading)
            {
                // "Güncelle" button
                int btnW = 130; int btnH = 34;
                int btnX = this.Width - btnW - 20;
                int btnY = this.Height - btnH - 16;

                var btnRect = new Rectangle(btnX, btnY, btnW, btnH);
                using var btnPath = Theme.RoundedRect(btnRect, 8);

                if (_btnHover)
                {
                    using var btnBg = new LinearGradientBrush(btnRect, Theme.AccentTeal, Theme.AccentBlue, LinearGradientMode.Horizontal);
                    g.FillPath(btnBg, btnPath);
                }
                else
                {
                    using var btnBg = new LinearGradientBrush(btnRect, Theme.AccentBlue, Theme.Darken(Theme.AccentBlue, 20), LinearGradientMode.Vertical);
                    g.FillPath(btnBg, btnPath);
                }

                using var btnFont = new Font("Segoe UI", 10, FontStyle.Bold);
                TextRenderer.DrawText(g, "⬇ Güncelle", btnFont,
                    new Rectangle(btnX, btnY, btnW, btnH),
                    Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                // "Sonra" link
                using var laterFont = new Font("Segoe UI", 8.5f);
                TextRenderer.DrawText(g, "Sonra", laterFont,
                    new Point(20, this.Height - btnH - 8), Theme.TextMuted, TextFormatFlags.NoPadding);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _slideTimer.Dispose();
                _autoHideTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
