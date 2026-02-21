namespace PosProjesi.Models
{
    public class MasaKategori
    {
        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string? OlusturmaTarihi { get; set; }

        public override string ToString() => Ad;
    }
}
