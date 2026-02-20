using System.Drawing.Drawing2D;
using System.Drawing.Text;
using PosProjesi.DataAccess;
using PosProjesi.Models;
using PosProjesi.UI;
using PosProjesi.Services;

namespace PosProjesi.Forms
{
    public class PersonelLoginForm : Form
    {
        public Personel? SelectedPersonel { get; private set; }

        private readonly PersonelRepository _repo = new();
        private List<Personel> _personeller = new();
        private Panel _cardsPanel = null!;
        private Panel _passwordPanel = null!;
        private TextBox _txtSifre = null!;
        private Label _lblPersonelName = null!;
        private Label _lblError = null!;
        private Personel? _selectedForLogin;

        // Animation
        private float _fadeProgress = 0f;
        private readonly System.Windows.Forms.Timer _fadeTimer;
        private bool _fadingToPassword = false;

        // Accent gradient colors
        private static readonly Color GradStart = Color.FromArgb(56, 128, 255);
        private static readonly Color GradMid = Color.FromArgb(100, 80, 240);
        private static readonly Color GradEnd = Color.FromArgb(50, 200, 180);

        // Personel card colors (rotating)
        private static readonly Color[] AvatarColors = new[]
        {
            Color.FromArgb(56, 128, 255),   // Blue
            Color.FromArgb(140, 100, 220),  // Purple
            Color.FromArgb(50, 180, 160),   // Teal
            Color.FromArgb(210, 150, 60),   // Amber
            Color.FromArgb(210, 70, 100),   // Rose
            Color.FromArgb(50, 170, 90),    // Green
        };

        // Clock
        private readonly System.Windows.Forms.Timer _clockTimer;
        private string _timeStr = "";
        private string _dateStr = "";

        public PersonelLoginForm()
        {
            this.Text = "Personel Girişi";
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(1000, 650);
            this.BackColor = Color.FromArgb(12, 12, 20);
            this.DoubleBuffered = true;
            this.KeyPreview = true;

            _fadeTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _fadeTimer.Tick += FadeTimer_Tick;

            _clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _clockTimer.Tick += (s, e) => { UpdateClock(); };
            UpdateClock();
            _clockTimer.Start();

            InitUI();
            LoadPersoneller();
        }

        private void UpdateClock()
        {
            _timeStr = DateTime.Now.ToString("HH:mm");
            _dateStr = DateTime.Now.ToString("dd MMMM yyyy, dddd");
        }

        private void FadeTimer_Tick(object? sender, EventArgs e)
        {
            _fadeProgress += 0.08f;
            if (_fadeProgress >= 1f)
            {
                _fadeProgress = 1f;
                _fadeTimer.Stop();
            }
            if (_fadingToPassword)
                _cardsPanel.Invalidate();
            else
                _passwordPanel.Invalidate();
        }

        private void InitUI()
        {
            // ── Background paint ──
            this.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Deep gradient background
                using var bgBrush = new LinearGradientBrush(
                    this.ClientRectangle,
                    Color.FromArgb(12, 12, 20),
                    Color.FromArgb(16, 14, 28),
                    LinearGradientMode.ForwardDiagonal);
                g.FillRectangle(bgBrush, this.ClientRectangle);

                // Large ambient glow - top left
                using var glow1 = new GraphicsPath();
                glow1.AddEllipse(-120, -120, 500, 400);
                using var glow1Brush = new PathGradientBrush(glow1)
                {
                    CenterColor = Color.FromArgb(12, GradStart),
                    SurroundColors = new[] { Color.Transparent }
                };
                g.FillPath(glow1Brush, glow1);

                // Large ambient glow - bottom right
                using var glow2 = new GraphicsPath();
                glow2.AddEllipse(this.Width - 350, this.Height - 300, 500, 400);
                using var glow2Brush = new PathGradientBrush(glow2)
                {
                    CenterColor = Color.FromArgb(10, GradMid),
                    SurroundColors = new[] { Color.Transparent }
                };
                g.FillPath(glow2Brush, glow2);

                // Center subtle glow
                using var glow3 = new GraphicsPath();
                glow3.AddEllipse(this.Width / 2 - 250, this.Height / 2 - 180, 500, 360);
                using var glow3Brush = new PathGradientBrush(glow3)
                {
                    CenterColor = Color.FromArgb(6, GradEnd),
                    SurroundColors = new[] { Color.Transparent }
                };
                g.FillPath(glow3Brush, glow3);

                // Top accent bar (gradient)
                using var topBarBrush = new LinearGradientBrush(
                    new Rectangle(0, 0, this.Width, 3),
                    GradStart, GradEnd, LinearGradientMode.Horizontal);
                g.FillRectangle(topBarBrush, 0, 0, this.Width, 3);

                // Fine border
                using var borderPen = new Pen(Color.FromArgb(25, 255, 255, 255), 1f);
                g.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
            };

            // ── Header area ──
            var header = new Panel { Dock = DockStyle.Top, Height = 160, BackColor = Color.Transparent };
            header.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                int centerX = this.Width / 2;

                // Clock display
                using var clockFont = new Font("Segoe UI", 36, FontStyle.Bold);
                var clockSize = TextRenderer.MeasureText(_timeStr, clockFont);
                TextRenderer.DrawText(g, _timeStr, clockFont,
                    new Point((this.Width - clockSize.Width) / 2, 14),
                    Color.FromArgb(240, 240, 255), TextFormatFlags.NoPadding);

                // Date
                using var dateFont = new Font("Segoe UI", 11);
                var dateSize = TextRenderer.MeasureText(_dateStr, dateFont);
                TextRenderer.DrawText(g, _dateStr, dateFont,
                    new Point((this.Width - dateSize.Width) / 2, 96),
                    Color.FromArgb(110, 115, 140), TextFormatFlags.NoPadding);

                // Divider line
                int divY = 126;
                using var divPen = new Pen(Color.FromArgb(30, 255, 255, 255), 1f);
                g.DrawLine(divPen, 100, divY, this.Width - 100, divY);

                // Title
                using var titleFont = new Font("Segoe UI", 13, FontStyle.Bold);
                var title = "Personel seçin";
                var titleSize = TextRenderer.MeasureText(title, titleFont);
                TextRenderer.DrawText(g, title, titleFont,
                    new Point((this.Width - titleSize.Width) / 2, 134),
                    Color.FromArgb(180, 185, 200), TextFormatFlags.NoPadding);
            };

            // ── Cards panel (personnel list) ──
            _cardsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(50, 15, 50, 15)
            };

            // ── Password panel (hidden initially) ──
            _passwordPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Visible = false
            };
            BuildPasswordPanel();

            // ── Footer ──
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.Transparent };
            footer.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // Separator line
                using var linePen = new Pen(Color.FromArgb(20, 255, 255, 255), 1f);
                g.DrawLine(linePen, 40, 0, this.Width - 40, 0);

                // Version
                using var font = new Font("Segoe UI", 8.5f);
                var ver = $"v{UpdateService.CurrentVersion}";
                TextRenderer.DrawText(g, ver, font,
                    new Point(20, 16), Color.FromArgb(70, 75, 90), TextFormatFlags.NoPadding);

                // Company name
                var company = "Verimek POS Sistemi";
                var compSize = TextRenderer.MeasureText(company, font);
                TextRenderer.DrawText(g, company, font,
                    new Point(this.Width - compSize.Width - 20, 16),
                    Color.FromArgb(70, 75, 90), TextFormatFlags.NoPadding);

                // ESC hint - center
                using var hintFont = new Font("Segoe UI", 8f);
                var hint = "ESC — Çıkış";
                var hintSize = TextRenderer.MeasureText(hint, hintFont);
                TextRenderer.DrawText(g, hint, hintFont,
                    new Point((this.Width - hintSize.Width) / 2, 17),
                    Color.FromArgb(55, 60, 75), TextFormatFlags.NoPadding);
            };

            this.Controls.Add(_cardsPanel);
            this.Controls.Add(_passwordPanel);
            this.Controls.Add(header);
            this.Controls.Add(footer);

            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    if (_passwordPanel.Visible)
                        ShowCardList();
                    else
                    {
                        this.DialogResult = DialogResult.Cancel;
                        this.Close();
                    }
                }
            };
        }

        private void BuildPasswordPanel()
        {
            int formWidth = 1000;
            int centerX = formWidth / 2;

            // Main card dimensions
            int cardW = 420, cardH = 400;
            int cardX = centerX - cardW / 2;
            int cardY = 10;

            _passwordPanel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                float alpha = Math.Min(1f, _fadeProgress);

                // Card background
                var cardRect = new Rectangle(cardX, cardY, cardW, cardH);
                using var cardPath = Theme.RoundedRect(cardRect, 20);

                // Card fill - glassmorphism effect
                using var cardBg = new LinearGradientBrush(cardRect,
                    Color.FromArgb((int)(alpha * 255), 22, 22, 36),
                    Color.FromArgb((int)(alpha * 255), 18, 18, 30),
                    LinearGradientMode.Vertical);
                g.FillPath(cardBg, cardPath);

                // Card border with gradient
                using var borderPen = new Pen(Color.FromArgb((int)(alpha * 50), 255, 255, 255), 1f);
                g.DrawPath(borderPen, cardPath);

                // Top accent bar inside card
                g.SetClip(cardPath);
                using var accentBrush = new LinearGradientBrush(
                    new Rectangle(cardX, cardY, cardW, 4),
                    GradStart, GradEnd, LinearGradientMode.Horizontal);
                g.FillRectangle(accentBrush, cardX, cardY, cardW, 4);
                g.ResetClip();

                // Glow behind avatar
                using var glowPath = new GraphicsPath();
                glowPath.AddEllipse(centerX - 60, cardY + 25, 120, 120);
                using var glowBrush = new PathGradientBrush(glowPath)
                {
                    CenterColor = Color.FromArgb((int)(alpha * 20), GradMid),
                    SurroundColors = new[] { Color.Transparent }
                };
                g.FillPath(glowBrush, glowPath);
            };

            // Large avatar circle
            var avatarPanel = new Panel
            {
                Size = new Size(90, 90),
                Location = new Point(centerX - 45, cardY + 35),
                BackColor = Color.Transparent
            };
            avatarPanel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Outer ring glow
                using var outerGlow = new Pen(Color.FromArgb(30, GradStart), 3f);
                g.DrawEllipse(outerGlow, 2, 2, 85, 85);

                // Main circle
                using var circleBrush = new LinearGradientBrush(
                    new Rectangle(0, 0, 89, 89),
                    Color.FromArgb(35, GradStart.R, GradStart.G, GradStart.B),
                    Color.FromArgb(20, GradMid.R, GradMid.G, GradMid.B),
                    LinearGradientMode.ForwardDiagonal);
                g.FillEllipse(circleBrush, 5, 5, 79, 79);

                using var circlePen = new Pen(Color.FromArgb(60, GradStart), 1.5f);
                g.DrawEllipse(circlePen, 5, 5, 79, 79);

                // Lock icon
                using var iconFont = new Font("Segoe UI Emoji", 28);
                TextRenderer.DrawText(g, "🔐", iconFont, new Point(22, 20),
                    GradStart, TextFormatFlags.NoPadding);
            };

            _lblPersonelName = new Label
            {
                AutoSize = false,
                Size = new Size(360, 34),
                Location = new Point(centerX - 180, cardY + 140),
                ForeColor = Color.FromArgb(240, 240, 255),
                Font = new Font("Segoe UI", 17, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            var lblSifreLabel = new Label
            {
                Text = "ŞİFRE",
                AutoSize = false,
                Size = new Size(320, 20),
                Location = new Point(centerX - 160, cardY + 190),
                ForeColor = Color.FromArgb(100, 105, 130),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                BackColor = Color.Transparent
            };

            // Custom password input container
            var txtContainer = new Panel
            {
                Size = new Size(320, 48),
                Location = new Point(centerX - 160, cardY + 214),
                BackColor = Color.Transparent
            };
            txtContainer.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(0, 0, txtContainer.Width - 1, txtContainer.Height - 1);
                using var path = Theme.RoundedRect(r, 12);
                using var bgBrush = new SolidBrush(Color.FromArgb(28, 28, 42));
                g.FillPath(bgBrush, path);

                bool focused = _txtSifre.Focused;
                using var borderPen = new Pen(
                    focused ? Color.FromArgb(80, GradStart) : Color.FromArgb(35, 255, 255, 255), 
                    focused ? 1.5f : 1f);
                g.DrawPath(borderPen, path);

                if (focused)
                {
                    // Bottom glow
                    using var glowPen = new Pen(Color.FromArgb(15, GradStart), 3f);
                    g.DrawPath(glowPen, path);
                }
            };

            _txtSifre = new TextBox
            {
                Size = new Size(290, 30),
                Location = new Point(15, 10),
                Font = new Font("Segoe UI", 16),
                BackColor = Color.FromArgb(28, 28, 42),
                ForeColor = Color.FromArgb(230, 230, 245),
                BorderStyle = BorderStyle.None,
                UseSystemPasswordChar = true,
                TextAlign = HorizontalAlignment.Center
            };
            _txtSifre.GotFocus += (s, e) => txtContainer.Invalidate();
            _txtSifre.LostFocus += (s, e) => txtContainer.Invalidate();
            _txtSifre.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; AttemptLogin(); }
            };
            txtContainer.Controls.Add(_txtSifre);

            _lblError = new Label
            {
                AutoSize = false,
                Size = new Size(320, 22),
                Location = new Point(centerX - 160, cardY + 272),
                ForeColor = Color.FromArgb(240, 80, 80),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Text = ""
            };

            // Login button with gradient
            var btnGiris = new Panel
            {
                Size = new Size(320, 50),
                Location = new Point(centerX - 160, cardY + 302),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            bool girisHover = false;
            btnGiris.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var r = new Rectangle(0, 0, btnGiris.Width - 1, btnGiris.Height - 1);
                using var path = Theme.RoundedRect(r, 12);

                // Gradient button
                using var bgBrush = girisHover
                    ? new LinearGradientBrush(r, Color.FromArgb(70, 140, 255), GradEnd, LinearGradientMode.Horizontal)
                    : new LinearGradientBrush(r, GradStart, Color.FromArgb(80, 110, 230), LinearGradientMode.Horizontal);
                g.FillPath(bgBrush, path);

                // Subtle shine effect on hover
                if (girisHover)
                {
                    using var shinePath = new GraphicsPath();
                    shinePath.AddEllipse(-20, -30, btnGiris.Width + 40, 40);
                    using var shineBrush = new PathGradientBrush(shinePath)
                    {
                        CenterColor = Color.FromArgb(25, 255, 255, 255),
                        SurroundColors = new[] { Color.Transparent }
                    };
                    g.SetClip(path);
                    g.FillPath(shineBrush, shinePath);
                    g.ResetClip();
                }

                using var f = new Font("Segoe UI", 13, FontStyle.Bold);
                TextRenderer.DrawText(g, "Giriş Yap →", f,
                    new Rectangle(0, 0, btnGiris.Width, btnGiris.Height),
                    Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btnGiris.MouseEnter += (s, e) => { girisHover = true; btnGiris.Invalidate(); };
            btnGiris.MouseLeave += (s, e) => { girisHover = false; btnGiris.Invalidate(); };
            btnGiris.Click += (s, e) => AttemptLogin();

            // Back button
            var btnBack = new Panel
            {
                Size = new Size(320, 36),
                Location = new Point(centerX - 160, cardY + 362),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            bool backHover = false;
            btnBack.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                using var f = new Font("Segoe UI", 10);
                var color = backHover ? GradStart : Color.FromArgb(100, 105, 130);
                TextRenderer.DrawText(g, "← Geri Dön", f,
                    new Rectangle(0, 0, btnBack.Width, btnBack.Height),
                    color, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btnBack.MouseEnter += (s, e) => { backHover = true; btnBack.Invalidate(); };
            btnBack.MouseLeave += (s, e) => { backHover = false; btnBack.Invalidate(); };
            btnBack.Click += (s, e) => ShowCardList();

            _passwordPanel.Controls.AddRange(new Control[]
            {
                avatarPanel, _lblPersonelName, lblSifreLabel,
                txtContainer, _lblError, btnGiris, btnBack
            });
        }

        private void LoadPersoneller()
        {
            _personeller = _repo.GetAll();
            _cardsPanel.Controls.Clear();
            _cardsPanel.SuspendLayout();

            // Calculate dynamic card width based on longest name
            int cardH = 220, gap = 24;
            int minCardW = 200;
            using (var measureFont = new Font("Segoe UI", 12, FontStyle.Bold))
            {
                foreach (var p in _personeller)
                {
                    var nameW = TextRenderer.MeasureText(p.TamAd, measureFont).Width + 40;
                    if (nameW > minCardW) minCardW = nameW;
                }
            }
            int cardW = minCardW;
            int availW = this.Width - _cardsPanel.Padding.Horizontal;
            int cols = Math.Min(3, Math.Max(1, (availW + gap) / (cardW + gap)));
            int totalW = cols * cardW + (cols - 1) * gap;
            int startX = (availW - totalW) / 2;

            // Center vertically if few items
            int totalRows = (int)Math.Ceiling((double)_personeller.Count / cols);
            int totalH = totalRows * (cardH + gap) - gap;
            int panelH = this.Height - 160 - 48; // header + footer
            int startY = Math.Max(0, (panelH - totalH) / 2 - 20);

            for (int i = 0; i < _personeller.Count; i++)
            {
                var p = _personeller[i];
                int col = i % cols;
                int row = i / cols;
                int colorIdx = i % AvatarColors.Length;
                var accentColor = AvatarColors[colorIdx];

                var card = new Panel
                {
                    Size = new Size(cardW, cardH),
                    Location = new Point(startX + col * (cardW + gap), startY + row * (cardH + gap)),
                    Cursor = Cursors.Hand,
                    BackColor = Color.Transparent,
                    Tag = p
                };

                bool hover = false;
                card.Paint += (s, e) =>
                {
                    var g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                    var r = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                    using var path = Theme.RoundedRect(r, 18);

                    // Card background
                    var bgColor = hover
                        ? Color.FromArgb(28, 28, 44)
                        : Color.FromArgb(20, 20, 34);
                    using var bg = new SolidBrush(bgColor);
                    g.FillPath(bg, path);

                    // Card border
                    var borderColor = hover
                        ? Color.FromArgb(80, accentColor.R, accentColor.G, accentColor.B)
                        : Color.FromArgb(25, 255, 255, 255);
                    using var bp = new Pen(borderColor, hover ? 1.5f : 1f);
                    g.DrawPath(bp, path);

                    // Top accent strip on hover
                    if (hover)
                    {
                        g.SetClip(path);
                        using var accentBrush = new LinearGradientBrush(
                            new Rectangle(0, 0, card.Width, 4),
                            accentColor,
                            Theme.Lighten(accentColor, 40),
                            LinearGradientMode.Horizontal);
                        g.FillRectangle(accentBrush, 0, 0, card.Width, 4);
                        g.ResetClip();
                    }

                    // Avatar circle
                    int avatarSize = 76;
                    int avatarX = (card.Width - avatarSize) / 2;
                    int avatarY = 28;

                    // Avatar glow on hover
                    if (hover)
                    {
                        using var glowPath = new GraphicsPath();
                        glowPath.AddEllipse(avatarX - 15, avatarY - 15, avatarSize + 30, avatarSize + 30);
                        using var glowBrush = new PathGradientBrush(glowPath)
                        {
                            CenterColor = Color.FromArgb(18, accentColor),
                            SurroundColors = new[] { Color.Transparent }
                        };
                        g.FillPath(glowBrush, glowPath);
                    }

                    // Avatar circle fill
                    using var avBg = new LinearGradientBrush(
                        new Rectangle(avatarX, avatarY, avatarSize, avatarSize),
                        Color.FromArgb(hover ? 45 : 30, accentColor.R, accentColor.G, accentColor.B),
                        Color.FromArgb(hover ? 25 : 15, accentColor.R, accentColor.G, accentColor.B),
                        LinearGradientMode.Vertical);
                    g.FillEllipse(avBg, avatarX, avatarY, avatarSize, avatarSize);

                    using var avBorder = new Pen(
                        Color.FromArgb(hover ? 120 : 50, accentColor.R, accentColor.G, accentColor.B), 
                        hover ? 2f : 1.5f);
                    g.DrawEllipse(avBorder, avatarX, avatarY, avatarSize, avatarSize);

                    // Initials
                    var initials = $"{p.Ad[0]}{p.Soyad[0]}".ToUpper();
                    using var initFont = new Font("Segoe UI", 22, FontStyle.Bold);
                    TextRenderer.DrawText(g, initials, initFont,
                        new Rectangle(avatarX, avatarY, avatarSize, avatarSize),
                        hover ? accentColor : Color.FromArgb(200, accentColor.R, accentColor.G, accentColor.B),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                    // Name
                    using var nameFont = new Font("Segoe UI", 12, FontStyle.Bold);
                    var name = p.TamAd;
                    var nameSize = TextRenderer.MeasureText(name, nameFont);
                    TextRenderer.DrawText(g, name, nameFont,
                        new Point((card.Width - nameSize.Width) / 2, 116),
                        Color.FromArgb(hover ? 255 : 220, 235, 235, 250),
                        TextFormatFlags.NoPadding);

                    // Role badge
                    using var roleFont = new Font("Segoe UI", 8f, FontStyle.Bold);
                    var role = "KASİYER";
                    var roleSize = TextRenderer.MeasureText(role, roleFont);
                    int badgeW = roleSize.Width + 16;
                    int badgeH = 22;
                    int badgeX = (card.Width - badgeW) / 2;
                    int badgeY = 144;
                    var badgeRect = new Rectangle(badgeX, badgeY, badgeW, badgeH);
                    using var badgePath = Theme.RoundedRect(badgeRect, 6);
                    using var badgeBrush = new SolidBrush(
                        Color.FromArgb(hover ? 25 : 15, accentColor.R, accentColor.G, accentColor.B));
                    g.FillPath(badgeBrush, badgePath);
                    using var badgeBorder = new Pen(
                        Color.FromArgb(hover ? 50 : 25, accentColor.R, accentColor.G, accentColor.B), 1f);
                    g.DrawPath(badgeBorder, badgePath);
                    TextRenderer.DrawText(g, role, roleFont,
                        badgeRect,
                        Color.FromArgb(hover ? 180 : 120, accentColor.R, accentColor.G, accentColor.B),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                    // "Seç" hint on hover
                    if (hover)
                    {
                        using var hintFont = new Font("Segoe UI", 9f);
                        TextRenderer.DrawText(g, "Giriş yapmak için tıklayın", hintFont,
                            new Rectangle(0, 178, card.Width, 20),
                            Color.FromArgb(90, 95, 115),
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                };

                card.MouseEnter += (s, e) => { hover = true; card.Invalidate(); };
                card.MouseLeave += (s, e) => { hover = false; card.Invalidate(); };
                card.Click += (s, e) => ShowPasswordPanel((Personel)card.Tag);

                _cardsPanel.Controls.Add(card);
            }

            _cardsPanel.ResumeLayout();
        }

        private void ShowPasswordPanel(Personel personel)
        {
            _selectedForLogin = personel;
            _lblPersonelName.Text = personel.TamAd;
            _txtSifre.Text = "";
            _lblError.Text = "";
            _cardsPanel.Visible = false;
            _passwordPanel.Visible = true;
            _txtSifre.Focus();
        }

        private void ShowCardList()
        {
            _passwordPanel.Visible = false;
            _cardsPanel.Visible = true;
            _selectedForLogin = null;
        }

        private void AttemptLogin()
        {
            if (_selectedForLogin == null) return;

            var result = _repo.Authenticate(_selectedForLogin.Id, _txtSifre.Text);
            if (result != null)
            {
                SelectedPersonel = result;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                _lblError.Text = "⚠ Şifre yanlış! Tekrar deneyin.";
                _txtSifre.SelectAll();
                _txtSifre.Focus();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            _fadeTimer.Stop();
            _clockTimer.Stop();
        }
    }
}
