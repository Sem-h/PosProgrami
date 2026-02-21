namespace PosProjesi.Models
{
    public class Urun
    {
        public int Id { get; set; }
        public string? Barkod { get; set; }
        public string Ad { get; set; } = string.Empty;
        public int? KategoriId { get; set; }
        public string? KategoriAdi { get; set; }
        public decimal AlisFiyati { get; set; }
        public decimal SatisFiyati { get; set; }
        public int Stok { get; set; }
        public string? OlusturmaTarihi { get; set; }
        public string? ResimYolu { get; set; }
    }
}
