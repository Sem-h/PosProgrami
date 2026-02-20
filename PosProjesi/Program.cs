using PosProjesi.Database;
using PosProjesi.Forms;
using PosProjesi.Services;

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

        // Check if update arrived while app was closed
        if (UpdateService.HasPendingUpdate(out var pendingVersion))
        {
            MessageBox.Show(
                $"Yeni bir güncelleme mevcut: v{pendingVersion}\n\n" +
                $"Mevcut sürüm: v{UpdateService.CurrentVersion}",
                "Güncelleme Bildirimi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // Run main application
        Application.Run(new MainForm());
    }
}
