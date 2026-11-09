# Yayın (Docker) — Sunucu kurulumu ve işletim

Bu doküman, API'nin temiz bir Linux sunucusunda (VPS) Docker ile yayınlanmasını anlatır. Yapı:

```
İnternet ──443/80──▶ caddy (HTTPS, Let's Encrypt) ──▶ api:8080 ──▶ sqlserver:1433
                                                       │               │
                                                [volume: media]  [volume: sqlserver-data]
migrator: API'den önce bir kez çalışır, migration'ları uygular ve kapanır.
```

| Dosya | Görevi |
|---|---|
| `Dockerfile` | `runtime` (API) ve `migrator` (EF Core migration bundle) image'ları |
| `docker-compose.yml` | Yayın ortamı: sqlserver, migrator, api, caddy |
| `docker/Caddyfile` | Reverse proxy ve HTTPS |
| `docker/backup.sh`, `docker/restore.sh` | Veritabanı + görsel yedeği ve geri yükleme |
| `.env` (git'te yok) | Gizli bilgiler ve ortam ayarları; şablon `.env.example` |

Image'lar CI'da üretilir (`.github/workflows/ci.yml`): `master`'a her push'ta `ghcr.io/talipbl/ecommerce-api` ve `ecommerce-migrator` için `latest` + `sha-<commit>`, `v1.2.0` gibi bir etikette `1.2.0` + `1.2` etiketleri.

---

## 1. Sunucu gereksinimleri

- Ubuntu 24.04 LTS (veya güncel Debian), x86_64.
- **En az 4 GB RAM** (SQL Server tek başına ~2 GB ister), 2 vCPU, 40 GB disk.
- Alan adı: API için bir A kaydı (ör. `api.ornek.com` → sunucu IP'si). DNS yayılmadan Caddy sertifika alamaz.

## 2. Sunucuyu hazırla

```bash
# Docker Engine + Compose eklentisi (resmi betik)
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER   # oturumu kapatıp yeniden açın

# Güvenlik duvarı: yalnızca SSH, HTTP, HTTPS
sudo ufw allow OpenSSH
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw allow 443/udp
sudo ufw enable
```

> Docker, yayınlanan portlar için ufw'yi atlar. Bu yüzden compose'da yalnızca Caddy port yayınlıyor; API ve SQL Server'a `ports:` eklemeyin.
> `docker` grubundaki kullanıcı fiilen root yetkisine sahiptir; bu gruba yalnızca yöneticiyi ekleyin.

## 3. Dosyaları sunucuya al

```bash
git clone https://github.com/Talipbl/ECommerce.git /opt/ecommerce
cd /opt/ecommerce
cp .env.example .env
chmod 600 .env
```

`.env`'i doldurun (değerlerde `;` `$` `"` `'` kullanmayın):

| Değişken | Not |
|---|---|
| `DOMAIN` | `api.ornek.com` |
| `MSSQL_SA_PASSWORD` | Yeni ve güçlü bir parola |
| `JWT_SIGNING_KEY` | **Sunucuda yeniden üretin:** `openssl rand -base64 48` |
| `TRENDYOL_*` | Prod bilgileri; Stage için `TRENDYOL_BASE_URL=https://stageapigw.trendyol.com` (bkz. §8) |
| `SEED_ADMIN_*` | İlk kurulumda doldurun; ilk girişten sonra parolayı değiştirip `SEED_ADMIN_PASSWORD`'ü silin |
| `BANK_*` | Havale/EFT siparişlerinde müşteriye gösterilir |
| `MSSQL_PID` | `Express` (ücretsiz, veritabanı başına 10 GB). Developer sürümü yayında lisanslı değildir |
| `IMAGE_TAG` | `latest` veya sabit bir sürüm (`1.2.0`, `sha-abc1234`) |

GHCR paketleri özelse sunucuyu bir kez yetkilendirin (GitHub → Settings → Developer settings → *fine-grained* veya classic token, yalnızca `read:packages`):

```bash
echo '<token>' | docker login ghcr.io -u Talipbl --password-stdin
```

## 4. İlk açılış

```bash
docker compose pull
docker compose up -d --no-build
docker compose ps -a
```

Beklenen: `migrator` → `Exited (0)`, `sqlserver` ve `api` → `healthy`, `caddy` → `Up`.

```bash
curl -i https://api.ornek.com/health/ready     # 200 Healthy
docker compose logs caddy | grep -i certificate # sertifika alındı mı
```

`--no-build`: sunucuda kaynak koddan derleme yapılmaz; yalnızca CI'ın ürettiği image'lar kullanılır.

## 5. Güncelleme ve geri dönüş

```bash
cd /opt/ecommerce
git pull                        # compose/Caddyfile/betik değişiklikleri için
docker compose pull
docker compose up -d --no-build # migrator yeni migration'ları uygular, sonra API yeniden başlar
docker image prune -f
```

Bir sürüm sorunluysa `.env`'de `IMAGE_TAG`'i önceki sürüme (`sha-...` veya `1.1.0`) çekip aynı iki komutu çalıştırın.

> Geri dönüşte veritabanı şeması geri alınmaz. Yeni sürüm şemayı geriye uyumsuz değiştirdiyse önce §6'daki yedekten geri dönün. Bu yüzden her güncellemeden önce `docker/backup.sh` çalıştırın.

## 6. Yedekleme ve geri yükleme

```bash
docker/backup.sh                  # ./backups/ECommerce-<zaman>.bak.gz ve media-<zaman>.tar.gz
docker/backup.sh /mnt/yedek       # başka bir klasöre
```

`BACKUP_KEEP_DAYS` (varsayılan 14) günden eski yedekler silinir. Her gece 03:00'te otomatik yedek için önce `mkdir -p /opt/ecommerce/backups`, sonra `crontab -e`:

```
0 3 * * * /opt/ecommerce/docker/backup.sh >> /opt/ecommerce/backups/backup.log 2>&1
```

**Yedekler sunucu dışına da kopyalanmalı** (disk arızası/sunucu kaybı). Ör. `rclone` ile S3 uyumlu bir depoya ya da başka bir sunucuya `rsync`.

Geri yükleme (mevcut veriler silinir; onay ister):

```bash
docker/restore.sh backups/ECommerce-20261107-030000.bak.gz backups/media-20261107-030000.tar.gz
```

Betik API'yi durdurur, veritabanını ve (verildiyse) görselleri geri yükler, ardından `docker compose up -d` ile servisleri başlatır. Yedek eski bir sürümdense migrator eksik migration'ları uygular.

**Geri yükleme provası:** 2026-11-07'de yerelde denendi. Yedek alındı → veritabanına tablo ve görsel klasörüne dosya eklendi → geri yüklendi → ikisi de yedekteki hâline döndü, `/health/ready` 200. Yayında üç ayda bir, ayrı bir sunucuda tekrarlanması önerilir.

## 7. İzleme ve loglar

```bash
docker compose logs -f api            # canlı API logları (Serilog, konsol)
docker compose logs --since 1h caddy
docker stats
```

Container logları 5 × 10 MB ile sınırlı (compose'daki `x-logging`). Merkezi log gerekirse (Seq vb.) Serilog sink'i ile sonradan eklenebilir.

## 8. Trendyol

**Stage IP whitelist'i.** Stage ortamı yalnızca whitelist'teki IP'lerden gelen isteklere cevap verir; diğerlerine 503 döner.

1. Sunucunun çıkış IP'sini öğrenin: `curl -4 https://ifconfig.me`
2. Bu IP'yi Trendyol satıcı destek / çağrı merkezi üzerinden stage whitelist'ine ekletin (satıcı tarafından yapılır).
3. `.env`'de `TRENDYOL_BASE_URL=https://stageapigw.trendyol.com` ve stage satıcı bilgilerini girip `docker compose up -d --no-build`.
4. Uçtan uca deneme sırası için `docs/MVP-PLAN.md` → Faz 3, "Uçtan uca deneme sırası".

Prod'a geçerken `TRENDYOL_BASE_URL` satırını boşaltın (varsayılan Prod) ve Prod bilgilerini girin.

**Webhook.** Sipariş bildirimi için Trendyol webhook'u henüz API'de bir uç olarak yok (Faz 3.4'te opsiyonel). Uç eklendiğinde Trendyol'a verilecek adres `https://<DOMAIN>/...` olacak; sunucu tarafında ek bir ayar gerekmiyor, Caddy HTTPS'i zaten sağlıyor. O zamana kadar siparişler periyodik çekme ile alınır.

## 9. Yerelde yayın ortamını denemek

```bash
cp .env.example .env    # DOMAIN=localhost
docker compose up -d --build
curl -k https://localhost/health/ready
```

`localhost` için Caddy kendi yerel sertifikasını üretir (`-k` bu yüzden). Geliştirme için yalnızca SQL Server gerekiyorsa `docker-compose.dev.yml` kullanılmaya devam eder; iki ortam ayrı compose projeleridir (`ecommerce` / `ecommerce-prod`).
