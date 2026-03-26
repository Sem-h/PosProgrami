using System.Net;
using System.Net.Mail;
using System.Text;
using PosProjesi.DataAccess;
using PosProjesi.Models;

namespace PosProjesi.Services
{
    public class EmailRaporService
    {
        private readonly AyarlarRepository _ayarRepo = new();
        private readonly SatisRepository _satisRepo = new();

        /// <summary>
        /// Günlük satış raporu oluşturur ve e-posta ile gönderir
        /// </summary>
        public void GunlukRaporGonder(DateTime? tarih = null)
        {
            var raporTarih = tarih ?? DateTime.Today;
            var smtp = GetSmtpSettings();

            if (string.IsNullOrEmpty(smtp.Sunucu) || string.IsNullOrEmpty(smtp.Alici))
                throw new Exception("E-posta ayarları eksik! Lütfen Yönetim Paneli → E-posta Ayarları'ndan yapılandırın.");

            var html = RaporHtmlOlustur(raporTarih);
            var subject = $"Günlük Satış Raporu — {raporTarih:dd.MM.yyyy}";

            using var client = new SmtpClient(smtp.Sunucu, smtp.Port)
            {
                Credentials = new NetworkCredential(smtp.Kullanici, smtp.Sifre),
                EnableSsl = smtp.Ssl,
                Timeout = 15000
            };

            var gonderen = string.IsNullOrEmpty(smtp.Gonderici) ? smtp.Kullanici : smtp.Gonderici;
            var mail = new MailMessage(gonderen, smtp.Alici, subject, html)
            {
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };

            client.Send(mail);
        }

        /// <summary>
        /// Test e-postası gönderir
        /// </summary>
        public void TestMailGonder()
        {
            var smtp = GetSmtpSettings();

            if (string.IsNullOrEmpty(smtp.Sunucu) || string.IsNullOrEmpty(smtp.Alici))
                throw new Exception("E-posta ayarları eksik!");

            using var client = new SmtpClient(smtp.Sunucu, smtp.Port)
            {
                Credentials = new NetworkCredential(smtp.Kullanici, smtp.Sifre),
                EnableSsl = smtp.Ssl,
                Timeout = 15000
            };

            var gonderen = string.IsNullOrEmpty(smtp.Gonderici) ? smtp.Kullanici : smtp.Gonderici;
            var isletmeAdi = _ayarRepo.GetAyar(AyarlarRepository.IsletmeAdi, "Verimek POS");

            var mail = new MailMessage(gonderen, smtp.Alici,
                $"{isletmeAdi} — Test E-postası",
                $"<h2>✅ E-posta bağlantısı başarılı!</h2><p>Bu bir test mesajıdır.</p><p><small>{isletmeAdi} POS Sistemi — {DateTime.Now:dd.MM.yyyy HH:mm}</small></p>")
            {
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8
            };

            client.Send(mail);
        }

        private string RaporHtmlOlustur(DateTime tarih)
        {
            var isletmeAdi = _ayarRepo.GetAyar(AyarlarRepository.IsletmeAdi, "Verimek POS");
            var satislar = _satisRepo.GetSatislar(tarih, tarih);
            var gunlukToplam = _satisRepo.GetGunlukToplam(tarih);
            var enCokSatanlar = _satisRepo.GetEnCokSatanUrunler(10, tarih, tarih);

            // Ödeme tiplerine göre gruplama
            var nakitToplam = satislar.Where(s => s.OdemeTipi == "Nakit").Sum(s => s.ToplamTutar);
            var kartToplam = satislar.Where(s => s.OdemeTipi == "Kredi Kartı").Sum(s => s.ToplamTutar);
            var misafirToplam = satislar.Where(s => s.OdemeTipi == "Misafir").Sum(s => s.ToplamTutar);

            var sb = new StringBuilder();

            sb.Append($@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: Segoe UI, Arial, sans-serif; background: #f5f5f5; margin: 0; padding: 20px;'>
<div style='max-width: 600px; margin: 0 auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
  
  <!-- Header -->
  <div style='background: linear-gradient(135deg, #1a1a2e, #16213e); color: white; padding: 24px 30px;'>
    <h1 style='margin: 0; font-size: 22px;'>{isletmeAdi}</h1>
    <p style='margin: 5px 0 0; opacity: 0.7; font-size: 14px;'>Günlük Satış Raporu — {tarih:dd MMMM yyyy, dddd}</p>
  </div>

  <!-- Summary -->
  <div style='padding: 24px 30px;'>
    <div style='background: #f0fdf4; border-left: 4px solid #22c55e; padding: 16px 20px; border-radius: 0 8px 8px 0; margin-bottom: 20px;'>
      <div style='font-size: 12px; color: #666; text-transform: uppercase;'>Günlük Toplam</div>
      <div style='font-size: 32px; font-weight: bold; color: #16a34a;'>₺{gunlukToplam:N2}</div>
      <div style='font-size: 13px; color: #888; margin-top: 4px;'>{satislar.Count} satış işlemi</div>
    </div>

    <!-- Payment Breakdown -->
    <table style='width: 100%; border-collapse: collapse; margin-bottom: 20px;'>
      <tr>
        <td style='padding: 10px 16px; background: #f8fafc; border-radius: 6px;'>
          <div style='font-size: 12px; color: #888;'>💵 Nakit</div>
          <div style='font-size: 18px; font-weight: bold; color: #333;'>₺{nakitToplam:N2}</div>
        </td>
        <td style='width: 10px;'></td>
        <td style='padding: 10px 16px; background: #f8fafc; border-radius: 6px;'>
          <div style='font-size: 12px; color: #888;'>💳 Kart</div>
          <div style='font-size: 18px; font-weight: bold; color: #333;'>₺{kartToplam:N2}</div>
        </td>
        <td style='width: 10px;'></td>
        <td style='padding: 10px 16px; background: #f8fafc; border-radius: 6px;'>
          <div style='font-size: 12px; color: #888;'>🎁 Misafir</div>
          <div style='font-size: 18px; font-weight: bold; color: #333;'>₺{misafirToplam:N2}</div>
        </td>
      </tr>
    </table>");

            // En çok satan ürünler
            if (enCokSatanlar.Count > 0)
            {
                sb.Append(@"
    <h3 style='margin: 20px 0 10px; color: #333; font-size: 15px;'>🏆 En Çok Satan Ürünler</h3>
    <table style='width: 100%; border-collapse: collapse;'>
      <tr style='background: #1a1a2e; color: white;'>
        <th style='padding: 8px 12px; text-align: left; font-size: 12px;'>Ürün</th>
        <th style='padding: 8px 12px; text-align: center; font-size: 12px;'>Adet</th>
        <th style='padding: 8px 12px; text-align: right; font-size: 12px;'>Tutar</th>
      </tr>");

                int i = 0;
                foreach (var urun in enCokSatanlar)
                {
                    var bgColor = i % 2 == 0 ? "#f9fafb" : "#ffffff";
                    sb.Append($@"
      <tr style='background: {bgColor};'>
        <td style='padding: 8px 12px; font-size: 13px;'>{urun.UrunAdi}</td>
        <td style='padding: 8px 12px; text-align: center; font-size: 13px;'>{urun.ToplamMiktar}</td>
        <td style='padding: 8px 12px; text-align: right; font-size: 13px; font-weight: bold;'>₺{urun.ToplamTutar:N2}</td>
      </tr>");
                    i++;
                }
                sb.Append("</table>");
            }

            // Satış detayları
            if (satislar.Count > 0)
            {
                sb.Append(@"
    <h3 style='margin: 24px 0 10px; color: #333; font-size: 15px;'>📋 Satış İşlemleri</h3>
    <table style='width: 100%; border-collapse: collapse;'>
      <tr style='background: #1a1a2e; color: white;'>
        <th style='padding: 8px 12px; text-align: left; font-size: 12px;'>Fiş No</th>
        <th style='padding: 8px 12px; text-align: left; font-size: 12px;'>Saat</th>
        <th style='padding: 8px 12px; text-align: left; font-size: 12px;'>Ödeme</th>
        <th style='padding: 8px 12px; text-align: left; font-size: 12px;'>Kasiyer</th>
        <th style='padding: 8px 12px; text-align: right; font-size: 12px;'>Tutar</th>
      </tr>");

                int i = 0;
                foreach (var s in satislar)
                {
                    var bgColor = i % 2 == 0 ? "#f9fafb" : "#ffffff";
                    var saat = DateTime.TryParse(s.SatisTarihi, out var dt) ? dt.ToString("HH:mm") : "-";
                    sb.Append($@"
      <tr style='background: {bgColor};'>
        <td style='padding: 6px 12px; font-size: 12px;'>#{s.Id}</td>
        <td style='padding: 6px 12px; font-size: 12px;'>{saat}</td>
        <td style='padding: 6px 12px; font-size: 12px;'>{s.OdemeTipi}</td>
        <td style='padding: 6px 12px; font-size: 12px;'>{s.KasiyerAdi}</td>
        <td style='padding: 6px 12px; text-align: right; font-size: 12px; font-weight: bold;'>₺{s.ToplamTutar:N2}</td>
      </tr>");
                    i++;
                }
                sb.Append("</table>");
            }

            sb.Append($@"
  </div>

  <!-- Footer -->
  <div style='background: #f8fafc; padding: 16px 30px; text-align: center; border-top: 1px solid #e5e7eb;'>
    <p style='margin: 0; font-size: 12px; color: #999;'>{isletmeAdi} POS Sistemi — Oluşturulma: {DateTime.Now:dd.MM.yyyy HH:mm}</p>
  </div>
</div>
</body>
</html>");

            return sb.ToString();
        }

        private SmtpSettings GetSmtpSettings()
        {
            return new SmtpSettings
            {
                Sunucu = _ayarRepo.GetAyar(AyarlarRepository.SmtpSunucu),
                Port = int.TryParse(_ayarRepo.GetAyar(AyarlarRepository.SmtpPort, "587"), out var p) ? p : 587,
                Kullanici = _ayarRepo.GetAyar(AyarlarRepository.SmtpKullanici),
                Sifre = _ayarRepo.GetAyar(AyarlarRepository.SmtpSifre),
                Ssl = _ayarRepo.GetAyar(AyarlarRepository.SmtpSsl, "1") == "1",
                Alici = _ayarRepo.GetAyar(AyarlarRepository.MailAlici),
                Gonderici = _ayarRepo.GetAyar(AyarlarRepository.MailGonderici)
            };
        }

        private class SmtpSettings
        {
            public string Sunucu { get; set; } = "";
            public int Port { get; set; } = 587;
            public string Kullanici { get; set; } = "";
            public string Sifre { get; set; } = "";
            public bool Ssl { get; set; } = true;
            public string Alici { get; set; } = "";
            public string Gonderici { get; set; } = "";
        }
    }
}
