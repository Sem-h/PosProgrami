using Dapper;
using PosProjesi.Database;

namespace PosProjesi.DataAccess
{
    public class AyarlarRepository
    {
        public string GetAyar(string key, string defaultValue = "")
        {
            using var db = DatabaseHelper.GetConnection();
            var value = db.ExecuteScalar<string>(
                "SELECT Deger FROM Ayarlar WHERE Anahtar = @Key",
                new { Key = key });
            return value ?? defaultValue;
        }

        public void SetAyar(string key, string value)
        {
            using var db = DatabaseHelper.GetConnection();
            var exists = db.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM Ayarlar WHERE Anahtar = @Key",
                new { Key = key });

            if (exists > 0)
            {
                db.Execute(
                    "UPDATE Ayarlar SET Deger = @Value WHERE Anahtar = @Key",
                    new { Key = key, Value = value });
            }
            else
            {
                db.Execute(
                    "INSERT INTO Ayarlar (Anahtar, Deger) VALUES (@Key, @Value)",
                    new { Key = key, Value = value });
            }
        }

        // Ayar anahtarları (constants)
        public const string YaziciAdi = "YaziciAdi";
        public const string IsletmeAdi = "IsletmeAdi";
        public const string IsletmeAdres = "IsletmeAdres";
        public const string IsletmeTelefon = "IsletmeTelefon";
        public const string KagitGenisligi = "KagitGenisligi"; // "58" or "80"
        public const string FisAltMesaj = "FisAltMesaj";
        public const string FisYazdirmaAktif = "FisYazdirmaAktif"; // "1" or "0"

        // E-posta rapor ayarları
        public const string SmtpSunucu = "SmtpSunucu";
        public const string SmtpPort = "SmtpPort";
        public const string SmtpKullanici = "SmtpKullanici";
        public const string SmtpSifre = "SmtpSifre";
        public const string SmtpSsl = "SmtpSsl"; // "1" or "0"
        public const string MailAlici = "MailAlici";
        public const string MailGonderici = "MailGonderici";
        public const string OtomatikRaporAktif = "OtomatikRaporAktif"; // "1" or "0"
        public const string OtomatikRaporSaat = "OtomatikRaporSaat"; // "23:00"

        // Garson web sunucu ayarları
        public const string GarsonAktif = "GarsonAktif"; // "1" or "0"
        public const string GarsonPort = "GarsonPort"; // "5555"
        public const string GarsonPin = "GarsonPin"; // "1234"
    }
}
