using System.Drawing.Drawing2D;
using System.Drawing.Text;
using PosProjesi.UI;
using PosProjesi.Services;

namespace PosProjesi.Forms
{
    public class AdminPanelForm : Form
    {
        private Panel _contentPanel = null!;
        private readonly List<Panel> _cards = new();

        public AdminPanelForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Theme.ApplyFormDefaults(this, "Verimek POS - Yönetim");
            this.Size = new Size(780, 560);
            this.MinimumSize = new Size(640, 480);

            var header = Theme.CreateHeaderBar("Yönetim Paneli", Theme.AccentOrange);

            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(32, 28, 32, 20),
                BackColor = Theme.BgDark
            };

            var items = new (string title, string desc, string icon, Color accent, Action action)[]
            {
                ("Ürün Yönetimi",  "Ürün ekle, düzenle, stok ve fiyat güncelle",     "📦", Theme.AccentBlue,   () => new UrunYonetimForm().ShowDialog()),
                ("Raporlar",       "Satış raporları, en çok satanlar ve istatistik",  "📊", Theme.AccentPurple, () => new RaporForm().ShowDialog()),
                ("Kategoriler",    "Kategori ekle, düzenle ve organize et",           "🏷️", Theme.AccentTeal,   () => new KategoriYonetimForm().ShowDialog()),
                ("Güncelleme Kontrol", "Yeni sürüm kontrolü yap ve güncelle",         "🔄", Theme.AccentGreen,  CheckForUpdate),
                ("Geri Dön",       "Ana menüye geri dön",                             "↩️",  Theme.TextMuted,    () => this.Close()),
            };

            foreach (var (title, desc, icon, accent, action) in items)
            {
                var card = CreateCard(title, desc, icon, accent, 310, 160);
                card.Click += (s, e) => action();
                _cards.Add(card);
                _contentPanel.Controls.Add(card);
            }

            _contentPanel.Resize += (s, e) => LayoutCards();

            this.Controls.Add(_contentPanel);
            this.Controls.Add(header);

            this.Load += (s, e) => LayoutCards();
        }

        private async void CheckForUpdate()
        {
            using var service = new UpdateService();
            var info = await service.CheckOnceAsync();

            if (info == null)
            {
                MessageBox.Show(
                    $"Mevcut sürüm: v{UpdateService.CurrentVersion}\n\nGüncelsiniz! Yeni güncelleme bulunamadı.",
                    "Güncelleme Kontrolü",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Yeni sürüm: v{info.Version}\nMevcut sürüm: v{UpdateService.CurrentVersion}\n\n{info.Notes}\n\nŞimdi güncellemek ister misiniz?",
                "Güncelleme Mevcut",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                var toast = new UpdateToastPanel(info);
                toast.ShowIn(this);
            }
        }

        private void LayoutCards()
        {
            if (_contentPanel == null || _cards.Count == 0) return;

            int padX = _contentPanel.Padding.Left;
            int padY = _contentPanel.Padding.Top;
            int availW = _contentPanel.ClientSize.Width - padX - _contentPanel.Padding.Right;
            int availH = _contentPanel.ClientSize.Height - padY - _contentPanel.Padding.Bottom;

            // Use 3 cols for first row (3 cards), 2 cols for second row (2 cards)
            int gap = 16;

            // First row: 3 cards
            int firstRowCount = Math.Min(3, _cards.Count);
            int cardW3 = (availW - gap * (firstRowCount - 1)) / firstRowCount;
            cardW3 = Math.Max(cardW3, 180);

            int totalRows = _cards.Count <= 3 ? 1 : 2;
            int cardH = (availH - gap * (totalRows - 1)) / totalRows;
            cardH = Math.Clamp(cardH, 140, 220);

            for (int i = 0; i < firstRowCount; i++)
            {
                _cards[i].Size = new Size(cardW3, cardH);
                _cards[i].Location = new Point(padX + i * (cardW3 + gap), padY);
                _cards[i].Invalidate();
            }

            // Second row: remaining cards
            int secondRowCount = _cards.Count - firstRowCount;
            if (secondRowCount > 0)
            {
                int cardW2 = (availW - gap * (secondRowCount - 1)) / secondRowCount;
                cardW2 = Math.Max(cardW2, 180);

                for (int i = 0; i < secondRowCount; i++)
                {
                    int ci = firstRowCount + i;
                    _cards[ci].Size = new Size(cardW2, cardH);
                    _cards[ci].Location = new Point(padX + i * (cardW2 + gap), padY + cardH + gap);
                    _cards[ci].Invalidate();
                }
            }
        }

        private Panel CreateCard(string title, string desc, string icon, Color accent, int w, int h)
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
                using var path = Theme.RoundedRect(rect, 14);

                // Background with subtle gradient on hover
                if (isHover)
                {
                    using var bgBrush = new LinearGradientBrush(
                        rect,
                        Color.FromArgb(36, 38, 52),
                        Color.FromArgb(28, 30, 42),
                        LinearGradientMode.Vertical);
                    g.FillPath(bgBrush, path);
                }
                else
                {
                    using var bgBrush = new SolidBrush(Theme.BgCard);
                    g.FillPath(bgBrush, path);
                }

                // Border
                using var borderPen = new Pen(isHover ? accent : Theme.Border, isHover ? 1.5f : 1f);
                g.DrawPath(borderPen, path);

                // Top accent gradient stripe
                g.SetClip(path);
                using var accentBrush = new LinearGradientBrush(
                    new Rectangle(0, 0, card.Width, 3),
                    accent, Theme.Darken(accent, 40),
                    LinearGradientMode.Horizontal);
                g.FillRectangle(accentBrush, 0, 0, card.Width, 3);
                g.ResetClip();

                // Dynamic Y positions
                int ch = card.Height;
                int iconY = 18;
                int titleY = iconY + 50;
                int descY = titleY + 30;

                // Icon circle background
                var iconBgRect = new Rectangle(20, iconY, 44, 44);
                using var iconBgBrush = new SolidBrush(Color.FromArgb(25, accent.R, accent.G, accent.B));
                g.FillEllipse(iconBgBrush, iconBgRect);
                using var iconBorderPen = new Pen(Color.FromArgb(40, accent.R, accent.G, accent.B), 1);
                g.DrawEllipse(iconBorderPen, iconBgRect);

                // Icon emoji centered in circle
                using var iconFont = new Font("Segoe UI Emoji", 18);
                var iconSize = TextRenderer.MeasureText(icon, iconFont);
                int iconX = iconBgRect.X + (iconBgRect.Width - iconSize.Width) / 2 + 2;
                int iconTextY = iconBgRect.Y + (iconBgRect.Height - iconSize.Height) / 2 + 1;
                TextRenderer.DrawText(g, icon, iconFont, new Point(iconX, iconTextY), accent,
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

                // Title
                using var titleFont = new Font("Segoe UI", 14, FontStyle.Bold);
                TextRenderer.DrawText(g, title, titleFont, new Point(22, titleY),
                    Theme.TextPrimary, TextFormatFlags.NoPadding);

                // Description
                using var descFont = new Font("Segoe UI", 9.5f);
                var descRect = new Rectangle(22, descY, card.Width - 44, ch - descY - 10);
                TextRenderer.DrawText(g, desc, descFont, descRect,
                    Theme.TextSecondary,
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);

                // Hover arrow
                if (isHover && accent != Theme.TextMuted)
                {
                    using var arrowFont = new Font("Segoe UI", 16, FontStyle.Bold);
                    TextRenderer.DrawText(g, "→", arrowFont,
                        new Point(card.Width - 38, ch / 2 - 12), accent,
                        TextFormatFlags.NoPadding);
                }
            };

            card.MouseEnter += (s, e) => { isHover = true; card.Invalidate(); };
            card.MouseLeave += (s, e) => { isHover = false; card.Invalidate(); };

            return card;
        }
    }
}
