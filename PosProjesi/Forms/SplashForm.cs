using Svg;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
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
        private readonly System.Windows.Forms.Timer _typeTimer;
        private Image? _logoImage;

        // Typewriter animation state
        private const string FullText = "Verimek Telekomünikasyon";
        private int _displayLen = 0;
        private int _actionIndex = 0;
        private int _subStep = 0;
        private bool _cursorVisible = true;
        private int _cursorBlink = 0;

        // The animation script: positive = type N chars, negative = delete N chars
        private static readonly int[] _actions = { 2, -1, 3, -2, 5, -3, 7, -2, 15 };

        public SplashForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(950, 680);
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
                if (_opacity >= 1f) { _opacity = 1f; _fadeInTimer.Stop(); _typeTimer.Start(); }
                this.Opacity = _opacity;
                this.Invalidate();
            };

            _typeTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _typeTimer.Tick += TypeTimer_Tick;

            _holdTimer = new System.Windows.Forms.Timer { Interval = 5000 };
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

        private void TypeTimer_Tick(object? sender, EventArgs e)
        {
            _cursorBlink++;
            _cursorVisible = (_cursorBlink % 4) < 3; // mostly visible, brief blink

            if (_actionIndex >= _actions.Length)
            {
                // Typing complete — hold cursor blinking for a moment then proceed
                _cursorBlink++;
                if (_cursorBlink > _actions.Length + 30)
                {
                    _typeTimer.Stop();
                    _holdTimer.Start();
                }
                this.Invalidate();
                return;
            }

            int action = _actions[_actionIndex];
            if (action > 0)
            {
                // Typing forward
                _displayLen = Math.Min(_displayLen + 1, FullText.Length);
                _subStep++;
                if (_subStep >= action)
                {
                    _subStep = 0;
                    _actionIndex++;
                }
            }
            else
            {
                // Deleting backward
                _displayLen = Math.Max(_displayLen - 1, 0);
                _subStep++;
                if (_subStep >= -action)
                {
                    _subStep = 0;
                    _actionIndex++;
                }
            }

            this.Invalidate();
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
                    svgDoc.Width = new SvgUnit(SvgUnitType.Pixel, 520);
                    svgDoc.Height = new SvgUnit(SvgUnitType.Pixel, 520);
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
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            // Background
            using var bgBrush = new SolidBrush(Theme.BgDark);
            g.FillRectangle(bgBrush, this.ClientRectangle);

            // Outer border
            using var borderPen = new Pen(Color.FromArgb(40, Theme.AccentBlue.R, Theme.AccentBlue.G, Theme.AccentBlue.B), 1.5f);
            g.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);

            // Subtle radial glow behind logo
            using var glowPath = new GraphicsPath();
            glowPath.AddEllipse(this.Width / 2 - 270, -50, 540, 400);
            using var glowBrush = new PathGradientBrush(glowPath)
            {
                CenterColor = Color.FromArgb(18, Theme.AccentBlue),
                SurroundColors = new[] { Color.Transparent }
            };
            g.FillPath(glowBrush, glowPath);

            // Secondary glow (purple)
            using var glow2Path = new GraphicsPath();
            glow2Path.AddEllipse(this.Width / 2 - 190, 60, 380, 260);
            using var glow2Brush = new PathGradientBrush(glow2Path)
            {
                CenterColor = Color.FromArgb(8, Theme.AccentPurple),
                SurroundColors = new[] { Color.Transparent }
            };
            g.FillPath(glow2Brush, glow2Path);

            int centerX = this.Width / 2;
            int currentY = 42;

            // Logo
            if (_logoImage != null)
            {
                int logoX = (this.Width - _logoImage.Width) / 2;
                g.DrawImage(_logoImage, logoX, currentY);
                currentY += _logoImage.Height + 18;
            }
            else
            {
                using var f = new Font("Segoe UI", 44, FontStyle.Bold);
                var size = g.MeasureString("Verimek", f);
                using var textBrush = new SolidBrush(Theme.AccentBlue);
                g.DrawString("Verimek", f, textBrush, (this.Width - size.Width) / 2, currentY);
                currentY += (int)size.Height + 18;
            }

            // ── Typewriter text: "Verimek Telekomünikasyon" ──
            var displayText = FullText[.._displayLen];
            using var typeFont = new Font("Consolas", 24, FontStyle.Bold);

            var textSize = TextRenderer.MeasureText(displayText + "|", typeFont);
            int textX = (this.Width - TextRenderer.MeasureText(FullText, typeFont).Width) / 2;
            int textY = currentY;

            // Draw typed text
            TextRenderer.DrawText(g, displayText, typeFont,
                new Point(textX, textY), Theme.TextPrimary, TextFormatFlags.NoPadding);

            // Draw cursor
            if (_cursorVisible)
            {
                var partSize = TextRenderer.MeasureText(displayText, typeFont);
                int cursorX = textX + partSize.Width - 6;
                using var cursorBrush = new SolidBrush(Theme.AccentBlue);
                g.FillRectangle(cursorBrush, cursorX, textY + 2, 3, 34);
            }

            currentY = textY + 48;

            // Subtitle
            var subText = "POS Satış Sistemi";
            using var sf = new Font("Segoe UI", 15);
            var subSize = g.MeasureString(subText, sf);
            using var subBrush = new SolidBrush(Theme.TextSecondary);
            g.DrawString(subText, sf, subBrush, (this.Width - subSize.Width) / 2, currentY);

            // Version at bottom-right
            using var verBrush = new SolidBrush(Theme.TextMuted);
            g.DrawString($"v{UpdateService.CurrentVersion}", Theme.FontSmall, verBrush, this.Width - 70, this.Height - 28);

            // Bottom accent line with gradient
            using var lineBrush = new LinearGradientBrush(
                new Rectangle(0, this.Height - 3, this.Width, 3),
                Theme.AccentBlue,
                Theme.AccentTeal,
                LinearGradientMode.Horizontal);
            g.FillRectangle(lineBrush, 0, this.Height - 3, this.Width, 3);

            // Top thin accent line
            using var topLineBrush = new LinearGradientBrush(
                new Rectangle(0, 0, this.Width, 2),
                Theme.AccentTeal,
                Theme.AccentBlue,
                LinearGradientMode.Horizontal);
            g.FillRectangle(topLineBrush, 0, 0, this.Width, 2);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            _fadeInTimer.Stop(); _holdTimer.Stop(); _fadeOutTimer.Stop(); _typeTimer.Stop();
            _logoImage?.Dispose();
        }
    }
}
