using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace PosProjesi.UI
{
    /// <summary>
    /// Animated toast notification panel that slides in from the bottom-right corner.
    /// </summary>
    public class UpdateToastPanel : Panel
    {
        private readonly string _version;
        private readonly string _notes;
        private readonly System.Windows.Forms.Timer _slideTimer;
        private readonly System.Windows.Forms.Timer _autoHideTimer;
        private int _targetY;
        private bool _isClosing;

        public UpdateToastPanel(string version, string notes)
        {
            _version = version;
            _notes = string.IsNullOrWhiteSpace(notes) ? "Yeni güncelleme mevcut" : notes;

            this.Size = new Size(320, 90);
            this.BackColor = Color.Transparent;
            this.Cursor = Cursors.Default;

            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer |
                ControlStyles.SupportsTransparentBackColor, true);

            // Slide-in animation
            _slideTimer = new System.Windows.Forms.Timer { Interval = 12 };
            _slideTimer.Tick += SlideTimer_Tick;

            // Auto-hide after 15 seconds
            _autoHideTimer = new System.Windows.Forms.Timer { Interval = 15000 };
            _autoHideTimer.Tick += (s, e) => { _autoHideTimer.Stop(); SlideOut(); };

            this.Paint += Toast_Paint;
            this.MouseClick += (s, e) =>
            {
                // Click the X area (top-right 30x30)
                if (e.X >= this.Width - 34 && e.Y <= 30)
                    SlideOut();
            };
        }

        /// <summary>
        /// Show the toast on the given parent form.
        /// </summary>
        public void ShowIn(Form parent)
        {
            int margin = 20;
            int startX = parent.ClientSize.Width - this.Width - margin;
            int startY = parent.ClientSize.Height; // start off-screen below
            _targetY = parent.ClientSize.Height - this.Height - margin;

            this.Location = new Point(startX, startY);
            parent.Controls.Add(this);
            this.BringToFront();

            _isClosing = false;
            _slideTimer.Start();
            _autoHideTimer.Start();
        }

        private void SlideOut()
        {
            if (_isClosing) return;
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
                // Ease-out animation
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

            // Background with slight transparency feel
            using var bgBrush = new SolidBrush(Color.FromArgb(30, 32, 48));
            g.FillPath(bgBrush, path);

            // Border with accent
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
            TextRenderer.DrawText(g, "🔄", iconFont, new Point(12, 16), Theme.AccentBlue,
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

            // Title
            using var titleFont = new Font("Segoe UI", 11, FontStyle.Bold);
            TextRenderer.DrawText(g, $"Güncelleme Mevcut v{_version}", titleFont,
                new Point(52, 14), Theme.TextPrimary, TextFormatFlags.NoPadding);

            // Notes
            using var notesFont = new Font("Segoe UI", 8.5f);
            var notesRect = new Rectangle(52, 40, this.Width - 90, 40);
            TextRenderer.DrawText(g, _notes, notesFont, notesRect, Theme.TextSecondary,
                TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);

            // Close button (X)
            using var closeFont = new Font("Segoe UI", 10, FontStyle.Bold);
            TextRenderer.DrawText(g, "✕", closeFont,
                new Point(this.Width - 28, 8), Theme.TextMuted, TextFormatFlags.NoPadding);
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
