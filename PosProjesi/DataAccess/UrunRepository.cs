using Dapper;
using PosProjesi.Database;
using PosProjesi.Models;

namespace PosProjesi.DataAccess
{
    public class UrunRepository
    {
        public List<Urun> GetAll()
        {
            using var db = DatabaseHelper.GetConnection();
            return db.Query<Urun>(
                @"SELECT u.*, k.Ad AS KategoriAdi 
                  FROM Urunler u 
                  LEFT JOIN Kategoriler k ON u.KategoriId = k.Id 
                  ORDER BY u.Ad").ToList();
        }

        public List<Urun> GetByKategori(int kategoriId)
        {
            using var db = DatabaseHelper.GetConnection();
            return db.Query<Urun>(
                @"SELECT u.*, k.Ad AS KategoriAdi 
                  FROM Urunler u 
                  LEFT JOIN Kategoriler k ON u.KategoriId = k.Id 
                  WHERE u.KategoriId = @KategoriId 
                  ORDER BY u.Ad",
                new { KategoriId = kategoriId }).ToList();
        }

        public Urun? GetByBarkod(string barkod)
        {
            using var db = DatabaseHelper.GetConnection();
            return db.QueryFirstOrDefault<Urun>(
                @"SELECT u.*, k.Ad AS KategoriAdi 
                  FROM Urunler u 
                  LEFT JOIN Kategoriler k ON u.KategoriId = k.Id 
                  WHERE u.Barkod = @Barkod",
                new { Barkod = barkod });
        }

        public List<Urun> Search(string searchTerm)
        {
            using var db = DatabaseHelper.GetConnection();
            return db.Query<Urun>(
                @"SELECT u.*, k.Ad AS KategoriAdi 
                  FROM Urunler u 
                  LEFT JOIN Kategoriler k ON u.KategoriId = k.Id 
                  WHERE u.Ad LIKE @Search OR u.Barkod LIKE @Search 
                  ORDER BY u.Ad",
                new { Search = $"%{searchTerm}%" }).ToList();
        }

        public int Add(Urun urun)
        {
            using var db = DatabaseHelper.GetConnection();
            return db.ExecuteScalar<int>(
                @"INSERT INTO Urunler (Barkod, Ad, KategoriId, AlisFiyati, SatisFiyati, Stok, ResimYolu) 
                  VALUES (@Barkod, @Ad, @KategoriId, @AlisFiyati, @SatisFiyati, @Stok, @ResimYolu);
                  SELECT last_insert_rowid();", urun);
        }

        public void Update(Urun urun)
        {
            using var db = DatabaseHelper.GetConnection();
            db.Execute(
                @"UPDATE Urunler SET 
                    Barkod = @Barkod, Ad = @Ad, KategoriId = @KategoriId,
                    AlisFiyati = @AlisFiyati, SatisFiyati = @SatisFiyati, Stok = @Stok,
                    ResimYolu = @ResimYolu 
                  WHERE Id = @Id", urun);
        }

        public void Delete(int id)
        {
            using var db = DatabaseHelper.GetConnection();
            db.Execute("DELETE FROM Urunler WHERE Id = @Id", new { Id = id });
        }

        public void UpdateStok(int urunId, int miktar)
        {
            using var db = DatabaseHelper.GetConnection();
            db.Execute(
                "UPDATE Urunler SET Stok = Stok - @Miktar WHERE Id = @Id",
                new { Id = urunId, Miktar = miktar });
        }
    }
}
