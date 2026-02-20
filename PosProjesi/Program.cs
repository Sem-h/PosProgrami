using PosProjesi.Database;
using PosProjesi.Forms;
using PosProjesi.Services;
using PosProjesi.UI;

namespace PosProjesi;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Initialize database
        DatabaseHelper.InitializeDatabase();

        // Show splash screen
        using (var splash = new SplashForm())
        {
            splash.ShowDialog();
        }

        // Check for update at startup (active check)
        try
        {
            using var startupService = new UpdateService();
            var info = startupService.CheckOnceAsync().GetAwaiter().GetResult();
            if (info != null && info.Files != null && info.Files.Count > 0)
            {
                using var prompt = new UpdatePromptDialog(info);
                var result = prompt.ShowDialog();

                if (result == DialogResult.Yes)
                {
                    ApplyStartupUpdate();
                    return; // App will restart via batch script
                }
            }
        }
        catch { }

        // Run main application
        Application.Run(new MainForm());
    }

    private static void ApplyStartupUpdate()
    {
        using var service = new UpdateService();
        var info = service.CheckOnceAsync().GetAwaiter().GetResult();

        if (info == null || info.Files == null || info.Files.Count == 0)
        {
            MessageBox.Show("İndirme bilgisi alınamadı.", "Hata",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Application.Run(new MainForm());
            return;
        }

        // Simple progress form
        var progressForm = new Form
        {
            Text = "Güncelleme İndiriliyor",
            Size = new Size(400, 130),
            StartPosition = FormStartPosition.CenterScreen,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.FromArgb(24, 26, 38)
        };

        var lbl = new Label
        {
            Text = "İndiriliyor... %0",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10),
            Location = new Point(20, 15),
            AutoSize = true
        };

        var bar = new ProgressBar
        {
            Location = new Point(20, 45),
            Size = new Size(345, 22),
            Style = ProgressBarStyle.Continuous
        };

        progressForm.Controls.Add(lbl);
        progressForm.Controls.Add(bar);

        progressForm.Shown += async (s, e) =>
        {
            var success = await service.DownloadAndApplyAsync(info, progress =>
            {
                progressForm.Invoke(() =>
                {
                    bar.Value = Math.Min(progress, 100);
                    lbl.Text = $"İndiriliyor... %{progress}";
                });
            });

            if (success)
            {
                lbl.Text = "Güncelleme uygulanıyor...";
                await Task.Delay(500);
                progressForm.Close();
                Application.Exit();
            }
            else
            {
                MessageBox.Show("İndirme başarısız oldu!", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                progressForm.Close();
                Application.Run(new MainForm());
            }
        };

        Application.Run(progressForm);
    }
}

