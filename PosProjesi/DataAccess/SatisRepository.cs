using Dapper;
using PosProjesi.Database;
using PosProjesi.Models;

namespace PosProjesi.DataAccess
{
    public class SatisRepository
    {
        private readonly UrunRepository _urunRepo = new();

        public int CreateSatis(Satis satis, List<SatisDetay> detaylar)
        {
            using var db = DatabaseHelper.GetConnection();
            using var transaction = db.BeginTransaction();

            try
            {
                // Insert sale header
                var satisId = db.ExecuteScalar<int>(
                    @"INSERT INTO Satislar (ToplamTutar, OdemeTipi, KasiyerAdi, PersonelId, MasaId, MasaAdi) 
                      VALUES (@ToplamTutar, @OdemeTipi, @KasiyerAdi, @PersonelId, @MasaId, @MasaAdi);
                      SELECT last_insert_rowid();",
                    satis, transaction);

                // Insert sale details and update stock
                foreach (var detay in detaylar)
                {
                    detay.SatisId = satisId;
                    db.Execute(
                        @"INSERT INTO SatisDetaylari (SatisId, UrunId, Miktar, BirimFiyat, ToplamFiyat) 
                          VALUES (@SatisId, @UrunId, @Miktar, @BirimFiyat, @ToplamFiyat)",
                        detay, transaction);

                    // Update stock
                    db.Execute(
                        "UPDATE Urunler SET Stok = Stok - @Miktar WHERE Id = @UrunId",
                        new { detay.Miktar, detay.UrunId }, transaction);
                }

                transaction.Commit();
                return satisId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public List<Satis> GetSatislar(DateTime? baslangic = null, DateTime? bitis = null, int? personelId = null)
        {
            using var db = DatabaseHelper.GetConnection();
            var sql = "SELECT * FROM Satislar";
            var conditions = new List<string>();

            if (baslangic.HasValue)
                conditions.Add("SatisTarihi >= @Baslangic");
            if (bitis.HasValue)
                conditions.Add("SatisTarihi <= @Bitis");
            if (personelId.HasValue)
                conditions.Add("PersonelId = @PersonelId");

            if (conditions.Count > 0)
                sql += " WHERE " + string.Join(" AND ", conditions);

            sql += " ORDER BY SatisTarihi DESC";

            return db.Query<Satis>(sql, new
            {
                Baslangic = baslangic?.ToString("yyyy-MM-dd 00:00:00"),
                Bitis = bitis?.ToString("yyyy-MM-dd 23:59:59"),
                PersonelId = personelId
            }).ToList();
        }

        public List<SatisDetay> GetSatisDetaylari(int satisId)
        {
            using var db = DatabaseHelper.GetConnection();
            return db.Query<SatisDetay>(
                @"SELECT sd.*, u.Ad AS UrunAdi 
                  FROM SatisDetaylari sd 
                  JOIN Urunler u ON sd.UrunId = u.Id 
                  WHERE sd.SatisId = @SatisId",
                new { SatisId = satisId }).ToList();
        }

        public decimal GetGunlukToplam(DateTime tarih)
        {
            using var db = DatabaseHelper.GetConnection();
            return db.ExecuteScalar<decimal>(
                @"SELECT COALESCE(SUM(ToplamTutar), 0) FROM Satislar 
                  WHERE SatisTarihi >= @Baslangic AND SatisTarihi <= @Bitis",
                new
                {
                    Baslangic = tarih.ToString("yyyy-MM-dd 00:00:00"),
                    Bitis = tarih.ToString("yyyy-MM-dd 23:59:59")
                });
        }

        public List<dynamic> GetEnCokSatanUrunler(int top = 10, DateTime? baslangic = null, DateTime? bitis = null)
        {
            using var db = DatabaseHelper.GetConnection();
            var sql = @"SELECT u.Ad AS UrunAdi, SUM(sd.Miktar) AS ToplamMiktar, 
                        SUM(sd.ToplamFiyat) AS ToplamTutar
                        FROM SatisDetaylari sd
                        JOIN Urunler u ON sd.UrunId = u.Id
                        JOIN Satislar s ON sd.SatisId = s.Id";

            var conditions = new List<string>();
            if (baslangic.HasValue)
                conditions.Add("s.SatisTarihi >= @Baslangic");
            if (bitis.HasValue)
                conditions.Add("s.SatisTarihi <= @Bitis");

            if (conditions.Count > 0)
                sql += " WHERE " + string.Join(" AND ", conditions);

            sql += " GROUP BY u.Id, u.Ad ORDER BY ToplamMiktar DESC LIMIT @Top";

            return db.Query(sql, new
            {
                Top = top,
                Baslangic = baslangic?.ToString("yyyy-MM-dd 00:00:00"),
                Bitis = bitis?.ToString("yyyy-MM-dd 23:59:59")
            }).ToList();
        }
    }
}
