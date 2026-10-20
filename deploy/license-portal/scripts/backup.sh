#!/usr/bin/env bash
# Run from deploy/license-portal on the VPS (or via cron).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
BACKUP_DIR="${BACKUP_DIR:-/var/backups/inventorypos-portal}"
STAMP="$(date +%F)"
mkdir -p "$BACKUP_DIR"

cd "$ROOT"
docker compose exec -T postgres pg_dump -U portal inventorypos_license_portal \
  | gzip > "$BACKUP_DIR/db-$STAMP.sql.gz"

tar czf "$BACKUP_DIR/keys-$STAMP.tgz" -C "$ROOT" data/keys

# Named volume for releases (compose project name may vary; override RELEASES_VOLUME if needed)
RELEASES_VOLUME="${RELEASES_VOLUME:-license-portal_portal_releases}"
docker run --rm \
  -v "${RELEASES_VOLUME}:/data:ro" \
  -v "$BACKUP_DIR:/out" \
  alpine tar czf "/out/releases-$STAMP.tgz" -C /data .

echo "Backups written to $BACKUP_DIR"
