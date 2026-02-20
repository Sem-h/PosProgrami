namespace PosProjesi.Models
{
    public class SatisDetay
    {
        public int Id { get; set; }
        public int SatisId { get; set; }
        public int UrunId { get; set; }
        public string? UrunAdi { get; set; }
        public int Miktar { get; set; }
        public decimal BirimFiyat { get; set; }
        public decimal ToplamFiyat { get; set; }
    }
}
