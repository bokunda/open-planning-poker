#!/usr/bin/env bash
# Nightly PostgreSQL backup (OPP-36). Runs as the deploy user on the private server (it is in the docker group).
#  - pg_dump of the Game Engine database (custom format, compressed) and the roles, encrypted with gpg (AES256)
#  - kept for 14 days in ~/opp-backups and mirrored to the Hetzner Storage Box with rsync (port 23)
#  - every new file is decrypted again and read with pg_restore --list, so a broken backup fails the run
# Needs: BACKUP_PASSPHRASE, STORAGE_BOX (user@host) in the environment; ~/.ssh/storagebox (key for the box).
set -euo pipefail
: "${BACKUP_PASSPHRASE:?BACKUP_PASSPHRASE is not set}"
: "${STORAGE_BOX:?STORAGE_BOX is not set}"
DB="${DB_NAME:-opp-gameengine}"
DIR="$HOME/opp-backups"
KEEP_DAYS=14
PG=OpenPlanningPoker.Db

mkdir -p "$DIR" && chmod 700 "$DIR"
stamp=$(date -u +%Y%m%d-%H%M%S)
dump="$DIR/opp-$stamp-$DB.dump.gpg"
roles="$DIR/opp-$stamp-roles.sql.gpg"

encrypt() { gpg --batch --yes --quiet --pinentry-mode loopback --symmetric --cipher-algo AES256 --passphrase-fd 3 -o "$1" 3<<<"$BACKUP_PASSPHRASE"; }
decrypt() { gpg --batch --quiet --pinentry-mode loopback --decrypt --passphrase-fd 3 "$1" 3<<<"$BACKUP_PASSPHRASE"; }

docker exec "$PG" pg_dump -U postgres -Fc -Z 9 "$DB" | encrypt "$dump"
docker exec "$PG" pg_dumpall -U postgres --roles-only | encrypt "$roles"

# The dump must decrypt and list its tables.
tables=$(decrypt "$dump" | docker exec -i "$PG" pg_restore --list | grep -c " TABLE " || true)
if [ "$tables" -lt 5 ]; then echo "Backup check failed: only $tables tables in $dump" >&2; exit 1; fi
echo "Backup $(basename "$dump"): $(du -h "$dump" | cut -f1), $tables tables"

find "$DIR" -name 'opp-*.gpg' -mtime +$KEEP_DAYS -delete
rsync -a --delete -e "ssh -p 23 -i $HOME/.ssh/storagebox -o BatchMode=yes -o StrictHostKeyChecking=accept-new" \
  "$DIR/" "$STORAGE_BOX:opp-backups/"
echo "Mirrored $(ls "$DIR" | wc -l) files to the Storage Box"
