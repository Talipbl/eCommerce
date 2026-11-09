#!/usr/bin/env bash
# Veritabanının ve ürün görsellerinin yedeğini alır, eski yedekleri siler.
# Kullanım: docker/backup.sh [hedef klasör]   (varsayılan: ./backups)
set -euo pipefail
export MSYS_NO_PATHCONV=1  # Windows Git Bash container yollarını dönüştürmesin
cd "$(dirname "$0")/.."

BACKUP_DIR="${1:-${BACKUP_DIR:-./backups}}"
KEEP_DAYS="${BACKUP_KEEP_DAYS:-$(grep -E '^BACKUP_KEEP_DAYS=' .env 2>/dev/null | cut -d= -f2 | tr -d '\r' || true)}"
KEEP_DAYS="${KEEP_DAYS:-14}"
STAMP="$(date -u +%Y%m%d-%H%M%S)"
CONTAINER_FILE="/var/opt/mssql/backup/ECommerce-$STAMP.bak"
mkdir -p "$BACKUP_DIR"

sqlcmd() {
  docker compose exec -T sqlserver bash -c \
    "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"\$MSSQL_SA_PASSWORD\" -C -b -Q \"$1\""
}

echo "Veritabanı yedekleniyor..."
docker compose exec -T sqlserver mkdir -p /var/opt/mssql/backup
sqlcmd "BACKUP DATABASE [ECommerce] TO DISK = N'$CONTAINER_FILE' WITH CHECKSUM, INIT"
docker compose exec -T sqlserver cat "$CONTAINER_FILE" | gzip > "$BACKUP_DIR/ECommerce-$STAMP.bak.gz"
docker compose exec -T sqlserver rm -f "$CONTAINER_FILE"

echo "Görseller yedekleniyor..."
docker compose exec -T api tar czf - -C /app/media . > "$BACKUP_DIR/media-$STAMP.tar.gz"

find "$BACKUP_DIR" -maxdepth 1 \( -name 'ECommerce-*.bak.gz' -o -name 'media-*.tar.gz' \) -mtime +"$KEEP_DAYS" -delete

echo "Tamamlandı: $BACKUP_DIR/ECommerce-$STAMP.bak.gz, $BACKUP_DIR/media-$STAMP.tar.gz"
