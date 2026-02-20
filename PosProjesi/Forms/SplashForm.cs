using Svg;
using System.Drawing.Drawing2D;
using PosProjesi.UI;
using PosProjesi.Services;

namespace PosProjesi.Forms
{
    public class SplashForm : Form
    {
        private float _opacity = 0f;
        private readonly System.Windows.Forms.Timer _fadeInTimer;
        private readonly System.Windows.Forms.Timer _holdTimer;
        private readonly System.Windows.Forms.Timer _fadeOutTimer;
        private Image? _logoImage;

        public SplashForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(580, 400);
            this.BackColor = Theme.BgDark;
            this.DoubleBuffered = true;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Opacity = 0;

            LoadLogo();

            _fadeInTimer = new System.Windows.Forms.Timer { Interval = 25 };
            _fadeInTimer.Tick += (s, e) =>
            {
                _opacity += 0.06f;
                if (_opacity >= 1f) { _opacity = 1f; _fadeInTimer.Stop(); _holdTimer.Start(); }
                this.Opacity = _opacity;
                this.Invalidate();
            };

            _holdTimer = new System.Windows.Forms.Timer { Interval = 1800 };
            _holdTimer.Tick += (s, e) => { _holdTimer.Stop(); _fadeOutTimer.Start(); };

            _fadeOutTimer = new System.Windows.Forms.Timer { Interval = 25 };
            _fadeOutTimer.Tick += (s, e) =>
            {
                _opacity -= 0.06f;
                if (_opacity <= 0f) { _opacity = 0f; _fadeOutTimer.Stop(); this.DialogResult = DialogResult.OK; this.Close(); }
                this.Opacity = _opacity;
            };

            this.Paint += SplashForm_Paint;
        }

        private void LoadLogo()
        {
            try
            {
                var possiblePaths = new[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "image", "verimek-logo3.svg"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "image", "verimek-logo3.svg"),
                    Path.Combine(Directory.GetCurrentDirectory(), "image", "verimek-logo3.svg"),
                };

                string? svgPath = possiblePaths.FirstOrDefault(File.Exists);
                if (svgPath != null)
                {
                    var svgDoc = SvgDocument.Open(svgPath);
                    svgDoc.Width = new SvgUnit(SvgUnitType.Pixel, 200);
                    svgDoc.Height = new SvgUnit(SvgUnitType.Pixel, 200);
                    _logoImage = svgDoc.Draw();
                }
            }
            catch { }
        }

        protected override void OnShown(EventArgs e) { base.OnShown(e); _fadeInTimer.Start(); }

        private void SplashForm_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // Background
            using var bgBrush = new SolidBrush(Theme.BgDark);
            g.FillRectangle(bgBrush, this.ClientRectangle);

            // Subtle radial glow
            using var glowPath = new GraphicsPath();
            glowPath.AddEllipse(this.Width / 2 - 200, -50, 400, 300);
            using var glowBrush = new PathGradientBrush(glowPath)
            {
                CenterColor = Color.FromArgb(15, Theme.AccentBlue),
                SurroundColors = new[] { Color.Transparent }
            };
            g.FillPath(glowBrush, glowPath);

            int centerX = this.Width / 2;
            int currentY = 30;

            // Logo
            if (_logoImage != null)
            {
                int logoX = (this.Width - _logoImage.Width) / 2;
                g.DrawImage(_logoImage, logoX, currentY);
                currentY += _logoImage.Height + 10;
            }
            else
            {
                using var f = new Font("Segoe UI", 32, FontStyle.Bold);
                var size = g.MeasureString("Verimek", f);
                g.DrawString("Verimek", f, new SolidBrush(Theme.AccentBlue), (this.Width - size.Width) / 2, currentY);
                currentY += (int)size.Height + 10;
            }

            // Subtitle
            var subText = "POS Satış Sistemi";
            using var sf = new Font("Segoe UI", 12);
            var subSize = g.MeasureString(subText, sf);
            g.DrawString(subText, sf, new SolidBrush(Theme.TextSecondary), (this.Width - subSize.Width) / 2, currentY);

            // Version at bottom
            g.DrawString($"v{UpdateService.CurrentVersion}", Theme.FontSmall, new SolidBrush(Theme.TextMuted), this.Width - 60, this.Height - 22);

            // Bottom accent line
            using var lineBrush = new LinearGradientBrush(
                new Rectangle(0, this.Height - 2, this.Width, 2),
                Theme.AccentBlue,
                Color.FromArgb(60, Theme.AccentBlue),
                LinearGradientMode.Horizontal);
            g.FillRectangle(lineBrush, 0, this.Height - 2, this.Width, 2);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            _fadeInTimer.Stop(); _holdTimer.Stop(); _fadeOutTimer.Stop();
            _logoImage?.Dispose();
        }
    }
}
