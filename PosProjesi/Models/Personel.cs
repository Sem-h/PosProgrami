namespace PosProjesi.Models
{
    public class Personel
    {
        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string Soyad { get; set; } = string.Empty;
        public string Sifre { get; set; } = string.Empty;
        public string? OlusturmaTarihi { get; set; }

        public string TamAd => $"{Ad} {Soyad}";
    }
}
