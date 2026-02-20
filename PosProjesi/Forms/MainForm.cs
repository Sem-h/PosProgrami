using System.Drawing.Drawing2D;
using System.Drawing.Text;
using PosProjesi.Services;
using PosProjesi.UI;

namespace PosProjesi.Forms
{
    public class MainForm : Form
    {
        private Panel _sidePanel = null!;
        private Panel _contentPanel = null!;
        private Label _clockLabel = null!;
        private Label _dateLabel = null!;
        private readonly List<Panel> _actionCards = new();
        private readonly List<Panel> _statCards = new();
        private UpdateService? _updateService;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Theme.ApplyFormDefaults(this, "Verimek POS");
            this.WindowState = FormWindowState.Maximized;
            this.Size = new Size(1050, 680);
            this.MinimumSize = new Size(900, 550);
            this.DoubleBuffered = true;

            // ──────────────── SIDEBAR ────────────────
            _sidePanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = Color.FromArgb(14, 14, 20)
            };

            // Logo area (custom paint)
            var logoPanel = new Panel { Dock = DockStyle.Top, Height = 110 };
            logoPanel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // Logo text
                using var logoFont = new Font("Segoe UI", 24, FontStyle.Bold);
                TextRenderer.DrawText(g, "Verimek", logoFont,
                    new Point(22, 18), Theme.AccentBlue, TextFormatFlags.NoPadding);

                // Subtitle
                using var subFont = new Font("Segoe UI", 8.5f);
                TextRenderer.DrawText(g, "POS Satış Sistemi", subFont,
                    new Point(24, 66), Theme.TextMuted, TextFormatFlags.NoPadding);

                // Bottom line
                using var linePen = new Pen(Theme.Border, 1);
                g.DrawLine(linePen, 20, 95, logoPanel.Width - 20, 95);
            };
            _sidePanel.Controls.Add(logoPanel);

            // Sidebar nav items
            var navItems = new (string icon, string text, Color accent, Action action)[]
            {
                ("🏠", "Ana Sayfa",         Theme.AccentBlue,   () => {}),
                ("🛒", "Satış Ekranı",      Theme.AccentGreen,  () => OpenSatisForm()),
                ("⚙️", "Yönetim Paneli",    Theme.AccentOrange, () => OpenAdminPanel()),
                ("ℹ️",  "Program Hakkında",  Theme.AccentPurple, () => new HakkindaForm().ShowDialog()),
            };

            int btnY = 100;
            foreach (var (icon, text, accent, action) in navItems)
            {
                var btn = CreateSidebarButton(icon, text, accent, btnY);
                btn.Click += (s, e) => action();
                _sidePanel.Controls.Add(btn);
                btnY += 46;
            }

            // Version info at bottom
            var versionPanel = new Panel { Dock = DockStyle.Bottom, Height = 60 };
            versionPanel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                using var pen = new Pen(Theme.Border);
                g.DrawLine(pen, 20, 0, versionPanel.Width - 20, 0);

                using var iconFont = new Font("Segoe UI", 8);
                TextRenderer.DrawText(g, "●", iconFont, new Point(22, 18), Theme.AccentGreen, TextFormatFlags.NoPadding);
                using var verFont = new Font("Segoe UI", 8);
                TextRenderer.DrawText(g, "Sistem Aktif", verFont, new Point(36, 17), Theme.TextSecondary, TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, $"v{Services.UpdateService.CurrentVersion}  •  SQLite", verFont, new Point(22, 36), Theme.TextMuted, TextFormatFlags.NoPadding);
            };
            _sidePanel.Controls.Add(versionPanel);

            // Exit button
            var exitBtn = CreateSidebarButton("🚪", "Çıkış", Theme.AccentRed, 0);
            exitBtn.Dock = DockStyle.Bottom;
            exitBtn.Click += (s, e) => this.Close();
            _sidePanel.Controls.Add(exitBtn);

            // Sidebar right border
            var sep = new Panel { Dock = DockStyle.Right, Width = 1, BackColor = Theme.Border };
            _sidePanel.Controls.Add(sep);

            // ──────────────── CONTENT AREA ────────────────
            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgDark
            };
            _contentPanel.Paint += ContentPanel_Paint;

            // Welcome header area (custom painted in ContentPanel_Paint)
            _clockLabel = new Label
            {
                Font = new Font("Segoe UI", 32, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(40, 25)
            };

            _dateLabel = new Label
            {
                Font = new Font("Segoe UI", 11),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(42, 72)
            };

            UpdateClock();
            var clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            clockTimer.Tick += (s, e) => UpdateClock();
            clockTimer.Start();

            // ── Action Cards ──
            var actionCardDefs = new (string icon, string title, string desc, string badge, Color accent, Action action)[]
            {
                ("🛒", "Yeni Satış Başlat", "Dokunmatik POS ekranını açarak\nhızlıca satış işlemi gerçekleştirin", "F1 Kısayol", Theme.AccentGreen, () => OpenSatisForm()),
                ("⚙️", "Yönetim Paneli", "Ürün, kategori ve raporları\nyönetin, stok takibi yapın" , "Şifre Korumalı", Theme.AccentOrange, () => OpenAdminPanel()),
            };

            foreach (var (icon, title, desc, badge, accent, action) in actionCardDefs)
            {
                var card = CreateActionCard(icon, title, desc, badge, accent, 340, 180);
                card.Click += (s, e) => action();
                _actionCards.Add(card);
                _contentPanel.Controls.Add(card);
            }

            // ── Bottom stat cards ──
            var statDefs = new (string icon, string label, string value, Color accent)[]
            {
                ("💾", "Veritabanı", "SQLite Yerel", Theme.AccentBlue),
                ("📡", "Bağlantı", "Çevrimdışı Hazır", Theme.AccentTeal),
                ("🖥️", "Müşteri Ekranı", "Aktif", Theme.AccentPurple),
            };

            foreach (var (icon, label, value, accent) in statDefs)
            {
                var card = CreateStatCard(icon, label, value, accent, 220, 100);
                _statCards.Add(card);
                _contentPanel.Controls.Add(card);
            }

            _contentPanel.Controls.Add(_clockLabel);
            _contentPanel.Controls.Add(_dateLabel);

            _contentPanel.Resize += (s, e) => LayoutContentCards();

            this.Controls.Add(_contentPanel);
            this.Controls.Add(_sidePanel);

            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F1) { OpenSatisForm(); e.Handled = true; }
            };
            this.Load += (s, e) =>
            {
                LayoutContentCards();
                StartUpdateService();
            };

            this.FormClosing += (s, e) =>
            {
                _updateService?.Dispose();
            };
        }

        private void StartUpdateService()
        {
            _updateService = new UpdateService();
            _updateService.UpdateAvailable += info =>
            {
                var toast = new UpdateToastPanel(info);
                toast.ShowIn(this);
            };
        }

        private void ContentPanel_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            // Subtle gradient background at the top
            var headerRect = new Rectangle(0, 0, _contentPanel.Width, 105);
            using var headerBrush = new LinearGradientBrush(
                headerRect,
                Color.FromArgb(22, 24, 36),
                Theme.BgDark,
                LinearGradientMode.Vertical);
            g.FillRectangle(headerBrush, headerRect);

            // Subtle bottom line under header
            using var linePen = new Pen(Theme.Border, 1);
            g.DrawLine(linePen, 40, 104, _contentPanel.Width - 40, 104);
        }

        private void LayoutContentCards()
        {
            if (_contentPanel == null || _actionCards.Count == 0) return;

            int padding = 40;
            int gap = 20;
            int availW = _contentPanel.ClientSize.Width - padding * 2;
            int availH = _contentPanel.ClientSize.Height;

            // ── Action cards: fill width evenly ──
            int actionCount = _actionCards.Count;
            int actionCardW = (availW - gap * (actionCount - 1)) / actionCount;
            actionCardW = Math.Max(actionCardW, 280); // minimum width
            int actionCardH = Math.Max(180, (int)(availH * 0.36));
            actionCardH = Math.Min(actionCardH, 240); // cap height
            int cardY = 115;

            for (int i = 0; i < _actionCards.Count; i++)
            {
                var card = _actionCards[i];
                card.Size = new Size(actionCardW, actionCardH);
                card.Location = new Point(padding + i * (actionCardW + gap), cardY);
                card.Invalidate();
            }

            // ── Stat cards: fill width evenly below action cards ──
            int statCount = _statCards.Count;
            int statY = cardY + actionCardH + 24;
            int statCardW = (availW - gap * (statCount - 1)) / statCount;
            statCardW = Math.Max(statCardW, 160); // minimum width
            int statCardH = Math.Max(100, (int)(availH * 0.18));
            statCardH = Math.Min(statCardH, 130); // cap height

            for (int i = 0; i < _statCards.Count; i++)
            {
                var card = _statCards[i];
                card.Size = new Size(statCardW, statCardH);
                card.Location = new Point(padding + i * (statCardW + gap), statY);
                card.Invalidate();
            }
        }

        private void UpdateClock()
        {
            _clockLabel.Text = DateTime.Now.ToString("HH:mm:ss");
            _dateLabel.Text = DateTime.Now.ToString("dd MMMM yyyy, dddd");
        }

        private Panel CreateSidebarButton(string icon, string text, Color accent, int y)
        {
            var btn = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(219, 42),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };

            bool isHover = false;

            btn.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                if (isHover)
                {
                    using var bgBrush = new SolidBrush(Theme.BgHover);
                    g.FillRectangle(bgBrush, btn.ClientRectangle);

                    // Left accent bar
                    using var accentBrush = new SolidBrush(accent);
                    g.FillRectangle(accentBrush, 0, 6, 3, btn.Height - 12);
                }

                // Icon
                using var iconFont = new Font("Segoe UI Emoji", 12);
                TextRenderer.DrawText(g, icon, iconFont, new Point(20, 10), accent,
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

                // Text
                using var textFont = new Font("Segoe UI", 10, isHover ? FontStyle.Bold : FontStyle.Regular);
                var textColor = isHover ? Theme.TextPrimary : Theme.TextSecondary;
                TextRenderer.DrawText(g, text, textFont, new Point(48, 11), textColor,
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            };

            btn.MouseEnter += (s, e) => { isHover = true; btn.Invalidate(); };
            btn.MouseLeave += (s, e) => { isHover = false; btn.Invalidate(); };

            return btn;
        }

        private Panel CreateActionCard(string icon, string title, string desc, string badge, Color accent, int w, int h)
        {
            var card = new Panel
            {
                Size = new Size(w, h),
                Cursor = Cursors.Hand
            };

            bool isHover = false;

            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using var path = Theme.RoundedRect(rect, 12);

                // Background
                var bg = isHover ? Theme.BgHover : Theme.BgCard;
                using var bgBrush = new SolidBrush(bg);
                g.FillPath(bgBrush, path);

                // Border
                using var borderPen = new Pen(isHover ? accent : Theme.Border, isHover ? 1.5f : 1f);
                g.DrawPath(borderPen, path);

                // Top accent gradient stripe
                g.SetClip(path);
                using var accentBrush = new LinearGradientBrush(
                    new Rectangle(0, 0, card.Width, 4),
                    accent, Theme.Darken(accent, 40),
                    LinearGradientMode.Horizontal);
                g.FillRectangle(accentBrush, 0, 0, card.Width, 4);
                g.ResetClip();

                // Dynamic Y positions based on card height
                int ch = card.Height;
                int iconY = 16;
                int titleY = iconY + 62;       // +10px more spacing
                int descY = titleY + 40;       // +10px more spacing
                int badgeY = ch - 40;          // pinned to bottom

                // Icon
                using var iconFont = new Font("Segoe UI Emoji", 26);
                TextRenderer.DrawText(g, icon, iconFont, new Point(24, iconY), accent,
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

                // Title
                using var titleFont = new Font("Segoe UI", 15, FontStyle.Bold);
                TextRenderer.DrawText(g, title, titleFont, new Point(24, titleY), Theme.TextPrimary,
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

                // Description
                using var descFont = new Font("Segoe UI", 9.5f);
                var descRect = new Rectangle(24, descY, card.Width - 50, badgeY - descY - 4);
                TextRenderer.DrawText(g, desc, descFont, descRect, Theme.TextSecondary,
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);

                // Badge at bottom-left
                using var badgeFont = new Font("Segoe UI", 8, FontStyle.Bold);
                var badgeSize = TextRenderer.MeasureText(badge, badgeFont);
                var badgeRect = new Rectangle(22, badgeY, badgeSize.Width + 14, 24);
                using var badgePath = Theme.RoundedRect(badgeRect, 5);
                using var badgeBg = new SolidBrush(Color.FromArgb(30, accent.R, accent.G, accent.B));
                g.FillPath(badgeBg, badgePath);
                using var badgeBorderPen = new Pen(Color.FromArgb(60, accent.R, accent.G, accent.B), 1);
                g.DrawPath(badgeBorderPen, badgePath);
                TextRenderer.DrawText(g, badge, badgeFont,
                    new Point(29, badgeY + 4), accent, TextFormatFlags.NoPadding);

                // Hover arrow
                if (isHover)
                {
                    using var arrowFont = new Font("Segoe UI", 18, FontStyle.Bold);
                    TextRenderer.DrawText(g, "→", arrowFont,
                        new Point(card.Width - 40, card.Height / 2 - 14), accent,
                        TextFormatFlags.NoPadding);
                }
            };

            card.MouseEnter += (s, e) => { isHover = true; card.Invalidate(); };
            card.MouseLeave += (s, e) => { isHover = false; card.Invalidate(); };

            return card;
        }

        private Panel CreateStatCard(string icon, string label, string value, Color accent, int w, int h)
        {
            var card = new Panel
            {
                Size = new Size(w, h),
            };

            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using var path = Theme.RoundedRect(rect, 10);

                using var bgBrush = new SolidBrush(Theme.BgCard);
                g.FillPath(bgBrush, path);

                using var borderPen = new Pen(Theme.Border, 1);
                g.DrawPath(borderPen, path);

                // Left accent bar
                using var accentBrush = new SolidBrush(accent);
                g.FillRectangle(accentBrush, 0, 15, 3, card.Height - 30);

                // Icon
                using var iconFont = new Font("Segoe UI Emoji", 18);
                TextRenderer.DrawText(g, icon, iconFont, new Point(16, 14), accent,
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

                // Label
                using var labelFont = new Font("Segoe UI", 9);
                TextRenderer.DrawText(g, label, labelFont, new Point(16, 50), Theme.TextMuted,
                    TextFormatFlags.NoPadding);

                // Value
                using var valueFont = new Font("Segoe UI", 10.5f, FontStyle.Bold);
                TextRenderer.DrawText(g, value, valueFont, new Point(16, 70), Theme.TextPrimary,
                    TextFormatFlags.NoPadding);
            };

            return card;
        }

        private Color Darken(Color c, int amount)
        {
            return Color.FromArgb(c.A,
                Math.Max(0, c.R - amount),
                Math.Max(0, c.G - amount),
                Math.Max(0, c.B - amount));
        }

        private void OpenSatisForm()
        {
            new SatisForm().ShowDialog();
        }

        private void OpenAdminPanel()
        {
            using var loginForm = new AdminLoginForm();
            if (loginForm.ShowDialog() == DialogResult.OK && loginForm.GirisBasarili)
            {
                new AdminPanelForm().ShowDialog();
            }
        }
    }
}
