namespace PosProjesi.Models
{
    public class Masa
    {
        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
        public int? MasaKategoriId { get; set; }
        public string? MasaKategoriAdi { get; set; }
        public string Durum { get; set; } = "Boş"; // Boş, Dolu
        public string? AktifSepet { get; set; } // JSON serialized cart
        public string? OlusturmaTarihi { get; set; }
    }
}
