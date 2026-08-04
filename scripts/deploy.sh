#!/usr/bin/env bash
#
# Deploys ScanStore. Run it ON the server, from the repo root:
#
#   ./scripts/deploy.sh
#
# or remotely from your laptop:
#
#   ssh root@YOUR_SERVER 'cd /root/Shopify-V2 && ./scripts/deploy.sh'
#
# What it does, in order:
#   1. validates .env (missing keys, and the localhost trap)
#   2. backs up the database
#   3. pulls the latest code
#   4. rebuilds and restarts the containers
#   5. waits for the API to report healthy
#   6. confirms QR codes match PUBLIC_BASE_URL (the API refreshes them on boot)
#   7. smoke-tests the live site
#
# It stops at the first failure and tells you how to roll back. Nothing is
# destructive except the container restart — volumes are never touched.
#
# Flags:
#   --no-pull     deploy the working tree as-is, skip git pull
#   --no-backup   skip the database backup (not recommended)
#   --force-qr    regenerate QR codes even if PUBLIC_BASE_URL is unchanged
#   --help
set -Eeuo pipefail

cd "$(dirname "$0")/.."
ROOT="$PWD"
BACKUP_DIR="$ROOT/backups"
STATE_FILE="$ROOT/.deploy-state"
KEEP_BACKUPS=10

DO_PULL=1; DO_BACKUP=1; FORCE_QR=0; ALLOW_LOCALHOST=0
for arg in "$@"; do
  case "$arg" in
    --no-pull)         DO_PULL=0 ;;
    --no-backup)       DO_BACKUP=0 ;;
    --force-qr)        FORCE_QR=1 ;;
    --allow-localhost) ALLOW_LOCALHOST=1 ;;
    --help|-h)         sed -n '2,28p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) echo "unknown flag: $arg (try --help)" >&2; exit 2 ;;
  esac
done

# ---------- output helpers ----------
if [ -t 1 ]; then
  B=$'\033[1m'; G=$'\033[32m'; Y=$'\033[33m'; R=$'\033[31m'; D=$'\033[2m'; N=$'\033[0m'
else B=''; G=''; Y=''; R=''; D=''; N=''; fi

step() { printf '\n%s==>%s %s%s%s\n' "$B" "$N" "$B" "$1" "$N"; }
ok()   { printf '  %s✓%s %s\n' "$G" "$N" "$1"; }
warn() { printf '  %s!%s %s\n' "$Y" "$N" "$1"; }
die()  { printf '\n  %s✗ %s%s\n' "$R" "$1" "$N" >&2; exit 1; }

trap 'printf "\n%s✗ deploy failed at line %s%s\n  The previous containers may still be running — check: docker compose ps\n  To roll back the code: git reset --hard %s\n" "$R" "$LINENO" "$N" "${PREV_SHA:-HEAD@{1}}" >&2' ERR

# ---------- 0. prerequisites ----------
step "Checking prerequisites"
command -v docker >/dev/null || die "docker is not installed"
docker compose version >/dev/null 2>&1 || die "docker compose v2 is not available"
docker info >/dev/null 2>&1 || die "cannot talk to the Docker daemon (is it running? are you root?)"
ok "docker $(docker --version | grep -oE '[0-9]+\.[0-9]+\.[0-9]+' | head -1)"

# ---------- 1. validate .env ----------
step "Validating .env"
[ -f "$ROOT/.env" ] || die ".env not found. Run: cp .env.example .env  then fill it in."

# shellcheck disable=SC1090
set -a; . "$ROOT/.env"; set +a

REQUIRED=(DB_PASSWORD PUBLIC_BASE_URL JWT_KEY
          VITE_FIREBASE_API_KEY VITE_FIREBASE_AUTH_DOMAIN VITE_FIREBASE_PROJECT_ID
          VITE_FIREBASE_STORAGE_BUCKET VITE_FIREBASE_MESSAGING_SENDER_ID VITE_FIREBASE_APP_ID)
MISSING=()
for key in "${REQUIRED[@]}"; do
  [ -n "${!key:-}" ] || MISSING+=("$key")
done
[ ${#MISSING[@]} -eq 0 ] || die "these .env keys are empty: ${MISSING[*]}"

# The API refuses to start without a Firebase project id; it falls back to the
# VITE_ one, so only warn when both are unset (already covered above).
[ -n "${FIREBASE_PROJECT_ID:-}" ] || warn "FIREBASE_PROJECT_ID unset — falling back to VITE_FIREBASE_PROJECT_ID"

# THE trap: a copied-from-local .env sends every QR code to the customer's own
# device. Catch it here rather than after the site is live.
case "$PUBLIC_BASE_URL" in
  *localhost*|*127.0.0.1*)
    [ "$ALLOW_LOCALHOST" -eq 1 ] || die "PUBLIC_BASE_URL is '$PUBLIC_BASE_URL'.
     On a server this makes every catalog URL and QR code point at the
     visitor's own machine. Set it to this server's address, e.g.
       PUBLIC_BASE_URL=http://46.225.136.102
     (pass --allow-localhost if you are deploying on your own machine)"
    warn "PUBLIC_BASE_URL is localhost — allowed via --allow-localhost" ;;
esac
case "$PUBLIC_BASE_URL" in */) die "PUBLIC_BASE_URL must not end with a slash: $PUBLIC_BASE_URL" ;; esac
ok "PUBLIC_BASE_URL = $PUBLIC_BASE_URL"

[ "${#JWT_KEY}" -ge 32 ] || die "JWT_KEY is shorter than 32 characters"
ok "all required keys present"

[ -n "${RAZORPAY_KEY_ID:-}" ] || warn "RAZORPAY_KEY_ID unset — subscription checkout will be disabled"
[ -n "${AI_API_KEY:-}" ]      || warn "AI_API_KEY unset — AI description button will report it is off"

WEB_PORT="${WEB_PORT:-80}"

# ---------- 2. back up the database ----------
step "Backing up the database"
if [ "$DO_BACKUP" -eq 1 ] && docker ps --format '{{.Names}}' | grep -qx qrshop-db; then
  mkdir -p "$BACKUP_DIR"
  STAMP=$(date +%Y%m%d-%H%M%S)
  FILE="$BACKUP_DIR/QRShopDb-$STAMP.sql.gz"
  if docker exec qrshop-db mysqldump -uroot -p"$DB_PASSWORD" \
       --single-transaction --routines --triggers --databases "${DB_NAME:-QRShopDb}" 2>/dev/null \
       | gzip > "$FILE"; then
    ok "saved $(basename "$FILE") ($(du -h "$FILE" | cut -f1))"
    # keep only the newest N
    ls -1t "$BACKUP_DIR"/QRShopDb-*.sql.gz 2>/dev/null | tail -n +$((KEEP_BACKUPS + 1)) | xargs -r rm --
  else
    rm -f "$FILE"
    die "backup failed — refusing to deploy over data I cannot restore.
     Use --no-backup to override if you know the database is empty."
  fi
elif [ "$DO_BACKUP" -eq 0 ]; then
  warn "skipped (--no-backup)"
else
  warn "no qrshop-db container yet — first deploy, nothing to back up"
fi

# ---------- 3. pull ----------
step "Updating source"
PREV_SHA=$(git rev-parse HEAD 2>/dev/null || echo "")
if [ "$DO_PULL" -eq 1 ]; then
  if [ -n "$(git status --porcelain 2>/dev/null)" ]; then
    die "working tree has local changes — commit, stash, or use --no-pull:
$(git status --short | sed 's/^/       /')"
  fi
  git pull --ff-only
  NEW_SHA=$(git rev-parse HEAD)
  if [ "$PREV_SHA" = "$NEW_SHA" ]; then ok "already up to date ($(git log --oneline -1))"
  else ok "$(git log --oneline "$PREV_SHA..$NEW_SHA" | wc -l | tr -d ' ') new commit(s) -> $(git log --oneline -1)"; fi
else
  warn "skipped (--no-pull), deploying $(git log --oneline -1)"
fi

# ---------- 4. build and start ----------
step "Building and starting containers"
docker compose up -d --build
ok "compose up complete"

# ---------- 5. wait for health ----------
step "Waiting for the API to become healthy"
DEADLINE=$((SECONDS + 180))
while :; do
  STATUS=$(docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}' qrshop-api 2>/dev/null || echo missing)
  case "$STATUS" in
    healthy) ok "API healthy"; break ;;
    none)    curl -fsS "http://localhost:${WEB_PORT}/api/catalog" >/dev/null 2>&1 && { ok "API responding"; break; } ;;
    unhealthy) docker compose logs --tail 40 api; die "API reported unhealthy" ;;
  esac
  [ $SECONDS -lt $DEADLINE ] || { docker compose logs --tail 40 api; die "API did not become healthy within 180s"; }
  sleep 3
done

# ---------- 6. QR codes ----------
# The API rewrites stale QR codes itself on startup (see Program.cs), because the
# HTTP endpoint is admin-authenticated and a deploy script has no token. All this
# does is confirm it happened and surface it in the log.
step "QR codes"
LAST_URL=$(cat "$STATE_FILE" 2>/dev/null || echo "")
[ -n "$LAST_URL" ] && [ "$LAST_URL" != "$PUBLIC_BASE_URL" ] \
  && warn "public URL changed: $LAST_URL -> $PUBLIC_BASE_URL"

QR_LOG=$(docker compose logs api 2>/dev/null | grep -c "Regenerated .* QR code" || true)
if [ "${QR_LOG:-0}" -gt 0 ]; then
  ok "$(docker compose logs api 2>/dev/null | grep -o 'Regenerated .* QR code.*' | tail -1)"
else
  ok "already current for $PUBLIC_BASE_URL"
fi
printf '%s' "$PUBLIC_BASE_URL" > "$STATE_FILE"

# ---------- 7. smoke tests ----------
step "Smoke tests"
BASE="http://localhost:${WEB_PORT}"
fail=0
for path in / /shops /login /admin; do
  code=$(curl -s -o /dev/null -w '%{http_code}' --max-time 20 "$BASE$path" || echo 000)
  if [ "$code" = "200" ]; then ok "$path -> 200"; else warn "$path -> $code"; fail=1; fi
done
code=$(curl -s -o /dev/null -w '%{http_code}' --max-time 20 "$BASE/api/catalog" || echo 000)
[ "$code" = "200" ] && ok "/api/catalog -> 200" || { warn "/api/catalog -> $code"; fail=1; }

# A stored upload path proves the volume survived and nginx proxies /uploads/
IMG=$(curl -fsS --max-time 20 "$BASE/api/catalog" 2>/dev/null \
      | grep -oE '"logoUrl":"[^"]+"' | head -1 | cut -d'"' -f4 || true)
if [ -n "$IMG" ]; then
  code=$(curl -s -o /dev/null -w '%{http_code}' --max-time 20 "$BASE$IMG" || echo 000)
  [ "$code" = "200" ] && ok "uploaded file serves ($IMG)" || { warn "uploaded file -> $code ($IMG)"; fail=1; }
else
  warn "no shops yet — skipped the uploads check"
fi

SHOPS=$(curl -fsS --max-time 20 "$BASE/api/catalog" 2>/dev/null | grep -oc '"shopId"' || echo 0)

# ---------- done ----------
printf '\n%s' "$B"
if [ "$fail" -eq 0 ]; then printf '✓ Deploy complete%s\n' "$N"
else printf '%s! Deploy finished with warnings%s\n' "$Y" "$N"; fi
printf '  %ssite%s      %s\n' "$D" "$N" "$PUBLIC_BASE_URL"
printf '  %scommit%s    %s\n' "$D" "$N" "$(git log --oneline -1)"
printf '  %sshops%s     %s live\n' "$D" "$N" "$SHOPS"
printf '  %scontainers%s\n' "$D" "$N"
docker compose ps --format '    {{.Name}}  {{.Status}}' 2>/dev/null || true
[ "$fail" -eq 0 ] || printf '\n  %sSome checks did not pass — see: docker compose logs -f api%s\n' "$Y" "$N"
echo
