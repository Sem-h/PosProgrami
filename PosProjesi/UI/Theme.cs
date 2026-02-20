using System.Drawing.Drawing2D;

namespace PosProjesi.UI
{
    /// <summary>
    /// Centralized design system for the POS application.
    /// Corporate dark theme with clean, professional aesthetics.
    /// </summary>
    public static class Theme
    {
        // ── Core Palette (Professional dark) ──────────────────
        public static readonly Color BgDark       = Color.FromArgb(18, 18, 24);
        public static readonly Color BgCard       = Color.FromArgb(25, 25, 35);
        public static readonly Color BgInput      = Color.FromArgb(32, 32, 44);
        public static readonly Color BgHover      = Color.FromArgb(38, 38, 52);
        public static readonly Color Border       = Color.FromArgb(50, 50, 65);
        public static readonly Color BorderLight  = Color.FromArgb(65, 65, 82);

        // Text
        public static readonly Color TextPrimary    = Color.FromArgb(235, 235, 245);
        public static readonly Color TextSecondary   = Color.FromArgb(150, 155, 170);
        public static readonly Color TextMuted       = Color.FromArgb(100, 105, 120);

        // Accent colors (muted, corporate)
        public static readonly Color AccentBlue    = Color.FromArgb(70, 130, 220);
        public static readonly Color AccentGreen   = Color.FromArgb(50, 170, 90);
        public static readonly Color AccentOrange  = Color.FromArgb(210, 150, 60);
        public static readonly Color AccentRed     = Color.FromArgb(210, 70, 70);
        public static readonly Color AccentPurple  = Color.FromArgb(140, 100, 210);
        public static readonly Color AccentTeal    = Color.FromArgb(50, 180, 160);

        // ── Fonts ─────────────────────────────────────────────
        public static readonly Font FontTitle      = new("Segoe UI", 18, FontStyle.Bold);
        public static readonly Font FontSubtitle   = new("Segoe UI", 13, FontStyle.Bold);
        public static readonly Font FontBody       = new("Segoe UI", 9.5f);
        public static readonly Font FontBodyBold   = new("Segoe UI", 9.5f, FontStyle.Bold);
        public static readonly Font FontSmall      = new("Segoe UI", 8f);
        public static readonly Font FontMono       = new("Cascadia Code, Consolas", 9.5f);
        public static readonly Font FontBigPrice   = new("Segoe UI", 28, FontStyle.Bold);
        public static readonly Font FontButton     = new("Segoe UI", 9.5f, FontStyle.Bold);

        // ── Helpers ───────────────────────────────────────────

        public static void ApplyFormDefaults(Form form, string title)
        {
            form.Text = title;
            form.BackColor = BgDark;
            form.ForeColor = TextPrimary;
            form.Font = FontBody;
            form.StartPosition = FormStartPosition.CenterScreen;
        }

        public static Button CreateButton(string text, Color bgColor, int width = 150, int height = 36)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(width, height),
                FlatStyle = FlatStyle.Flat,
                BackColor = bgColor,
                ForeColor = Color.White,
                Font = FontButton,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Lighten(bgColor, 20);
            btn.FlatAppearance.MouseDownBackColor = Darken(bgColor, 10);
            return btn;
        }

        public static TextBox CreateTextBox(int width = 200)
        {
            var tb = new TextBox
            {
                Size = new Size(width, 28),
                Font = new Font("Segoe UI", 10),
                BackColor = BgInput,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
            return tb;
        }

        public static Label CreateLabel(string text, Color? color = null)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = color ?? TextSecondary,
                Font = FontBody
            };
        }

        public static Panel CreateHeaderBar(string title, Color accentColor)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48
            };
            panel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                using var bgBrush = new SolidBrush(BgCard);
                g.FillRectangle(bgBrush, panel.ClientRectangle);

                g.DrawString(title, FontSubtitle, new SolidBrush(TextPrimary), 20, 14);

                // Bottom accent line
                using var linePen = new Pen(accentColor, 2);
                g.DrawLine(linePen, 0, panel.Height - 1, panel.Width, panel.Height - 1);
            };
            return panel;
        }

        public static DataGridView CreateGrid()
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = BgDark,
                ForeColor = TextPrimary,
                GridColor = Border,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                Font = FontBody,
                ScrollBars = ScrollBars.Vertical
            };

            dgv.DefaultCellStyle.BackColor = BgDark;
            dgv.DefaultCellStyle.ForeColor = TextPrimary;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(40, 50, 75);
            dgv.DefaultCellStyle.SelectionForeColor = TextPrimary;
            dgv.DefaultCellStyle.Padding = new Padding(6, 4, 6, 4);

            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(22, 22, 32);

            dgv.ColumnHeadersDefaultCellStyle.BackColor = BgCard;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = TextSecondary;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 4, 6, 4);
            dgv.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            dgv.ColumnHeadersHeight = 36;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.EnableHeadersVisualStyles = false;
            dgv.RowTemplate.Height = 32;

            // Auto-resize columns when data changes
            dgv.DataBindingComplete += (s, e) => AutoFitColumns(dgv);

            return dgv;
        }

        /// <summary>
        /// Auto-fit column widths: size to content first, then stretch last column to fill.
        /// </summary>
        public static void AutoFitColumns(DataGridView dgv)
        {
            if (dgv.Columns.Count == 0) return;

            // First, auto-size all columns to their content
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }

            // Let the last column fill remaining space
            dgv.Columns[dgv.Columns.Count - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static Color Lighten(Color c, int amount)
        {
            return Color.FromArgb(c.A,
                Math.Min(255, c.R + amount),
                Math.Min(255, c.G + amount),
                Math.Min(255, c.B + amount));
        }

        public static Color Darken(Color c, int amount)
        {
            return Color.FromArgb(c.A,
                Math.Max(0, c.R - amount),
                Math.Max(0, c.G - amount),
                Math.Max(0, c.B - amount));
        }
    }
}
