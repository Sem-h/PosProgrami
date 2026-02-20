using Dapper;
using PosProjesi.Database;
using PosProjesi.Models;

namespace PosProjesi.DataAccess
{
    public class KategoriRepository
    {
        public List<Kategori> GetAll()
        {
            using var db = DatabaseHelper.GetConnection();
            return db.Query<Kategori>("SELECT * FROM Kategoriler ORDER BY Ad").ToList();
        }

        public Kategori? GetById(int id)
        {
            using var db = DatabaseHelper.GetConnection();
            return db.QueryFirstOrDefault<Kategori>(
                "SELECT * FROM Kategoriler WHERE Id = @Id", new { Id = id });
        }

        public int Add(Kategori kategori)
        {
            using var db = DatabaseHelper.GetConnection();
            return db.ExecuteScalar<int>(
                @"INSERT INTO Kategoriler (Ad) VALUES (@Ad);
                  SELECT last_insert_rowid();", kategori);
        }

        public void Update(Kategori kategori)
        {
            using var db = DatabaseHelper.GetConnection();
            db.Execute("UPDATE Kategoriler SET Ad = @Ad WHERE Id = @Id", kategori);
        }

        public void Delete(int id)
        {
            using var db = DatabaseHelper.GetConnection();
            db.Execute("DELETE FROM Kategoriler WHERE Id = @Id", new { Id = id });
        }
    }
}
