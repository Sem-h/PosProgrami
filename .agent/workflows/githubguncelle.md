---
description: GitHub'a versiyon güncellemesi yaparak push etme (version bump + release build + push)
---
// turbo-all

Bu workflow projeyi yeni versiyon ile derleyip GitHub'a push eder. Kullanıcılara otomatik güncelleme gider.

1. **version.json** dosyasındaki `version` değerini bir artır (patch: x.y.Z+1) ve `notes` kısmını güncelle.

2. **UpdateService.cs** dosyasındaki `CurrentVersion` sabitini aynı yeni versiyona güncelle:
   - Dosya: `c:\Users\Administrator\Desktop\Projes\PosProjesi\PosProjesi\Services\UpdateService.cs`
   - Satır: `public const string CurrentVersion = "X.Y.Z";`

3. Release build al:
```
dotnet build -c Release
```
Working directory: `c:\Users\Administrator\Desktop\Projes\PosProjesi\PosProjesi`

4. Release dosyalarını kopyala:
```
powershell -ExecutionPolicy Bypass -File .\copy_release.ps1
```
Working directory: `c:\Users\Administrator\Desktop\Projes\PosProjesi`

5. Git commit ve push:
```
git add -A; git commit -m "vX.Y.Z: Değişiklik açıklaması"; git push
```
Working directory: `c:\Users\Administrator\Desktop\Projes\PosProjesi`

> **Not:** Commit mesajındaki `vX.Y.Z` ve açıklamayı güncel versiyona göre düzenle.
