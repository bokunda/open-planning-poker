#!/usr/bin/env bash
# Monthly restore test (OPP-36): the newest backup is restored into a throwaway PostgreSQL container
# (no ports, no network) and checked with a few queries. The container is removed in any case.
set -euo pipefail
: "${BACKUP_PASSPHRASE:?BACKUP_PASSPHRASE is not set}"
DB="${DB_NAME:-opp-gameengine}"
DIR="$HOME/opp-backups"
TEST=opp-restore-test

latest=$(ls -1t "$DIR"/opp-*-"$DB".dump.gpg 2>/dev/null | head -1)
[ -n "$latest" ] || { echo "No backup found in $DIR" >&2; exit 1; }
trap 'docker rm -f $TEST >/dev/null 2>&1 || true' EXIT
docker rm -f $TEST >/dev/null 2>&1 || true
docker run -d --name $TEST --network none -e POSTGRES_PASSWORD=restore-test postgres:15.5 >/dev/null
for i in $(seq 1 60); do docker exec $TEST pg_isready -U postgres -q && break; sleep 1; done
docker exec $TEST createdb -U postgres restored

gpg --batch --quiet --pinentry-mode loopback --decrypt --passphrase-fd 3 "$latest" 3<<<"$BACKUP_PASSPHRASE" \
  | docker exec -i $TEST pg_restore -U postgres --dbname=restored --no-owner --exit-on-error

q() { docker exec $TEST psql -U postgres -d restored -Atc "$1"; }
games=$(q "select count(*) from games")
tickets=$(q "select count(*) from tickets")
migrations=$(q "select count(*) from \"__EFMigrationsHistory\"")
[ "$migrations" -gt 0 ] || { echo "Restore test failed: no EF migrations in the restored database" >&2; exit 1; }
echo "Restore test of $(basename "$latest"): $games games, $tickets tickets, $migrations migrations"
