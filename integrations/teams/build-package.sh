#!/usr/bin/env sh
# Builds the Microsoft Teams app package (OPP-23): the manifest and the two icons in one zip, at the root of
# the archive as Teams expects. Usage: integrations/teams/build-package.sh [output.zip]
set -e
cd "$(dirname "$0")"
out="${1:-open-planning-poker-teams.zip}"
case "$out" in /*) ;; *) out="$PWD/$out" ;; esac
rm -f "$out"
PY_BIN=; for c in python3 python; do if "$c" -c "" 2>/dev/null; then PY_BIN=$c; break; fi; done
"$PY_BIN" - "$out" <<'PY'
import sys, zipfile
with zipfile.ZipFile(sys.argv[1], 'w', zipfile.ZIP_DEFLATED) as z:
    for name in ('manifest.json', 'color.png', 'outline.png'):
        z.write(name, name)
PY
echo "Teams app package: $out"
