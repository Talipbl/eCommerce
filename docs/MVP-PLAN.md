# ECommerce — MVP Geliştirme Planı (Revizyon 2)

> Güncelleme: 2026-11-06 · Durum: Onay bekliyor (açık kararlar en altta)

## MVP tanımı

**Tek satıcılı** bir e-ticaret back-end'i:

1. **Yerel e-ticaret sitesi** için ürün, müşteri ve sipariş yönetimi.
2. **Pazaryeri modülü** (MVP'de yalnızca Trendyol): ürünlerin Trendyol'a gönderilmesi, fiyat/stok senkronu, Trendyol siparişlerinin işlenmesi.
3. İkisinin entegrasyonu: tek ürün kataloğu, tek stok, tüm kanalların siparişleri tek yerde.

**Mimari kararlar**
- Back-end ve front-end ayrı projeler. Back-end bir **ASP.NET Core Web API**; front-end (React veya Vue) **en son** yapılır.
- SaaS değil. Tek satıcı olduğu için kiracı (tenant) modeli yok; Trendyol bilgileri konfigürasyonda/secret'ta tutulur.
- Back-end operasyonları **TDD** ile geliştirilir (bkz. "TDD yaklaşımı").
- Yayın **Docker** ile yapılır.

**Kapsam dışı (MVP sonrası):** N11, Hepsiburada, kupon/indirim, XML scheduler, soru-cevap, iade/claim, Redis, çoklu dil.

---

## Hedef çözüm yapısı

```
src/
  ECommerce.API              ← YENİ: Web API (controller'lar, auth, OpenAPI)
  ECommerce.Application      ← servisler, use-case'ler, validasyon
  ECommerce.Domain           ← entity'ler, domain kuralları
  ECommerce.Persistence      ← EF Core (ECommerce.EntityFramework yeniden adlandırılır)
  ECommerce.Core             ← ortak altyapı (Result, extension'lar)
marketplaces/
  Trendyol.Client            ← Trendyol HTTP client'ı
tests/
  ECommerce.UnitTests
  ECommerce.IntegrationTests ← WebApplicationFactory + Testcontainers
  Trendyol.Client.Tests      ← WireMock ile sözleşme testleri
frontend/                    ← Faz 6
docker/                      ← Faz 5: Caddyfile, yedekleme betikleri
```

- `ECommerce.CustomerPortal` (MVC) Faz 3'te silindi (git geçmişinde duruyor).
- `N11.Client` ve `Hepsiburada.Client` Application'dan bağlantısı kesilip solution'dan çıkarılır; MVP sonrası geri alınır.
- `ECommerce.ImageAPI` ayrı servis olarak kalmaz; görsel yükleme API'nin bir modülü olur (bkz. Açık kararlar #3).

---

## Trendyol dokümantasyonu ile mevcut client arasındaki farklar

[Trendyol geliştirici dokümanı](https://developers.trendyol.com/docs/getting-started) incelendiğinde mevcut `Trendyol.Client`'ın büyük kısmı güncel değil:

| Konu | Mevcut kod | Güncel doküman |
|---|---|---|
| Base URL | `https://api.trendyol.com` + `/sapigw/...` | Prod `https://apigw.trendyol.com`, Stage `https://stageapigw.trendyol.com` |
| Ürün yaratma | `/sapigw/suppliers/{id}/v2/products` | `/integration/product/sellers/{sellerId}/v2/products` |
| Sipariş paketleri | `/sapigw/suppliers/{id}/orders` (v1) | `/integration/order/sellers/{sellerId}/v2/orders` — **v1 15.10.2026'da kapatıldı**, mevcut sipariş listeleme büyük ihtimalle çalışmıyor |
| Rate limit | Ele alınmıyor | 14.09.2026'dan itibaren servis **grubu** bazlı dakikalık limit; 429 cevabı |
| Stage ortamı | Yok | Çıkış IP'si Trendyol'a whitelist ettirilmeli (aksi halde 503) |
| User-Agent | `{SellerId} - SelfIntegration` ✔ | Aynı; eksikse 403 |
| `TrendyolDiscoveryClient` | Trendyol web sitesini tarıyor | Resmi API değil — kaldırılacak |

Bu yüzden Faz 3'ün ilk işi client'ı güncel uçlara taşımaktı. **Durum (Faz 3):** tablodaki tüm farklar giderildi; client v2 uçlarına göre yeniden yazıldı.

---

## TDD yaklaşımı

- **Kural:** Faz 2'den itibaren her yeni davranış önce başarısız bir testle başlar (Red → Green → Refactor). Faz 1'deki refaktörlerde önce mevcut davranışı sabitleyen *karakterizasyon testi* yazılır.
- **Test piramidi**
  - *Unit:* domain kuralları, Application servisleri (bağımlılıklar mock'lanır).
  - *Sözleşme:* Trendyol client'ı ↔ WireMock.Net; istek/yanıt örnekleri Trendyol dokümanındaki örneklerden alınır.
  - *Entegrasyon:* `WebApplicationFactory` + Testcontainers (MSSQL) ile API uçları gerçek veritabanına karşı.
  - *Canlı (manuel/opsiyonel):* Trendyol stage ortamına karşı, `[Trait("Category","Live")]`, CI'da çalışmaz.
- **Araçlar:** xUnit, NSubstitute, Shouldly, WireMock.Net, Testcontainers.MsSql, coverlet.
- **Bitti tanımı (her iş için):** testler yeşil, CI yeşil, OpenAPI dokümanı güncel, gizli bilgi yok.

---

## Faz 0 — Acil: Güvenlik, derleme, platform (1 gün)

- [ ] **Anahtarları iptal et ve yenile** *(satıcı tarafından yapılacak)*: Trendyol (git geçmişinde açık), N11, SQL Server (`talipbulundu.com`) şifresi. Hepsi git geçmişinde kalıcı olarak duruyor.
- [x] Bozuk maskeleme düzeltildi; Trendyol bilgileri `IOptions<TrendyolOptions>` ile `Trendyol` konfigürasyon bölümünden okunuyor. Eksikse uygulama açılışta anlaşılır bir hatayla duruyor (`ValidateOnStart`).
- [x] Repodaki düz metin gizli bilgiler kaldırıldı (`queries.txt`, N11 testi → ortam değişkeni + `TestCategory("Live")`); `.gitignore`'a `.env`, `*.local.json`, `secrets.json` eklendi.
- [x] **.NET 10'a geçildi:** tüm projeler `net10.0`, EF Core / Extensions / RuntimeCompilation 10.0.14, `Microsoft.Data.SqlClient` 6.1.8, `global.json` (SDK 10.0.403, `latestFeature`).
- [x] Geçişli `System.Security.Cryptography.Pkcs` güvenlik açıkları sabitlenerek kapatıldı. Kalanlar Faz 1'de: AutoMapper (→ Mapster), Swashbuckle 6.2.3 (ImageAPI birleşirken).
- [x] GitHub Actions: `.github/workflows/ci.yml` (restore, build, `TestCategory!=Live` testleri). Solution'daki büyük/küçük harf yol hatası Linux için düzeltildi.

**Kabul:** `dotnet build` hatasız ✔, repoda gizli bilgi yok ✔, hedef framework `net10.0` ✔, CI yeşil ⏳ (ilk push'ta doğrulanacak).

**Yerel çalıştırma:**
```bash
cd src/ECommerce.CustomerPortal
dotnet user-secrets set "Trendyol:SellerId" "<seller-id>"
dotnet user-secrets set "Trendyol:ApiKey" "<api-key>"
dotnet user-secrets set "Trendyol:ApiSecret" "<api-secret>"
```

---

## Faz 1 — Proje temeli ve altyapı (6–8 gün)

### 1.1 API projesi
- [x] `ECommerce.API`: controller tabanlı Web API, OpenAPI + Scalar (`/scalar`, sadece Development), versiyonlama (`/api/v1`, versiyon başına OpenAPI dokümanı).
- [x] ProblemDetails + `GlobalExceptionHandler` (stack trace dışarı sızmıyor), bilinmeyen route'lar için `application/problem+json`.
- [x] Komposizyon kökü: `AddApplication()`, `AddPersistence()`, `AddTrendyol()`. Autofac + Castle kaldırıldı, yerleşik DI kullanılıyor.

### 1.2 Veri erişimi
- [x] Bağlantı dizesi `ConnectionStrings:ECommerce`; `AddDbContext<ECommerceContext>`, eksikse açılışta hata.
- [x] Eski `EfBaseEntityRepository` + 21 servis-özel repository kaldırıldı; yerine generic async `IRepository<T>` / `EfRepository<T>` ve `IUnitOfWork`. Özel repository'ler Faz 4'te ihtiyaç oldukça TDD ile eklenecek.
- [x] Code-first: `InitialCreate` migration'ı (30 tablo), `dotnet-ef` yerel tool olarak (`dotnet-tools.json`), `DesignTimeContextFactory` (`ECOMMERCE_CONNECTION`).
- [x] 29 entity konfigürasyonu `IEntityTypeConfiguration<T>` sınıflarına ayrıldı.
- [x] `AuditSaveChangesInterceptor`: `CreatedDate`/`UpdatedDate` (TimeProvider ile, UTC) ve soft-delete (`DeletedDate` + global query filter).
- [x] Bulunan hata: 9 entity `CreatedDate`/`UpdatedDate` alanlarını yeniden tanımlayıp base sınıfı gizliyordu; audit değerleri bu tablolara yazılmayacaktı. Gizleyen alanlar kaldırıldı.

### 1.3 Cross-cutting
- [x] Loglama: Serilog (konsol, istek logları); `DbLogManager` ve `LogAspect` kaldırıldı.
- [x] Cache: Trendyol marka/kategori listeleri `IMemoryCache` ile (5 dk); `CacheAspect` kaldırıldı.
- [ ] Validasyon: FluentValidation endpoint filtresi → **Faz 2'ye taşındı** (ilk gerçek uçlar orada geliyor). `ValidationAspect` kaldırıldı.
- [x] `SecurityOperationAspect` kaldırıldı (Faz 2'de policy tabanlı yetkilendirme).
- [x] Trendyol client'ları typed `HttpClient` + `AddStandardResilienceHandler` (retry, timeout, circuit breaker); Basic auth ve User-Agent `TrendyolOptions`'tan.
- [x] Trendyol hata gövdesi artık JSON olarak ayrıştırılıyor (`TrendyolErrorParser`); önceden ham string DTO'ya map edilmeye çalışılıyordu.

### 1.4 Temizlik ve bağımlılıklar
- [x] Ölü `Compile Remove` blokları, `Model1.tt`, EF6, Autofac, Castle, `System.Data.SqlClient`, `Microsoft.AspNetCore.Http 2.2.2` kaldırıldı.
- [x] AutoMapper → **Mapster** (`IRegister` sınıfları). Bulunan hata: Trendyol tarih parametreleri saniye gönderiliyordu, doküman milisaniye istiyor; düzeltildi.
- [x] `TreatWarningsAsErrors` (`Directory.Build.props`); tam derleme 0 uyarı. Geçici istisna: eski Trendyol modelleri/DTO'ları ve portal ViewModel'lerinde CS8618 kapalı (`.editorconfig`) → Faz 3'te kaldırılacak.
- [x] Kullanılmayan yerel servisler, DTO'lar, validator'lar ve helper'lar silindi; Faz 2 (giriş) ve Faz 4'te TDD ile yeniden yazılacak.
- [x] Faz 3.1'den öne alındı: `TrendyolDiscoveryClient` (web scraping) ile kapsam dışı Claim/Question/Delivery client'ları silindi. `ECommerce.ImageAPI` kaldırıldı (görsel yükleme Faz 4.1).
- [x] Yapı: `ECommerce.EntityFramework` → `ECommerce.Persistence`, `clients/` → `marketplaces/`; N11/Hepsiburada solution dışında.

### 1.5 Test altyapısı ve geliştirme ortamı
- [x] xUnit v3 + Microsoft Testing Platform (`global.json`), Shouldly, NSubstitute; MSTest projeleri kaldırıldı.
- [x] `ECommerce.UnitTests`: Trendyol mapping karakterizasyon testleri, hata ayrıştırıcı, audit interceptor ve repository testleri (EF InMemory).
- [x] `ECommerce.IntegrationTests`: `WebApplicationFactory` + Testcontainers (SQL Server, migration uygulanmış) ile health, ProblemDetails ve exception handler testleri. Docker yoksa yerelde atlanır, CI'da hata verir.
- [x] `docker-compose.dev.yml` (SQL Server) + `.env.example`.
- [x] Health: `/health/live` (bağımlılıksız), `/health/ready` (veritabanı).

**Kabul:** API boş veritabanına migration ile kuruluyor ✔; hiçbir sınıf `new DbContext` / `new HttpClient` yapmıyor ✔; 28 test (23 unit + 5 entegrasyon) yerelde yeşil ✔; CI ⏳ (PR ile doğrulanacak).

**Yerel çalıştırma:**
```bash
cp .env.example .env   # MSSQL_SA_PASSWORD değerini doldurun
docker compose -f docker-compose.dev.yml up -d
dotnet tool restore
cd src/ECommerce.API
dotnet user-secrets set "ConnectionStrings:ECommerce" "Server=localhost,1433;Database=ECommerce;User Id=sa;Password=<şifre>;TrustServerCertificate=True"
dotnet user-secrets set "Trendyol:SellerId" "<seller-id>"
dotnet user-secrets set "Trendyol:ApiKey" "<api-key>"
dotnet user-secrets set "Trendyol:ApiSecret" "<api-secret>"
cd ../..
ECOMMERCE_CONNECTION="Server=localhost,1433;Database=ECommerce;User Id=sa;Password=<şifre>;TrustServerCertificate=True" dotnet ef database update --project src/ECommerce.Persistence
dotnet run --project src/ECommerce.API
```

---

## Faz 2 — Giriş ve güvenlik (4–5 gün) · TDD

- [x] **Yönetici girişi:** `DefAdmin` + `PasswordHasher<T>` (PBKDF2, tek `PasswordHash` alanı); eski salt/hash ve `SecurityLevel` kaldırıldı. Gerektiğinde hash otomatik yenileniyor.
- [x] **JWT access token** (15 dk, HS256, `sub`/`name`/`role`) + **refresh token** (7 gün, veritabanında SHA-256 özeti, her kullanımda döndürülüyor, `httpOnly` + `Secure` + `SameSite=Strict` cookie, path `/api/v1/auth`).
- [x] Döndürülmüş bir refresh token tekrar kullanılırsa yöneticinin tüm oturumları kapatılıyor (çalınma tespiti).
- [x] Uçlar: `POST /api/v1/auth/login`, `/refresh`, `/logout`, `GET /me`, `POST /change-password` (parola değişince tüm oturumlar kapanıyor).
- [x] Yetkilendirme: varsayılan (fallback) politika kimliği doğrulanmış + `Admin` rolü; `[AllowAnonymous]` olmayan her uç korumalı. Anonim istekler bilinmeyen route'larda da 401 alıyor (route bilgisi sızmıyor).
- [x] Brute-force: login/refresh için IP başına rate limit (varsayılan 10/dk, 429 ProblemDetails) + 5 hatalı denemede 15 dk hesap kilitleme. Geçersiz kullanıcı ve kilitli hesap aynı genel 401 mesajını alıyor.
- [x] İlk yönetici seed'i: `Seed:Admin:*` ayarları verilirse ve kullanıcı yoksa açılışta oluşturuluyor; parola kuralları uygulanıyor.
- [x] Güvenlik: CORS (`Cors:AllowedOrigins`, credentials), HSTS (Development dışı), `nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`, CSP. `Jwt:SigningKey` en az 32 bayt değilse uygulama açılmıyor.
- [x] FluentValidation global action filtresi → 400 `ValidationProblemDetails`. Parola kuralı: en az 12 karakter, büyük/küçük harf, rakam, eskisinden farklı.
- [x] Bulunan hata: `PasswordHash` sütunu `varbinary`'den `nvarchar`'a `ALTER` ile çevrilemiyordu; migration sütunu silip yeniden ekleyecek şekilde düzeltildi (entegrasyon testi yakaladı).
- [ ] Müşteri (site kullanıcısı) kimlik doğrulaması Faz 4.2'de aynı altyapıyla eklenir.
- [ ] Scalar arayüzüne Bearer güvenlik şeması eklenmesi (OpenAPI document transformer) → Faz 3'te ilk Trendyol uçlarıyla.

**Kabul:** Korunan uçlar token'sız 401 dönüyor ✔; refresh rotasyonu, tekrar kullanım tespiti, kilitleme ve rate limit testlerle doğrulandı ✔ (48 unit + 18 entegrasyon testi).

**Yerel ayarlar (API):**
```bash
cd src/ECommerce.API
dotnet user-secrets set "Jwt:SigningKey" "<en az 32 karakterlik rastgele değer>"
dotnet user-secrets set "Seed:Admin:UserName" "admin"
dotnet user-secrets set "Seed:Admin:Password" "<güçlü parola>"
dotnet user-secrets set "Seed:Admin:Email" "<e-posta>"
dotnet user-secrets set "Cors:AllowedOrigins:0" "http://localhost:5173"
```

---

## Faz 3 — Trendyol ürün ve sipariş, uçtan uca (8–11 gün) · TDD

### 3.1 Client modernizasyonu
- [x] Client güncel uçlara göre baştan yazıldı: `apigw.trendyol.com` / `stageapigw.trendyol.com` (`Trendyol:BaseUrl`), `/integration/...` path'leri, sipariş için v2.
- [x] Arayüzler: `ITrendyolCatalogClient`, `ITrendyolProductClient`, `ITrendyolOrderClient`; sonuç tipi `TrendyolResponse<T>` (exception yerine durum kodu + ayrıştırılmış hata).
- [x] System.Text.Json; null alanlar gönderilmiyor. Boolean'lar `true/false`, tarihler Unix milisaniye olarak yazılıyor (eski `QueryBooleanOption` sorunu ortadan kalktı).
- [x] Resilience: GET'ler tekrar deneniyor; POST/PUT/DELETE **tekrar denenmiyor** (ürün yaratma/fiyat güncelleme idempotent değil). Ağ hataları, zaman aşımı ve devre kesici 503 sonucu olarak dönüyor.
- [x] 24 WireMock sözleşme testi: metod, path, Basic auth, User-Agent, query string, gövde JSON'u ve doküman örneklerine göre cevap ayrıştırma.
- [x] Nullable uyumlu modeller; `.editorconfig`'teki geçici CS8618 istisnası kaldırıldı. Newtonsoft.Json bağımlılığı kalktı.
- [ ] Stage ortamı için çıkış IP'sinin Trendyol'a whitelist ettirilmesi (çağrı merkezi üzerinden — operasyonel iş, satıcı tarafından).

### 3.2 Referans veriler
- [x] Kategori ağacı, leaf kategori araması (yol bilgisiyle, Türkçe büyük/küçük harf duyarsız), kategori özellikleri, özellik değerleri (tüm sayfalar), marka araması. Başarılı cevaplar 6 saat önbellekte; hatalar önbelleğe alınmıyor.

### 3.3 Ürün
- [x] Onaylı (`approved`, contentId + varyantlar) ve onaysız (`unapproved`, red sebepleri) ürün listeleme.
- [x] Ürün yaratma (v2), onaylı ürün içerik güncellemesi (`content-bulk-update`), onaysız ürün güncellemesi, silme, fiyat/stok güncelleme.
- [x] İstek validasyonu: 1000 kalem, barkod karakterleri, en fazla 8 https görsel, liste fiyatı ≥ satış fiyatı, KDV 0/1/10/20, stok ≤ 20.000, istek içinde tekrar eden barkod yok.
- [x] **Batch takibi:** dönen `batchRequestId` `Gnl_MarketplaceBatchRequests` tablosuna yazılıyor; `TrendyolBatchStatusWorker` (varsayılan 60 sn) bekleyenleri sorguluyor, hatalı kalemleri (barkod + sebepler) saklıyor. Trendyol sonuçları ~4 saat tuttuğu için daha eski veya Trendyol'da bulunamayan kayıtlar `Expired` oluyor.
- [ ] Varyant ve teslimat bilgisi güncellemesi (`variant-bulk-update`, `delivery-info-bulk-update`) → ihtiyaç olursa Faz 4.3'te.

### 3.4 Sipariş
- [x] Sipariş paketlerini çekme (v2): sayfa ≤ 200, aralık ≤ 14 gün, en fazla son 1 ay, 10.000 kayıt sınırı ve statü doğrulaması Trendyol'a gitmeden yapılıyor.
- [x] Paket statüsü: Picking → Invoiced (fatura numarasıyla).
- [x] Tedarik edememe bildirimi (sebep kodları 500–506, API'de isimle).
- [x] Fatura linki gönderme (https, fatura numarası biçimi) ve silme.
- [x] **Periyodik sipariş çekme → Faz 4.3'te yapıldı** (imleçli 14 günlük pencereler).
- [ ] (Opsiyonel) Webhook ile sipariş bildirimi — public URL gerektirdiği için Faz 5 sonrasında.

### 3.5 API uçları
- [x] `/api/v1/marketplaces/trendyol/` altında katalog (5), ürün (7), batch (2) ve sipariş (6) uçları; hepsi `Admin` yetkili (fallback politika).
- [x] Pazaryeri hataları ProblemDetails'e eşleniyor: Trendyol 400 → 400, 404 → 404, 409 → 409, 401/403 → 502 ("kimlik bilgileri reddedildi"), 429 → 503 + `Retry-After`, ulaşılamıyor → 502; Trendyol mesajları `errors`, orijinal kod `upstreamStatus` alanında.
- [x] API JSON'unda enum'lar string (`"ProductCreate"`, `"OutOfStock"`).
- [x] Scalar/OpenAPI'ye Bearer güvenlik şeması eklendi (Faz 2'den taşınan madde).
- [x] `ECommerce.CustomerPortal` ve eski Trendyol katmanı (adapter, DTO, mapping, servis) silindi.

**Kabul:** Sözleşme (24), unit (83) ve entegrasyon (29) testleri yeşil ✔. Stage/prod ortamında uçtan uca canlı akış ⏳ — Trendyol stage bilgileri ve IP whitelist'i gerekiyor; satıcı tarafında yapılınca `Trendyol:BaseUrl=https://stageapigw.trendyol.com` ile denenecek.

**Uçtan uca deneme sırası (Scalar üzerinden):**
1. `POST /api/v1/auth/login` → `accessToken`'ı Scalar'daki Bearer alanına gir.
2. `GET .../trendyol/categories/search?term=...` → leaf kategori, `GET .../categories/{id}/attributes` ve `.../values` → özellikler; `GET .../brands?name=...` → marka.
3. `POST .../trendyol/products` → 202 + `Location` (batch). `GET .../batches/{id}` ile sonucu izle.
4. `GET .../products/unapproved` / `approved` → ürün durumu; `POST .../products/price-inventory` → fiyat/stok.
5. `GET .../orders/packages?status=Created` → `POST .../{packageId}/picking` → `POST .../{packageId}/invoiced` → `POST .../{packageId}/invoice-link`.

---

## Faz 4 — Yerel e-ticaret + pazaryeri entegrasyonu (13–18 gün) · TDD

### 4.1 Katalog (yönetim + vitrin okuma)
- [x] Eski domain (29 entity, Sys* durum tabloları) kaldırıldı; yeni model: tek tablo kategori ağacı, marka, ürün + varyant (stok kodu, barkod, fiyat, liste fiyatı, stok, özellikler), en fazla 8 görsel. Türkçe slug üretimi.
- [x] Yönetici uçları (`/api/v1/catalog/...`): kategori (döngü ve silme kontrolleri), marka, ürün, varyant, yayınlama. Barkod/stok kodu tüm ürünlerde tekil; soft-delete edilenler tekilliği engellemiyor (filtreli unique index).
- [x] Görsel yükleme: JPEG/PNG/WEBP, içerik imzası kontrolü, 5 MB; yerel diskte saklanıp `/media` altından sunuluyor (`Media:RootPath`, `Media:PublicBaseUrl`). Silme ve sıralama.
- [x] Vitrin uçları (`/api/v1/store/...`, anonim): aktif kategori ağacı, ürün arama (kategori + alt kategoriler, marka, fiyat aralığı, stokta olanlar, sıralama), ürün detayı (kategori yolu, aktif varyantlar).
- [x] İş kuralı ihlalleri (`DomainException`) 400 ProblemDetails olarak dönüyor.

### 4.2 Müşteri, sepet ve sipariş (yerel site)
- [x] Müşteri kaydı/girişi (`/api/v1/customer/auth`): e-posta normalize ve tekil, kayıt sonrası otomatik giriş, ayrı refresh cookie'si. Giriş akışı yönetici ile ortak (`AccountAuthService<TAccount, TToken>`): kilitleme, rotasyon, tekrar kullanım tespiti. Misafir sipariş yok (karar).
- [x] Profil ve adres yönetimi (en fazla 10 adres, varsayılan adres kuralları). Müşteri uçları ayrı `Customer` politikasıyla korunuyor; müşteri ve yönetici token'ları birbirinin uçlarına erişemiyor (403).
- [x] Sepet: fiyat saklanmıyor, her okumada güncel fiyat/stok; yayından kalkan veya stoğu düşen ürün işaretleniyor; satır başı en fazla 20 adet.
- [x] Checkout: önizleme ve sipariş oluşturma. Stok sipariş anında düşülüyor (ayrı rezervasyon yok); varyanttaki `rowversion` ile eşzamanlı siparişlerde fazla satış engelleniyor (409). Ücretsiz kargo eşiği, kargo ücreti ve kapıda ödeme ücreti `Checkout` ayarlarından.
- [x] Sipariş durum makinesi: Ödeme bekliyor (havale) / Onaylandı (kapıda) → Hazırlanıyor → Kargoda → Teslim edildi; teslimattan önce İptal (stok geri ekleniyor). Durum geçmişi tutuluyor.
- [x] Ödeme: havale/EFT (banka bilgileri ve açıklama sipariş detayında; yönetici ödemeyi onaylıyor) ve kapıda ödeme.
- [x] Müşteri sipariş uçları: listeleme, detay, hazırlanmadan önce iptal. Yönetici sipariş uçları (`/api/v1/orders`): listeleme (durum, kanal, arama), detay, ödeme onayı, hazırlama, kargo (firma + takip no), teslim, iptal.
- [ ] Ödenmeyen havale siparişlerinin belirli süre sonra otomatik iptali → Faz 5 sonrası (zamanlanmış iş).
- [ ] Sipariş/kargo e-posta bildirimleri → MVP sonrası.

### 4.3 Pazaryeri entegrasyon modülü
- [x] **Soyutlama:** `IMarketplaceProvider` (ürün yayınla, toplu fiyat/stok gönder, sipariş sayfası çek, statü güncelle + pazaryeri sınırları: pencere, geriye bakış, sayfa, kalem sayısı). İlk implementasyon `TrendyolMarketplaceProvider`; stok senkronu, sipariş içe alma ve statü bildirimi servisleri pazaryerinden bağımsız, N11 aynı arayüzle eklenecek.
- [x] **Eşleştirme:** yerel kategori ↔ Trendyol leaf kategorisi (Trendyol ağacından doğrulanıyor), yerel marka ↔ Trendyol markası, varyant ↔ barkod. **Özellik eşlemesi** (`PUT .../mappings/categories/{id}/attributes`): sabit (ürün seviyesi) değerler ve varyant özelliği → Trendyol değer numarası/serbest değer; özellik ve değer numaraları Trendyol kategorisinden, serbest değer izni `allowCustom`'dan doğrulanıyor.
- [x] **Yayınlama** (`POST .../listings/{productId}`): aktif varyantlardan Trendyol v2 ürün isteği üretiliyor; özellikler kategori eşlemesinden çözülüyor, istekte gelenler aynı özellik numarasında eşlemeyi ezer. Zorunlu özellik eksikse Trendyol'a gitmeden hata. Durum: Gönderildi → (batch sonucu, `Gnl_MarketplaceBatchRequests`) Onay bekliyor / Hatalı → Yayında / Reddedildi + sebepler.
- [x] **Tek stok ve fiyat:** varyantın fiyat/stok/aktiflik değişikliği `InventoryOutboxInterceptor` ile aynı kayıtta outbox'a yazılıyor; işçi kabul edilmiş ürünleri 1000'lik paketlerle gönderiyor. 429'da pazaryeri 60 sn beklemeye alınıyor (deneme harcanmaz), geçici hatada artan aralıkla en fazla 5 deneme, Trendyol'un reddettiği istek hata mesajıyla `Failed`.
- [x] **Sipariş içe alma:** paketler `TY{paketNo}` numarasıyla yerel siparişe (kanal = Trendyol) dönüşüyor; paket no tekil dış referans → idempotent. Barkodla eşleşen varyantların stoğu düşüyor (sıfırın altına inmiyor), pazaryeri satır numarası ve kargo bilgisi saklanıyor. Sonraki çekimlerde durum ileri yönde yansıtılıyor, Trendyol'da iptal edilen siparişin stoğu geri ekleniyor.
- [x] **Periyodik sipariş çekme** (Faz 3.4'ten taşındı): son çekim zamanından (`Marketplace_SyncStates` imleci, 5 dk örtüşme) şimdiye kadar 14 günlük pencerelerle, `PackageLastModifiedDate`'e göre artan sırada; ilk çalışmada son 14 gün, en fazla 28 gün geriye. İmleç yalnızca pencere tamamen işlenince ilerliyor.
- [x] **Statü geri bildirimi:** Trendyol siparişi yerelde Hazırlanıyor olunca Picking, Kargoda olunca Invoiced (fatura numarası zorunlu; kargo bilgisi Trendyol'dan geliyor) aynı kayıtta `Marketplace_OrderStatusOutbox`'a yazılıyor ve işçi sırayla gönderiyor (önceki bildirim beklerken sonraki gönderilmiyor).
- [x] **Senkron logu ve tekrar deneme** (`/api/v1/marketplaces/sync`, Admin): `logs` (pazaryeri/işlem/sonuç filtresi), `states` (imleç, bekleme, son hata), `outbox/{Inventory|OrderStatus}` listeleme, `.../{id}/retry` ve `.../retry-failed`. Yayınlama, fiyat/stok, sipariş içe alma ve statü bildirimleri loglanıyor.
- [x] Arka plan işleri: batch + listing durumu (60 sn), stok/fiyat (60 sn), sipariş içe alma (5 dk), statü bildirimi (60 sn); `Trendyol:{BatchPolling|InventorySync|OrderImport|OrderStatusSync}` ile açılıp kapatılabiliyor. Elle tetikleme: `POST .../trendyol/sync/listings|inventory|orders|order-status`.
- [ ] Onaylı ürün içerik güncellemesinin (başlık/açıklama/görsel) yerelden otomatik gönderimi → MVP sonrası (fiyat/stok otomatik).

**Kabul:** Ürün yerelde oluşturulup Trendyol'a yayınlanabiliyor ✔; yerel stok/fiyat değişikliği Trendyol'a gidiyor ✔; site ve Trendyol siparişleri `/api/v1/orders` altında tek listede, ortak stoğu düşürüyor ✔. `MarketplaceAcceptanceTests` bu senaryoyu gerçek Trendyol client'ı + WireMock ve Testcontainers SQL Server ile uçtan uca doğruluyor; canlı deneme stage bilgileri gelince.

---

## Faz 5 — Docker ile yayın (3–4 gün)

- [x] Multi-stage `Dockerfile`: `runtime` (API, `app` kullanıcısı, `/health/live` health check) ve `migrator` aşamaları; restore katmanı cache'leniyor.
- [x] `docker-compose.yml` (`ecommerce-prod`): sqlserver, migrator, api, caddy; volume'ler: veritabanı, görseller, Caddy sertifikaları. Veritabanı ve API portu dışarıya kapalı; container logları döndürülüyor. Seq eklenmedi (gerekirse Serilog sink'i ile).
- [x] Migration'lar EF Core **migration bundle** ile ayrı `migrator` container'ında; API, migrator başarıyla bitmeden başlamıyor. İkinci çalıştırmada değişiklik yapmıyor (idempotent).
- [x] Reverse proxy + HTTPS: **Caddy** (Let's Encrypt otomatik); `ASPNETCORE_FORWARDEDHEADERS_ENABLED` ile gerçek istemci IP'si (giriş hız sınırı IP'ye göre çalışıyor).
- [x] Ortam konfigürasyonu: `.env` (şablon `.env.example`); `TRENDYOL_BASE_URL` ile Prod/Stage, ilk yönetici, havale bilgileri, `MSSQL_PID` (varsayılan Express).
- [x] CI: PR'larda image derleniyor; `master`'da `latest` + `sha-`, `v*` etiketlerinde sürüm etiketiyle GHCR'a push (`ecommerce-api`, `ecommerce-migrator`). Sunucuda `docker compose pull && docker compose up -d --no-build`.
- [x] Yedekleme: `docker/backup.sh` (veritabanı + görseller, gzip, `BACKUP_KEEP_DAYS`), `docker/restore.sh` (onaylı). Geri yükleme yerelde denendi.
- [ ] Sunucu çıkış IP'sinin Trendyol stage whitelist'ine eklenmesi → sunucu açılınca satıcı tarafından (adımlar `docs/DEPLOYMENT.md` §8).
- [ ] Trendyol webhook'u → API'de webhook ucu henüz yok (Faz 3.4, opsiyonel); periyodik sipariş çekme ile Faz 4.3'e bağlı.

**Kabul:** Yerelde `docker compose up -d` ile tüm yığın ayağa kalkıyor ✔; `https://localhost/health/ready` 200 ✔; HTTP → HTTPS yönlendirmesi ✔; yedek → değişiklik → geri yükleme denendi ✔. Temiz bir VPS'te ilk kurulum ⏳ (sunucu ve alan adı bekleniyor; adımlar `docs/DEPLOYMENT.md`).

---

## Faz 6 — Front-end (React veya Vue) (kapsama göre 15–25 gün)

- [ ] Ayrı repo veya `frontend/` klasörü; Vite + TypeScript.
- [ ] API client'ı OpenAPI'den otomatik üretilir.
- [ ] **Yönetim paneli:** giriş, dashboard, katalog, siparişler (tüm kanallar), Trendyol yayın/eşleştirme ekranları, senkron logu.
- [ ] **Vitrin:** ürün listeleme/detay, sepet, ödeme, müşteri hesabı.
- [ ] Docker image'ı ve compose'a eklenmesi.

---

## Özet takvim

| Faz | İçerik | Süre |
|---|---|---|
| 0 | Güvenlik, derleme, .NET 10 | 1 gün |
| 1 | API projesi, altyapı, test altyapısı | 6–8 gün |
| 2 | Giriş ve güvenlik | 4–5 gün |
| 3 | Trendyol ürün + sipariş | 8–11 gün |
| 4 | Yerel e-ticaret + entegrasyon | 13–18 gün |
| 5 | Docker ile yayın | 3–4 gün |
| 6 | Front-end | 15–25 gün |

Back-end (Faz 0–5): yaklaşık **7–9 hafta**. Front-end dahil: **10–14 hafta**.

---

## Açık kararlar

1. **React mı Vue mu?** Faz 6'ya kadar gerekmiyor. Yönetim paneli ve vitrin tek uygulama mı, iki ayrı uygulama mı olacak? (Vitrin için SEO önemliyse Next.js/Nuxt gibi SSR değerlendirilir.)
2. **Yayın hedefi:** VPS (Hetzner/DigitalOcean vb.) varsayıldı.

## Alınan kararlar

| Tarih | Karar |
|---|---|
| 2026-11-06 | Tek satıcı, SaaS değil. Back-end ve front-end ayrı projeler; front-end en son (React veya Vue). |
| 2026-11-06 | Mapping: AutoMapper kaldırılır, **Mapster** kullanılır. |
| 2026-11-06 | DI: Autofac + Castle interceptor'ları kaldırılır, **.NET'in yerleşik DI**'ı kullanılır. |
| 2026-11-06 | Test framework'ü: **xUnit**. |
| 2026-11-07 | Domain modeli yeniden tasarlandı; eski tablolar migration ile kaldırıldı. |
| 2026-11-07 | Ödeme (MVP): havale/EFT ve kapıda ödeme; sipariş yönetici onayıyla ödendi sayılır. Sağlayıcı (iyzico/PayTR) sonra, arayüz arkasında. |
| 2026-11-07 | Görseller yerel diskte, API `/media` altından sunuyor; depolama `IFileStorage` arkasında (S3'e geçiş kolay). |
| 2026-11-07 | Yayın: tek VPS, Docker Compose; reverse proxy **Caddy**; gizli bilgiler `.env` (chmod 600) — Docker secrets tek sunucuda ek koruma sağlamadığı için kullanılmadı. |
| 2026-11-07 | Migration'lar API açılışında değil, ayrı `migrator` container'ında (EF Core migration bundle). SQL Server yayında Express sürümü. |
| 2026-11-07 | Yerel sitede sipariş için üyelik zorunlu (misafir sipariş yok). |