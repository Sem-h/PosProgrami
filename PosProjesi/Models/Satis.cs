namespace PosProjesi.Models
{
    public class Satis
    {
        public int Id { get; set; }
        public string? SatisTarihi { get; set; }
        public decimal ToplamTutar { get; set; }
        public string OdemeTipi { get; set; } = string.Empty;
        public string? KasiyerAdi { get; set; }
        public int? PersonelId { get; set; }
    }
}
