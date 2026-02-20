using System.Drawing.Drawing2D;
using System.Drawing.Text;
using PosProjesi.UI;

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
            this.Size = new Size(780, 520);
            this.MinimumSize = new Size(640, 440);

            var header = Theme.CreateHeaderBar("Yönetim Paneli", Theme.AccentOrange);

            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(40, 36, 40, 20),
                BackColor = Theme.BgDark
            };

            var items = new (string title, string desc, string icon, Color accent, Action action)[]
            {
                ("Ürün Yönetimi",  "Ürün ekle, düzenle, stok\nve fiyat güncelle",  "📦", Theme.AccentBlue,   () => new UrunYonetimForm().ShowDialog()),
                ("Raporlar",       "Satış raporları, en çok\nsatanlar ve istatistik","📊", Theme.AccentPurple, () => new RaporForm().ShowDialog()),
                ("Kategoriler",    "Kategori ekle, düzenle\nve organize et",       "🏷️", Theme.AccentTeal,   () => new KategoriYonetimForm().ShowDialog()),
                ("Geri Dön",       "Ana menüye geri dön",                          "↩️",  Theme.TextMuted,    () => this.Close()),
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

        private void LayoutCards()
        {
            if (_contentPanel == null || _cards.Count == 0) return;

            int padX = _contentPanel.Padding.Left;
            int padY = _contentPanel.Padding.Top;
            int availW = _contentPanel.ClientSize.Width - padX - _contentPanel.Padding.Right;
            int availH = _contentPanel.ClientSize.Height - padY - _contentPanel.Padding.Bottom;

            int cols = 2;
            int rows = (_cards.Count + cols - 1) / cols;
            int gap = 20;

            int cardW = (availW - gap * (cols - 1)) / cols;
            cardW = Math.Max(cardW, 220);

            int cardH = (availH - gap * (rows - 1)) / rows;
            cardH = Math.Clamp(cardH, 140, 240);

            for (int i = 0; i < _cards.Count; i++)
            {
                int col = i % cols;
                int row = i / cols;
                _cards[i].Size = new Size(cardW, cardH);
                _cards[i].Location = new Point(
                    padX + col * (cardW + gap),
                    padY + row * (cardH + gap));
                _cards[i].Invalidate();
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
                using var path = Theme.RoundedRect(rect, 10);

                // Background
                var bg = isHover ? Theme.BgHover : Theme.BgCard;
                using var bgBrush = new SolidBrush(bg);
                g.FillPath(bgBrush, path);

                // Border
                using var borderPen = new Pen(isHover ? accent : Theme.Border, isHover ? 1.5f : 1f);
                g.DrawPath(borderPen, path);

                // Left accent bar
                g.FillRectangle(new SolidBrush(accent), 0, 12, 4, card.Height - 24);

                // Icon (emoji)
                using var iconFont = new Font("Segoe UI Emoji", 28);
                TextRenderer.DrawText(g, icon, iconFont, new Point(20, 18), accent,
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

                // Title
                using var titleFont = new Font("Segoe UI", 14, FontStyle.Bold);
                TextRenderer.DrawText(g, title, titleFont, new Point(20, 70),
                    Theme.TextPrimary, TextFormatFlags.NoPadding);

                // Description
                using var descFont = new Font("Segoe UI", 9.5f);
                var descRect = new Rectangle(20, 98, card.Width - 40, card.Height - 105);
                TextRenderer.DrawText(g, desc, descFont, descRect,
                    Theme.TextSecondary,
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);

                // Hover arrow
                if (isHover && accent != Theme.TextMuted)
                {
                    using var arrowFont = new Font("Segoe UI", 14, FontStyle.Bold);
                    TextRenderer.DrawText(g, "→", arrowFont,
                        new Point(card.Width - 36, card.Height / 2 - 12), accent,
                        TextFormatFlags.NoPadding);
                }
            };

            card.MouseEnter += (s, e) => { isHover = true; card.Invalidate(); };
            card.MouseLeave += (s, e) => { isHover = false; card.Invalidate(); };

            return card;
        }
    }
}

