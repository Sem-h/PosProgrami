using Microsoft.Data.Sqlite;

namespace PosProjesi.Database
{
    public static class DatabaseHelper
    {
        private static readonly string DbPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "pos_database.db");

        public static string ConnectionString => $"Data Source={DbPath}";

        public static SqliteConnection GetConnection()
        {
            var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            // Enable WAL mode for better performance
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
            cmd.ExecuteNonQuery();
            return connection;
        }

        public static void InitializeDatabase()
        {
            using var connection = GetConnection();
            using var cmd = connection.CreateCommand();

            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Kategoriler (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Ad TEXT NOT NULL,
                    OlusturmaTarihi TEXT DEFAULT (datetime('now','localtime'))
                );

                CREATE TABLE IF NOT EXISTS Urunler (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Barkod TEXT UNIQUE,
                    Ad TEXT NOT NULL,
                    KategoriId INTEGER,
                    AlisFiyati REAL NOT NULL DEFAULT 0,
                    SatisFiyati REAL NOT NULL DEFAULT 0,
                    Stok INTEGER DEFAULT 0,
                    OlusturmaTarihi TEXT DEFAULT (datetime('now','localtime')),
                    FOREIGN KEY (KategoriId) REFERENCES Kategoriler(Id)
                );

                CREATE TABLE IF NOT EXISTS Satislar (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SatisTarihi TEXT DEFAULT (datetime('now','localtime')),
                    ToplamTutar REAL NOT NULL,
                    OdemeTipi TEXT NOT NULL,
                    KasiyerAdi TEXT,
                    PersonelId INTEGER
                );

                CREATE TABLE IF NOT EXISTS SatisDetaylari (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SatisId INTEGER NOT NULL,
                    UrunId INTEGER NOT NULL,
                    Miktar INTEGER NOT NULL,
                    BirimFiyat REAL NOT NULL,
                    ToplamFiyat REAL NOT NULL,
                    FOREIGN KEY (SatisId) REFERENCES Satislar(Id),
                    FOREIGN KEY (UrunId) REFERENCES Urunler(Id)
                );

                CREATE TABLE IF NOT EXISTS Personeller (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Ad TEXT NOT NULL,
                    Soyad TEXT NOT NULL,
                    Sifre TEXT NOT NULL,
                    OlusturmaTarihi TEXT DEFAULT (datetime('now','localtime'))
                );
            ";
            cmd.ExecuteNonQuery();

            // Add PersonelId column to Satislar if it doesn't exist (migration)
            try
            {
                cmd.CommandText = "ALTER TABLE Satislar ADD COLUMN PersonelId INTEGER";
                cmd.ExecuteNonQuery();
            }
            catch { /* Column already exists */ }

            // Seed default category if empty
            cmd.CommandText = "SELECT COUNT(*) FROM Kategoriler";
            var count = Convert.ToInt64(cmd.ExecuteScalar());
            if (count == 0)
            {
                cmd.CommandText = @"
                    INSERT INTO Kategoriler (Ad) VALUES ('Genel');
                    INSERT INTO Kategoriler (Ad) VALUES ('Gıda');
                    INSERT INTO Kategoriler (Ad) VALUES ('İçecek');
                    INSERT INTO Kategoriler (Ad) VALUES ('Temizlik');
                    INSERT INTO Kategoriler (Ad) VALUES ('Kırtasiye');
                ";
                cmd.ExecuteNonQuery();
            }

            // Seed default admin personel if empty
            cmd.CommandText = "SELECT COUNT(*) FROM Personeller";
            var personelCount = Convert.ToInt64(cmd.ExecuteScalar());
            if (personelCount == 0)
            {
                cmd.CommandText = "INSERT INTO Personeller (Ad, Soyad, Sifre) VALUES ('Admin', 'Yönetici', '1234')";
                cmd.ExecuteNonQuery();
            }
        }
    }
}
