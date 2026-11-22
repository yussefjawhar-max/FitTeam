# FitTeam

Görsel Programlama dersi için C# masaüstü spor salonu, üye gelişimi ve antrenman takip projesi.
.NET 8, Avalonia ve MySQL 8.4 kullanır. Bu uygulama Windows Forms kullanmaz.

## Windows üzerinde Visual Studio ile çalıştırma

1. .NET 8 SDK destekleyen Visual Studio 2022 ve .NET masaüstü geliştirme bileşenini kurun.
2. Bu depoyu indirin ve ZIP ise tamamını bir klasöre çıkarın.
3. Paylaşılmış FitTeam Windows uygulama paketini de tamamen çıkarın. İçindeki **mysql** klasörünü **FitTeam.sln ile aynı klasöre** kopyalayın. mysql/bin/mysqld.exe dosyası bulunmalıdır. Motor büyük olduğu için kaynak depoya dahil edilmemiştir; MySQL lisans dosyalarını motorla birlikte koruyun.
4. FitTeam.sln dosyasını Visual Studio ile açıp NuGet paketlerinin yüklenmesini bekleyin.
5. FitTeam.Desktop projesini başlangıç projesi seçin ve F5'e veya Çalıştır'a basın.
6. İlk açılışta kullanıcıya özel yerel MySQL hazırlanır. İlk yetkili hesabını uygulamanın Yetkili girişi bölümünden kurun; üyeler Yeni üye kaydı bölümünü kullanabilir.

Directory.Build.targets Windows'ta proje kökündeki mysql klasörünü derleme çıktısına bağlar. Projeyi veya motoru taşırsanız bin ve obj klasörlerini temizleyip yeniden derleyin.
Alternatif olarak FITTEAM_MYSQL_ENGINE değişkenini kendi MySQL motor klasörünüze tanımlayıp Visual Studio'yu yeniden açabilirsiniz. Mevcut MySQL sunucusu için FITTEAM_MYSQL_CONFIG bağlantı dosyası kullanılabilir; MySqlSettings.cs alanlarını inceleyin. Bağlantı parolanızı depoya yüklemeyin.

Hazır Windows paketini çalıştırmak için kod derlemek gerekmez: paketi çıkarıp BASLAT.cmd dosyasını açın. Kaynak depoda Windows uygulama paketi bulunmaz.

## Proje bölümleri

| Proje | Görevi |
|---|---|
| FitTeam.Desktop | Avalonia ekranları, Views ve kullanıcı etkileşimleri |
| FitTeam.Core | Modeller ve iş kuralları |
| FitTeam.Controllers | Kayıt, doğrulama ve yetki işlemleri |
| FitTeam.Persistence | MySQL, SMTP ve korumalı yerel ayarlar |
| FitTeam.AuthServer | Yerel giriş hizmeti |
| FitTeam.Checks | İş kuralları ve yetki kontrolleri |
| FitTeam.IntegrationChecks | MySQL ve e-posta bütünleştirme kontrolleri |

## Veritabanı

SQL/FitTeam-Tam-Kurulum.sql 30 tabloyu ve ilk durum kaydını içerir. MySQL 8.4 üzerinde temiz kurulum içindir. Uygulama kendi yerel kurulumunda şemayı otomatik hazırlar. SQL dosyası gerçek üye verilerini, SMTP anahtarını veya kişisel parolaları içermez.

## Giriş ve e-posta

Normal giriş kullanıcı adı ve FitTeam parolasıyla yapılır. Kayıtta e-posta girilirse kod doğrulaması gerekir. Gerçek e-posta için yetkili E-posta ayarları ekranından SMTP hizmetini yapılandırmalıdır. Google ile giriş ve SMS kullanılmaz. Yeni kurulumda önceden ayarlanmış kişisel SMTP hesabı bulunmaz.

## Kayıtların yeri

Her bilgisayar kendi yerel verilerini saklar; bilgisayarlar arasında otomatik eşitleme yoktur. Windows'ta .NET LocalApplicationData/FitTeam klasörü kullanılır. Uygulamanın Database klasörü ve korumalı ayar dosyaları kaynak depoya dahil edilmez.

## Doğrulama durumu

Mac üzerinde kaynak derlemesi, 506 uygulama kontrolü ve 25 bütünleştirme kontrolü başarılıdır. Tam SQL dosyası boş test veritabanına iki kez başarıyla yüklenmiştir. Gerçek SMTP sunucusu test mesajını kabul etmiştir. Windows x64 dağıtımı çapraz derlenmiştir; gerçek Windows bilgisayarda açılış henüz doğrulanmamıştır. Kayıt akışının tamamı ve alıcı gelen kutusu kanıtı için son kullanıcı kontrolü gereklidir.

## Komut satırı

    dotnet restore FitTeam.sln
    dotnet build FitTeam.sln
    dotnet run --project FitTeam.Desktop
    dotnet run --project FitTeam.Checks

Bütünleştirme kontrollerini yalnız ayrı test veritabanında FITTEAM_MYSQL_CONFIG ile çalıştırın.
