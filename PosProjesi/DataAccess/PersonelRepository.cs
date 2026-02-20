using Dapper;
using PosProjesi.Database;
using PosProjesi.Models;

namespace PosProjesi.DataAccess
{
    public class PersonelRepository
    {
        public List<Personel> GetAll()
        {
            using var db = DatabaseHelper.GetConnection();
            return db.Query<Personel>("SELECT * FROM Personeller ORDER BY Ad, Soyad").ToList();
        }

        public Personel? GetById(int id)
        {
            using var db = DatabaseHelper.GetConnection();
            return db.QueryFirstOrDefault<Personel>("SELECT * FROM Personeller WHERE Id = @Id", new { Id = id });
        }

        public Personel? Authenticate(int personelId, string sifre)
        {
            using var db = DatabaseHelper.GetConnection();
            return db.QueryFirstOrDefault<Personel>(
                "SELECT * FROM Personeller WHERE Id = @Id AND Sifre = @Sifre",
                new { Id = personelId, Sifre = sifre });
        }

        public int Add(Personel personel)
        {
            using var db = DatabaseHelper.GetConnection();
            return db.ExecuteScalar<int>(
                @"INSERT INTO Personeller (Ad, Soyad, Sifre) VALUES (@Ad, @Soyad, @Sifre);
                  SELECT last_insert_rowid();", personel);
        }

        public void Update(Personel personel)
        {
            using var db = DatabaseHelper.GetConnection();
            db.Execute(
                "UPDATE Personeller SET Ad = @Ad, Soyad = @Soyad, Sifre = @Sifre WHERE Id = @Id",
                personel);
        }

        public void Delete(int id)
        {
            using var db = DatabaseHelper.GetConnection();
            db.Execute("DELETE FROM Personeller WHERE Id = @Id", new { Id = id });
        }
    }
}
