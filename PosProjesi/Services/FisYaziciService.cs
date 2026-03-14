using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Text;
using PosProjesi.DataAccess;
using PosProjesi.Models;

namespace PosProjesi.Services
{
    public class FisYaziciService
    {
        private readonly AyarlarRepository _ayarRepo = new();

        // ESC/POS Commands
        private static readonly byte[] ESC_INIT = { 0x1B, 0x40 };           // Initialize printer
        private static readonly byte[] ESC_CENTER = { 0x1B, 0x61, 0x01 };   // Center align
        private static readonly byte[] ESC_LEFT = { 0x1B, 0x61, 0x00 };     // Left align
        private static readonly byte[] ESC_RIGHT = { 0x1B, 0x61, 0x02 };    // Right align
        private static readonly byte[] ESC_BOLD_ON = { 0x1B, 0x45, 0x01 };  // Bold on
        private static readonly byte[] ESC_BOLD_OFF = { 0x1B, 0x45, 0x00 }; // Bold off
        private static readonly byte[] ESC_DOUBLE_W = { 0x1D, 0x21, 0x10 }; // Double width
        private static readonly byte[] ESC_DOUBLE_HW = { 0x1D, 0x21, 0x11 };// Double height+width
        private static readonly byte[] ESC_NORMAL = { 0x1D, 0x21, 0x00 };   // Normal size
        private static readonly byte[] ESC_CUT = { 0x1D, 0x56, 0x42, 0x03 };// Cut paper (partial)
        private static readonly byte[] ESC_FEED = { 0x1B, 0x64, 0x05 };     // Feed 5 lines
        private static readonly byte[] LF = { 0x0A };                        // Line feed

        /// <summary>
        /// Ana fiş yazdırma metodu
        /// </summary>
        public void FisYazdir(int satisId, Satis satis, List<SatisDetay> detaylar)
        {
            var aktif = _ayarRepo.GetAyar(AyarlarRepository.FisYazdirmaAktif, "1");
            if (aktif != "1") return;

            var yaziciAdi = _ayarRepo.GetAyar(AyarlarRepository.YaziciAdi);
            var isletmeAdi = _ayarRepo.GetAyar(AyarlarRepository.IsletmeAdi, "VERİMEK POS");
            var isletmeAdres = _ayarRepo.GetAyar(AyarlarRepository.IsletmeAdres);
            var isletmeTelefon = _ayarRepo.GetAyar(AyarlarRepository.IsletmeTelefon);
            var kagitStr = _ayarRepo.GetAyar(AyarlarRepository.KagitGenisligi, "80");
            var altMesaj = _ayarRepo.GetAyar(AyarlarRepository.FisAltMesaj, "Bizi tercih ettiğiniz için teşekkürler!");

            int charWidth = kagitStr == "58" ? 32 : 48;

            // Use default printer if none specified
            if (string.IsNullOrEmpty(yaziciAdi))
            {
                var defaultPrinter = GetDefaultPrinter();
                if (string.IsNullOrEmpty(defaultPrinter))
                    throw new Exception("Yazıcı bulunamadı! Lütfen Ayarlar'dan bir yazıcı seçin.");
                yaziciAdi = defaultPrinter;
            }

            var data = BuildReceiptData(satisId, satis, detaylar,
                isletmeAdi, isletmeAdres, isletmeTelefon, altMesaj, charWidth);

            RawPrinterHelper.SendBytesToPrinter(yaziciAdi, data);
        }

        /// <summary>
        /// Test fişi yazdırma
        /// </summary>
        public void TestFisiYazdir()
        {
            var yaziciAdi = _ayarRepo.GetAyar(AyarlarRepository.YaziciAdi);
            var isletmeAdi = _ayarRepo.GetAyar(AyarlarRepository.IsletmeAdi, "VERİMEK POS");
            var kagitStr = _ayarRepo.GetAyar(AyarlarRepository.KagitGenisligi, "80");
            int charWidth = kagitStr == "58" ? 32 : 48;

            if (string.IsNullOrEmpty(yaziciAdi))
            {
                var defaultPrinter = GetDefaultPrinter();
                if (string.IsNullOrEmpty(defaultPrinter))
                    throw new Exception("Yazıcı bulunamadı! Lütfen bir yazıcı seçin.");
                yaziciAdi = defaultPrinter;
            }

            var ms = new MemoryStream();

            Write(ms, ESC_INIT);

            // Header
            Write(ms, ESC_CENTER);
            Write(ms, ESC_BOLD_ON);
            Write(ms, ESC_DOUBLE_HW);
            WriteText(ms, isletmeAdi);
            Write(ms, ESC_NORMAL);
            Write(ms, ESC_BOLD_OFF);
            Write(ms, LF);
            Write(ms, LF);

            WriteText(ms, "*** TEST FİŞİ ***");
            Write(ms, LF);
            WriteText(ms, new string('-', charWidth));
            Write(ms, LF);

            Write(ms, ESC_LEFT);
            WriteText(ms, $"Tarih: {DateTime.Now:dd.MM.yyyy HH:mm}");
            Write(ms, LF);
            WriteText(ms, $"Kağıt: {kagitStr}mm ({charWidth} karakter)");
            Write(ms, LF);
            WriteText(ms, $"Yazıcı: {yaziciAdi}");
            Write(ms, LF);
            WriteText(ms, new string('-', charWidth));
            Write(ms, LF);

            Write(ms, ESC_CENTER);
            WriteText(ms, "Yazıcı bağlantısı başarılı!");
            Write(ms, LF);
            Write(ms, LF);
            WriteText(ms, "Verimek POS Sistemi");
            Write(ms, LF);

            Write(ms, ESC_FEED);
            Write(ms, ESC_CUT);

            RawPrinterHelper.SendBytesToPrinter(yaziciAdi, ms.ToArray());
        }

        private byte[] BuildReceiptData(int satisId, Satis satis, List<SatisDetay> detaylar,
            string isletmeAdi, string isletmeAdres, string isletmeTelefon, string altMesaj, int charWidth)
        {
            var ms = new MemoryStream();

            // Initialize
            Write(ms, ESC_INIT);

            // ── HEADER: Business name ──
            Write(ms, ESC_CENTER);
            Write(ms, ESC_BOLD_ON);
            Write(ms, ESC_DOUBLE_HW);
            WriteText(ms, isletmeAdi);
            Write(ms, ESC_NORMAL);
            Write(ms, ESC_BOLD_OFF);
            Write(ms, LF);

            // Business address & phone
            if (!string.IsNullOrEmpty(isletmeAdres))
            {
                WriteText(ms, isletmeAdres);
                Write(ms, LF);
            }
            if (!string.IsNullOrEmpty(isletmeTelefon))
            {
                WriteText(ms, $"Tel: {isletmeTelefon}");
                Write(ms, LF);
            }

            Write(ms, LF);
            WriteText(ms, new string('=', charWidth));
            Write(ms, LF);

            // ── DATE / RECEIPT INFO ──
            Write(ms, ESC_LEFT);
            var tarih = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            WriteText(ms, FormatTwoColumn("Fiş No:", $"#{satisId}", charWidth));
            Write(ms, LF);
            WriteText(ms, FormatTwoColumn("Tarih:", tarih, charWidth));
            Write(ms, LF);
            WriteText(ms, FormatTwoColumn("Kasiyer:", satis.KasiyerAdi ?? "Kasiyer", charWidth));
            Write(ms, LF);

            if (!string.IsNullOrEmpty(satis.MasaAdi))
            {
                WriteText(ms, FormatTwoColumn("Masa:", satis.MasaAdi, charWidth));
                Write(ms, LF);
            }

            WriteText(ms, new string('-', charWidth));
            Write(ms, LF);

            // ── COLUMN HEADERS ──
            Write(ms, ESC_BOLD_ON);
            WriteText(ms, FormatItemHeader("Ürün", "Miktar", "Tutar", charWidth));
            Write(ms, ESC_BOLD_OFF);
            Write(ms, LF);
            WriteText(ms, new string('-', charWidth));
            Write(ms, LF);

            // ── ITEMS ──
            foreach (var item in detaylar)
            {
                var urunAdi = item.UrunAdi ?? "Ürün";

                // Product name on first line (may be long)
                WriteText(ms, urunAdi);
                Write(ms, LF);

                // Quantity × Unit Price = Total on second line (right-aligned details)
                var detail = $"  {item.Miktar} x {item.BirimFiyat:N2} = {item.ToplamFiyat:N2}";
                Write(ms, ESC_RIGHT);
                WriteText(ms, detail);
                Write(ms, ESC_LEFT);
                Write(ms, LF);
            }

            WriteText(ms, new string('-', charWidth));
            Write(ms, LF);

            // ── TOTAL ──
            Write(ms, ESC_BOLD_ON);
            Write(ms, ESC_DOUBLE_W);
            WriteText(ms, FormatTwoColumn("TOPLAM:", $"{satis.ToplamTutar:N2} TL", charWidth));
            Write(ms, ESC_NORMAL);
            Write(ms, ESC_BOLD_OFF);
            Write(ms, LF);

            WriteText(ms, new string('-', charWidth));
            Write(ms, LF);

            // ── PAYMENT TYPE ──
            WriteText(ms, FormatTwoColumn("Ödeme:", satis.OdemeTipi, charWidth));
            Write(ms, LF);
            Write(ms, LF);

            // ── FOOTER ──
            Write(ms, ESC_CENTER);
            if (!string.IsNullOrEmpty(altMesaj))
            {
                WriteText(ms, altMesaj);
                Write(ms, LF);
            }
            Write(ms, LF);
            WriteText(ms, $"Tarih: {tarih}");
            Write(ms, LF);

            // Feed and cut
            Write(ms, ESC_FEED);
            Write(ms, ESC_CUT);

            return ms.ToArray();
        }

        #region Helper Methods

        private static string FormatTwoColumn(string left, string right, int width)
        {
            int leftLen = GetDisplayLength(left);
            int rightLen = GetDisplayLength(right);
            int spaces = Math.Max(1, width - leftLen - rightLen);
            return left + new string(' ', spaces) + right;
        }

        private static string FormatItemHeader(string col1, string col2, string col3, int width)
        {
            // col2 and col3 are right-portion
            int col2Width = 6;
            int col3Width = 10;
            int col1Width = width - col2Width - col3Width;

            return col1.PadRight(col1Width) + col2.PadLeft(col2Width) + col3.PadLeft(col3Width);
        }

        private static int GetDisplayLength(string text)
        {
            // Turkish characters are single-width in most thermal printers
            return text.Length;
        }

        private static void Write(MemoryStream ms, byte[] data)
        {
            ms.Write(data, 0, data.Length);
        }

        private static void WriteText(MemoryStream ms, string text)
        {
            // Use Windows-1254 (Turkish) encoding for proper character support
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var bytes = Encoding.GetEncoding(857).GetBytes(text); // CP857 = DOS Turkish, widely supported by ESC/POS
            ms.Write(bytes, 0, bytes.Length);
        }

        private static string? GetDefaultPrinter()
        {
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                var settings = new PrinterSettings { PrinterName = printer };
                if (settings.IsDefaultPrinter)
                    return printer;
            }
            return PrinterSettings.InstalledPrinters.Count > 0
                ? PrinterSettings.InstalledPrinters[0]
                : null;
        }

        public static List<string> GetInstalledPrinters()
        {
            var printers = new List<string>();
            foreach (string printer in PrinterSettings.InstalledPrinters)
                printers.Add(printer);
            return printers;
        }

        #endregion

        #region RAW Printer Helper (Windows API)

        private static class RawPrinterHelper
        {
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            private struct DOCINFOW
            {
                [MarshalAs(UnmanagedType.LPWStr)] public string pDocName;
                [MarshalAs(UnmanagedType.LPWStr)] public string? pOutputFile;
                [MarshalAs(UnmanagedType.LPWStr)] public string? pDataType;
            }

            [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
            private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

            [DllImport("winspool.drv", SetLastError = true)]
            private static extern bool ClosePrinter(IntPtr hPrinter);

            [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
            private static extern bool StartDocPrinter(IntPtr hPrinter, int level, ref DOCINFOW pDocInfo);

            [DllImport("winspool.drv", SetLastError = true)]
            private static extern bool EndDocPrinter(IntPtr hPrinter);

            [DllImport("winspool.drv", SetLastError = true)]
            private static extern bool StartPagePrinter(IntPtr hPrinter);

            [DllImport("winspool.drv", SetLastError = true)]
            private static extern bool EndPagePrinter(IntPtr hPrinter);

            [DllImport("winspool.drv", SetLastError = true)]
            private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

            public static void SendBytesToPrinter(string printerName, byte[] data)
            {
                IntPtr hPrinter = IntPtr.Zero;
                var di = new DOCINFOW
                {
                    pDocName = "POS Fiş",
                    pDataType = "RAW"
                };

                if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
                    throw new Exception($"Yazıcı açılamadı: {printerName}\nHata kodu: {Marshal.GetLastWin32Error()}");

                try
                {
                    if (!StartDocPrinter(hPrinter, 1, ref di))
                        throw new Exception("Yazıcıya belge gönderilemedi.");

                    try
                    {
                        if (!StartPagePrinter(hPrinter))
                            throw new Exception("Yazıcı sayfası başlatılamadı.");

                        try
                        {
                            IntPtr pBytes = Marshal.AllocCoTaskMem(data.Length);
                            try
                            {
                                Marshal.Copy(data, 0, pBytes, data.Length);
                                if (!WritePrinter(hPrinter, pBytes, data.Length, out _))
                                    throw new Exception("Yazıcıya veri gönderilemedi.");
                            }
                            finally
                            {
                                Marshal.FreeCoTaskMem(pBytes);
                            }
                        }
                        finally
                        {
                            EndPagePrinter(hPrinter);
                        }
                    }
                    finally
                    {
                        EndDocPrinter(hPrinter);
                    }
                }
                finally
                {
                    ClosePrinter(hPrinter);
                }
            }
        }

        #endregion
    }
}
