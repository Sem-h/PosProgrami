using PosProjesi.Database;
using PosProjesi.Forms;
using PosProjesi.Models;
using PosProjesi.Services;
using PosProjesi.UI;

namespace PosProjesi;

static class Program
{
    public static Personel? ActivePersonel { get; set; }

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Global exception handler so app doesn't crash silently
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (s, e) =>
        {
            MessageBox.Show($"Beklenmeyen hata: {e.Exception.Message}\n\n{e.Exception.StackTrace}",
                "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                MessageBox.Show($"Kritik hata: {ex.Message}\n\n{ex.StackTrace}",
                    "Kritik Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

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

        // Personel login
        using (var loginForm = new PersonelLoginForm())
        {
            var loginResult = loginForm.ShowDialog();
            if (loginResult != DialogResult.OK || loginForm.SelectedPersonel == null)
            {
                return; // User cancelled — exit app
            }
            ActivePersonel = loginForm.SelectedPersonel;
        }

        // Start auto email report timer
        StartOtomatikRaporTimer();

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

    private static System.Windows.Forms.Timer? _raporTimer;

    private static void StartOtomatikRaporTimer()
    {
        _raporTimer = new System.Windows.Forms.Timer { Interval = 60_000 }; // Check every minute
        _raporTimer.Tick += (s, e) =>
        {
            try
            {
                var ayarRepo = new DataAccess.AyarlarRepository();
                var aktif = ayarRepo.GetAyar(DataAccess.AyarlarRepository.OtomatikRaporAktif, "0");
                if (aktif != "1") return;

                var saat = ayarRepo.GetAyar(DataAccess.AyarlarRepository.OtomatikRaporSaat, "23:00");
                var now = DateTime.Now;

                // Parse configured hour
                if (!int.TryParse(saat.Split(':')[0], out var targetHour)) return;
                if (now.Hour != targetHour || now.Minute > 1) return;

                // Check if already sent today
                var lastSentFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lastReportDate.txt");
                var today = now.ToString("yyyy-MM-dd");
                if (File.Exists(lastSentFile) && File.ReadAllText(lastSentFile).Trim() == today)
                    return;

                // Send report
                var service = new Services.EmailRaporService();
                service.GunlukRaporGonder(DateTime.Today);

                // Mark as sent
                File.WriteAllText(lastSentFile, today);
            }
            catch { /* Silent fail for auto reports */ }
        };
        _raporTimer.Start();
    }
}

