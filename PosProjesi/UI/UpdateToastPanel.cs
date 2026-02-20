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

        public UpdateToastPanel(UpdateInfo info)
        {
            _updateInfo = info;
            _statusText = info.Notes;

            this.Size = new Size(340, 110);
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

        private void OnToastClick(object? sender, MouseEventArgs e)
        {
            // Close button area (top-right 30x30)
            if (e.X >= this.Width - 34 && e.Y <= 28)
            {
                SlideOut();
                return;
            }

            // "Güncelle" button area (bottom-right)
            if (e.X >= this.Width - 110 && e.Y >= this.Height - 36 && !_isDownloading)
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
            using var path = Theme.RoundedRect(rect, 10);

            // Background
            using var bgBrush = new SolidBrush(Color.FromArgb(30, 32, 48));
            g.FillPath(bgBrush, path);

            // Border
            using var borderPen = new Pen(Theme.AccentBlue, 1.5f);
            g.DrawPath(borderPen, path);

            // Top accent stripe
            g.SetClip(path);
            using var accentBrush = new LinearGradientBrush(
                new Rectangle(0, 0, this.Width, 3),
                Theme.AccentBlue,
                Theme.Darken(Theme.AccentBlue, 40),
                LinearGradientMode.Horizontal);
            g.FillRectangle(accentBrush, 0, 0, this.Width, 3);
            g.ResetClip();

            // Icon
            using var iconFont = new Font("Segoe UI Emoji", 20);
            TextRenderer.DrawText(g, "🔄", iconFont, new Point(12, 14), Theme.AccentBlue,
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

            // Title
            using var titleFont = new Font("Segoe UI", 11, FontStyle.Bold);
            TextRenderer.DrawText(g, $"Güncelleme Mevcut v{_updateInfo.Version}", titleFont,
                new Point(52, 12), Theme.TextPrimary, TextFormatFlags.NoPadding);

            // Status text / Notes
            using var notesFont = new Font("Segoe UI", 8.5f);
            var notesRect = new Rectangle(52, 36, this.Width - 65, 20);
            TextRenderer.DrawText(g, _statusText, notesFont, notesRect, Theme.TextSecondary,
                TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

            // Close button (X)
            using var closeFont = new Font("Segoe UI", 10, FontStyle.Bold);
            TextRenderer.DrawText(g, "✕", closeFont,
                new Point(this.Width - 28, 8), Theme.TextMuted, TextFormatFlags.NoPadding);

            // Download progress bar (if downloading)
            if (_isDownloading && _downloadProgress > 0)
            {
                int barY = this.Height - 40;
                int barW = this.Width - 32;
                int barH = 6;

                // Background bar
                using var barBg = new SolidBrush(Theme.BgInput);
                var barBgRect = new Rectangle(16, barY, barW, barH);
                using var barBgPath = Theme.RoundedRect(barBgRect, 3);
                g.FillPath(barBg, barBgPath);

                // Progress fill
                int fillW = (int)(barW * _downloadProgress / 100.0);
                if (fillW > 0)
                {
                    var fillRect = new Rectangle(16, barY, fillW, barH);
                    using var fillBrush = new LinearGradientBrush(
                        fillRect, Theme.AccentBlue, Theme.AccentTeal,
                        LinearGradientMode.Horizontal);
                    using var fillPath = Theme.RoundedRect(fillRect, 3);
                    g.FillPath(fillBrush, fillPath);
                }
            }
            else if (!_isDownloading)
            {
                // "Güncelle" button
                int btnW = 90;
                int btnH = 28;
                int btnX = this.Width - btnW - 16;
                int btnY = this.Height - btnH - 12;

                var btnRect = new Rectangle(btnX, btnY, btnW, btnH);
                using var btnPath = Theme.RoundedRect(btnRect, 6);
                using var btnBg = new SolidBrush(Theme.AccentBlue);
                g.FillPath(btnBg, btnPath);

                using var btnFont = new Font("Segoe UI", 9, FontStyle.Bold);
                TextRenderer.DrawText(g, "Güncelle", btnFont,
                    new Rectangle(btnX, btnY, btnW, btnH),
                    Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
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
