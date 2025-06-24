# story
# 🎭 Shutter Island - İnteraktif Hikaye Uygulaması

Bu proje, Martin Scorsese'nin yönetmenliğini yaptığı *Shutter Island* filmine sadık kalınarak geliştirilmiş, kullanıcıya seçim hakkı tanıyan **WPF tabanlı bir interaktif hikaye oyunudur**. Uygulama, kullanıcı seçimlerine göre farklı sonlara ulaşan, dallanmalı bir yapıdadır.

## 📌 Özellikler

- 🎬 Filmle uyumlu sahne akışı (Toplam 50 sahne)
- 🧠 Seçim bazlı ilerleme (2 seçenekli dallanma yapısı)
- 🎵 Arka plan müziği kontrolü (Aç/Kapat)
- 🖼️ Sahneye özel arka plan görselleri
- ⌨️ Yazı animasyonu (harf harf yazım efekti)
- ⏩ SPACE tuşu ile yazıyı atlayarak anında görüntüleme
- 🧠 Kullanıcı ilerlemesi (son sahne ve müzik ayarları)

## 🛠 Kullanılan Teknolojiler

- **.NET (C#)** – Uygulama mantığı ve sahne yönetimi
- **WPF / XAML** – Arayüz tasarımı
- **MSSQL LocalDB** – Sahne ve kullanıcı verilerini saklamak için
- **MediaPlayer** – Müzik ve ses efektleri
- **DropShadowEffect** – Görsel efektler

## 📁 Proje Yapısı
story/

- Assets/ # Arka plan görselleri ve müzik dosyası
- MainWindow.xaml # Hikaye ekranı
- MainWindow.xaml.cs # Hikaye arayüzü kod kısmı
- basla.xaml #giriş ekranı
- basla.xaml.cs #giriş ekranı kodları
- dbo.Users # Kullanıcı ayarları (son sahne, müzik, şifre vb. ayarlar)
- dbo.Scene # Sahne veritabanı 
- README.md # Bu dosya

Admin:
- Kullanıcı Adı: esma
- Şifre:112233

- !!! Projeyi çalıştırmadan önce SQL Server’da şu şekilde sstory.dacpac dosyasını deploy edin. !!!
- story2.dacpac dosyası yedek dosyadır.

