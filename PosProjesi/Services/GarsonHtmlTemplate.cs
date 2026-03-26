namespace PosProjesi.Services
{
    public static class GarsonHtmlTemplate
    {
        private static string? _cachedHtml;

        public static string Get(string isletmeAdi)
        {
            if (_cachedHtml == null)
            {
                var htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "garson.html");
                if (!File.Exists(htmlPath))
                    return "<html><body><h1>garson.html dosyasi bulunamadi</h1></body></html>";
                _cachedHtml = File.ReadAllText(htmlPath, System.Text.Encoding.UTF8);
            }

            var safeName = isletmeAdi
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");

            return _cachedHtml.Replace("ISLETME_ADI", safeName);
        }
    }
}
