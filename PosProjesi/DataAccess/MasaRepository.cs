using Dapper;
using PosProjesi.Database;
using PosProjesi.Models;

namespace PosProjesi.DataAccess
{
    public class MasaRepository
    {
        // ─── Masa Kategorileri ───────────────────────

        public List<MasaKategori> GetKategoriler()
        {
            using var db = DatabaseHelper.GetConnection();
            return db.Query<MasaKategori>(
                "SELECT * FROM MasaKategorileri ORDER BY Ad").ToList();
        }

        public int AddKategori(MasaKategori kategori)
        {
            using var db = DatabaseHelper.GetConnection();
            return db.ExecuteScalar<int>(
                @"INSERT INTO MasaKategorileri (Ad) VALUES (@Ad);
                  SELECT last_insert_rowid();", kategori);
        }

        public void UpdateKategori(MasaKategori kategori)
        {
            using var db = DatabaseHelper.GetConnection();
            db.Execute("UPDATE MasaKategorileri SET Ad = @Ad WHERE Id = @Id", kategori);
        }

        public void DeleteKategori(int id)
        {
            using var db = DatabaseHelper.GetConnection();
            db.Execute("DELETE FROM MasaKategorileri WHERE Id = @Id", new { Id = id });
        }

        // ─── Masalar ─────────────────────────────────

        public List<Masa> GetAll()
        {
            using var db = DatabaseHelper.GetConnection();
            return db.Query<Masa>(
                @"SELECT m.*, mk.Ad AS MasaKategoriAdi 
                  FROM Masalar m 
                  LEFT JOIN MasaKategorileri mk ON m.MasaKategoriId = mk.Id 
                  ORDER BY mk.Ad, m.Ad").ToList();
        }

        public List<Masa> GetByKategori(int kategoriId)
        {
            using var db = DatabaseHelper.GetConnection();
            return db.Query<Masa>(
                @"SELECT m.*, mk.Ad AS MasaKategoriAdi 
                  FROM Masalar m 
                  LEFT JOIN MasaKategorileri mk ON m.MasaKategoriId = mk.Id 
                  WHERE m.MasaKategoriId = @KategoriId 
                  ORDER BY m.Ad",
                new { KategoriId = kategoriId }).ToList();
        }

        public Masa? GetById(int id)
        {
            using var db = DatabaseHelper.GetConnection();
            return db.QueryFirstOrDefault<Masa>(
                @"SELECT m.*, mk.Ad AS MasaKategoriAdi 
                  FROM Masalar m 
                  LEFT JOIN MasaKategorileri mk ON m.MasaKategoriId = mk.Id 
                  WHERE m.Id = @Id",
                new { Id = id });
        }

        public int Add(Masa masa)
        {
            using var db = DatabaseHelper.GetConnection();
            return db.ExecuteScalar<int>(
                @"INSERT INTO Masalar (Ad, MasaKategoriId, Durum) 
                  VALUES (@Ad, @MasaKategoriId, @Durum);
                  SELECT last_insert_rowid();", masa);
        }

        public void Update(Masa masa)
        {
            using var db = DatabaseHelper.GetConnection();
            db.Execute(
                @"UPDATE Masalar SET Ad = @Ad, MasaKategoriId = @MasaKategoriId 
                  WHERE Id = @Id", masa);
        }

        public void Delete(int id)
        {
            using var db = DatabaseHelper.GetConnection();
            db.Execute("DELETE FROM Masalar WHERE Id = @Id", new { Id = id });
        }

        // ─── Sipariş / Durum ─────────────────────────

        public void UpdateDurum(int masaId, string durum, string? sepetJson = null)
        {
            using var db = DatabaseHelper.GetConnection();
            db.Execute(
                @"UPDATE Masalar SET Durum = @Durum, AktifSepet = @Sepet WHERE Id = @Id",
                new { Id = masaId, Durum = durum, Sepet = sepetJson });
        }

        public void SaveSepet(int masaId, string sepetJson)
        {
            using var db = DatabaseHelper.GetConnection();
            db.Execute(
                @"UPDATE Masalar SET Durum = 'Dolu', AktifSepet = @Sepet WHERE Id = @Id",
                new { Id = masaId, Sepet = sepetJson });
        }

        public void ClearMasa(int masaId)
        {
            using var db = DatabaseHelper.GetConnection();
            db.Execute(
                @"UPDATE Masalar SET Durum = 'Boş', AktifSepet = NULL WHERE Id = @Id",
                new { Id = masaId });
        }
    }
}
