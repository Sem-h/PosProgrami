<div align="center">

# 🖥️ Verimek POS Sistemi

### Modern Satış Noktası Uygulaması

[![Version](https://img.shields.io/badge/Sürüm-1.1.1-blue?style=for-the-badge)]()
[![.NET](https://img.shields.io/badge/.NET_9-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![SQLite](https://img.shields.io/badge/SQLite-003B57?style=for-the-badge&logo=sqlite&logoColor=white)](https://www.sqlite.org/)
[![Windows](https://img.shields.io/badge/Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white)](https://www.microsoft.com/)
[![License](https://img.shields.io/badge/Lisans-MIT-green?style=for-the-badge)](LICENSE)

**Küçük ve orta ölçekli işletmeler için geliştirilmiş, modern arayüzlü, tam donanımlı POS (Satış Noktası) masaüstü uygulaması.**

---

</div>

## ✨ Öne Çıkan Özellikler

<table>
<tr>
<td width="50%">

### 🛒 Satış Yönetimi
- Barkod okuyucu desteği ile hızlı ürün ekleme
- Kategorilere göre ürün filtreleme
- Dinamik sepet yönetimi
- Nakit ve kredi kartı ödeme seçenekleri
- Otomatik stok güncelleme

</td>
<td width="50%">

### 👥 Personel Yönetimi
- Personel bazlı giriş sistemi
- Her personel için güvenli şifre koruması
- Satışlarda personel takibi
- Personel ekleme, düzenleme ve silme (CRUD)

</td>
</tr>
<tr>
<td width="50%">

### 📊 Raporlama
- Günlük / tarihe göre satış raporları
- Toplam satış, sipariş ve ortalama tutarlar
- Personele göre satış filtreleme
- Satış detay görüntüleme

</td>
<td width="50%">

### ⚙️ Yönetim Paneli
- Ürün yönetimi (Ekle / Düzenle / Sil)
- Kategori yönetimi
- Personel yönetimi
- Veritabanı yedekleme
- CSV içe/dışa aktarma (Excel uyumlu)
- Otomatik güncelleme kontrolü

</td>
</tr>
</table>

---

## 🛠️ Teknoloji Altyapısı

| Teknoloji | Açıklama |
|-----------|----------|
| **.NET 9** | Windows Forms (WinForms) masaüstü uygulaması |
| **SQLite** | Hafif, sunucusuz yerel veritabanı |
| **Dapper** | Yüksek performanslı mikro ORM |
| **SVG.NET** | Vektörel logo desteği |
| **GDI+** | Özel çizimli premium dark tema arayüzü |

---

## 🚀 Kurulum

### Gereksinimler

- Windows 10 / 11
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Çalıştırma

```bash
# Repoyu klonlayın
git clone https://github.com/Sem-h/PosProgrami.git

# Proje dizinine geçin
cd PosProgrami

# Uygulamayı çalıştırın
dotnet run --project PosProjesi
```

---

## 🔐 Giriş Bilgileri

> [!IMPORTANT]
> Uygulamayı ilk kez çalıştırdığınızda aşağıdaki varsayılan bilgileri kullanın.

| Giriş Noktası | Kullanıcı | Şifre |
|:---:|:---:|:---:|
| **Personel Giriş Ekranı** | Admin Yönetici | `1234` |
| **Yönetim Paneli Erişimi** | — | `admin123` |

> [!WARNING]
> Güvenliğiniz için varsayılan şifreleri ilk girişten sonra değiştirmeniz önerilir.

---

## 🏗️ Proje Yapısı

```
PosProjesi/
├── 📁 Database/
│   └── DatabaseHelper.cs         # SQLite bağlantı ve tablo yönetimi
├── 📁 DataAccess/
│   ├── UrunRepository.cs         # Ürün CRUD işlemleri
│   ├── KategoriRepository.cs     # Kategori CRUD işlemleri
│   ├── SatisRepository.cs        # Satış kayıt ve raporlama
│   └── PersonelRepository.cs     # Personel yönetimi
├── 📁 Models/
│   ├── Urun.cs                   # Ürün modeli
│   ├── Kategori.cs               # Kategori modeli
│   ├── Satis.cs                  # Satış modeli
│   ├── SatisDetay.cs             # Satış detay modeli
│   └── Personel.cs               # Personel modeli
├── 📁 Forms/
│   ├── SplashForm.cs             # Açılış animasyonu
│   ├── PersonelLoginForm.cs      # Personel giriş ekranı
│   ├── MainForm.cs               # Ana dashboard
│   ├── SatisForm.cs              # Satış ekranı
│   ├── RaporForm.cs              # Rapor ekranı
│   ├── AdminPanelForm.cs         # Yönetim paneli
│   ├── UrunYonetimForm.cs        # Ürün yönetimi
│   ├── KategoriYonetimForm.cs    # Kategori yönetimi
│   ├── PersonelYonetimForm.cs    # Personel yönetimi
│   ├── AdminLoginForm.cs         # Admin giriş formu
│   ├── MusteriEkranForm.cs       # Müşteri ekranı
│   └── HakkindaForm.cs           # Hakkında ekranı
├── 📁 Services/
│   └── UpdateService.cs          # Otomatik güncelleme servisi
├── 📁 UI/
│   └── Theme.cs                  # Merkezi tasarım sistemi
└── Program.cs                    # Uygulama giriş noktası
```

---

## 🎨 Tasarım

Uygulama baştan sona **özel GDI+ çizimleriyle** tasarlanmış modern bir **dark tema** kullanır:

- 🌑 Profesyonel koyu arka plan
- 💠 Gradient aksan çizgileri ve ambient ışık efektleri
- ✨ Hover animasyonları ve glassmorphism kartlar
- 🎯 Personel başına benzersiz renk paleti
- ⏰ Gerçek zamanlı saat gösterimi
- 📐 Responsive kart düzeni

---

## 🔄 Otomatik Güncelleme

Uygulama, GitHub üzerinden otomatik güncelleme kontrolü yapar:

1. Başlangıçta ve her 5 dakikada `version.json` kontrol edilir
2. Yeni sürüm tespit edildiğinde kullanıcıya bildirim gösterilir
3. Güncelleme dosyaları `release/` klasöründen indirilir
4. Otomatik yükleme scripti ile uygulama güncellenir

---

## 📋 Sürüm Geçmişi

| Sürüm | Tarih | Değişiklikler |
|-------|-------|---------------|
| **1.1.0** | 2026-02-21 | Personel yönetim sistemi, personel giriş ekranı, satışlara personel kaydı, satış ekranı tile dinamik genişlik, tam bağımlılık güncelleme |
| **1.0.7** | 2026-02-20 | Satış ekranı iyileştirmeleri, hata düzeltmeleri |
| **1.0.0** | 2026-02-15 | İlk sürüm — temel POS işlevleri |

---

## 📄 Lisans

Bu proje **MIT Lisansı** altında lisanslanmıştır.

---

<div align="center">

**Verimek Telekomünikasyon** tarafından geliştirilmiştir.

⭐ Bu projeyi beğendiyseniz yıldız vermeyi unutmayın!

</div>
