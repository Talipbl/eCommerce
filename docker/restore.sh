#!/usr/bin/env bash
# Veritabanını (ve verilirse görselleri) yedekten geri yükler; mevcut veriler silinir.
# Kullanım: docker/restore.sh [--yes] <ECommerce-*.bak.gz> [media-*.tar.gz]
set -euo pipefail
export MSYS_NO_PATHCONV=1  # Windows Git Bash container yollarını dönüştürmesin
cd "$(dirname "$0")/.."

CONFIRMED=false
if [ "${1:-}" = "--yes" ]; then CONFIRMED=true; shift; fi
DB_FILE="${1:?Veritabanı yedeği (ECommerce-*.bak.gz) belirtilmeli}"
MEDIA_FILE="${2:-}"
CONTAINER_FILE="/var/opt/mssql/backup/restore.bak"

[ -f "$DB_FILE" ] || { echo "Bulunamadı: $DB_FILE" >&2; exit 1; }
[ -z "$MEDIA_FILE" ] || [ -f "$MEDIA_FILE" ] || { echo "Bulunamadı: $MEDIA_FILE" >&2; exit 1; }

if [ "$CONFIRMED" != true ]; then
  read -r -p "ECommerce veritabanı$([ -n "$MEDIA_FILE" ] && echo " ve görseller") bu yedekle DEĞİŞTİRİLECEK. Devam için 'evet' yazın: " answer
  [ "$answer" = "evet" ] || { echo "İptal edildi."; exit 1; }
fi

sqlcmd() {
  docker compose exec -T sqlserver bash -c \
    "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"\$MSSQL_SA_PASSWORD\" -C -b -Q \"$1\""
}

if [ -n "$MEDIA_FILE" ]; then
  echo "Görseller geri yükleniyor..."
  docker compose exec -T api sh -c 'find /app/media -mindepth 1 -delete && tar xzf - -C /app/media' < "$MEDIA_FILE"
fi

echo "API durduruluyor..."
docker compose stop api

echo "Veritabanı geri yükleniyor..."
docker compose exec -T sqlserver mkdir -p /var/opt/mssql/backup
gunzip -c "$DB_FILE" | docker compose exec -T sqlserver sh -c "cat > $CONTAINER_FILE"
sqlcmd "IF DB_ID('ECommerce') IS NOT NULL ALTER DATABASE [ECommerce] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [ECommerce] FROM DISK = N'$CONTAINER_FILE' WITH REPLACE, CHECKSUM; ALTER DATABASE [ECommerce] SET MULTI_USER;"
docker compose exec -T sqlserver rm -f "$CONTAINER_FILE"

echo "Servisler başlatılıyor (yeni migration'lar varsa migrator uygular)..."
docker compose up -d

echo "Tamamlandı."
