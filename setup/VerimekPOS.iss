; Verimek POS Sistemi - Inno Setup Script
; Ozellestirilmis kurulum sihirbazi

#define MyAppName "Verimek POS Sistemi"
#define MyAppVersion "1.1.3"
#define MyAppPublisher "Verimek Telekomünikasyon"
#define MyAppURL "https://github.com/Sem-h/PosProgrami"
#define MyAppExeName "PosProjesi.exe"

[Setup]
AppId={{B3F2A1D0-5E4C-4A8F-9B2D-1C3E5F7A9B0D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} v{#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={localappdata}\VerimekPOS
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
; Cikti ayarlari
OutputDir=..\installer
OutputBaseFilename=VerimekPOS_Setup_v{#MyAppVersion}
; Sikistirma
Compression=lzma2/ultra64
SolidCompression=yes
; Gorunum
WizardStyle=modern
WizardSizePercent=110
WizardImageFile=wizard_sidebar.bmp
WizardSmallImageFile=wizard_small.bmp
; Ek ozellikler
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
; Minimum Windows surumu
MinVersion=10.0
SetupIconFile=app.ico
; Uninstall ayarlari
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[CustomMessages]
turkish.WelcomeLabel1=Verimek POS Sistemi Kurulumu
turkish.WelcomeLabel2=Bu sihirbaz, Verimek POS Sistemi v{#MyAppVersion} uygulamasini bilgisayariniza kuracaktir.%n%nDevam etmeden once tum diger uygulamalari kapatmaniz onerilir.
turkish.FinishedHeadingLabel=Verimek POS Sistemi Kuruldu!
turkish.FinishedLabel=Verimek POS Sistemi basariyla bilgisayariniza yuklendi.%n%nUygulama, masaustu kisayolundan veya Baslat menusunden baslatilabilir.

[Tasks]
Name: "desktopicon"; Description: "Masaüstü kısayolu oluştur"; GroupDescription: "Ek görevler:"; Flags: checkedonce
Name: "startupicon"; Description: "Windows başlangıcında otomatik çalıştır"; GroupDescription: "Ek görevler:"; Flags: unchecked

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{#MyAppName} Kaldır"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Windows baslangicinda calistir (istege bagli)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "VerimekPOS"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Verimek POS Sistemi'ni şimdi başlat"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\database"
Type: filesandordirs; Name: "{app}\image"
Type: filesandordirs; Name: "{app}\_update"
Type: files; Name: "{app}\lastcheck.txt"
