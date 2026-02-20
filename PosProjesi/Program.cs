using PosProjesi.Database;
using PosProjesi.Forms;

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

        // Run main application
        Application.Run(new MainForm());
    }
}