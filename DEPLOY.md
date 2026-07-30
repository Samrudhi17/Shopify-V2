# Deploying to Hetzner

## 1. Server

Every image in this stack (MySQL, .NET, nginx, Caddy) publishes native
**linux/arm64** builds, so Hetzner's Ampere **CAX** servers work — as do the
x86 **CX/CPX** lines.

| Requirement | Note |
| --- | --- |
| **CAX11** (2 vCPU / 4 GB) or better | Comfortable for this stack. 2 GB would be tight. |
| Ubuntu 24.04 | Anything with Docker works; this guide assumes Ubuntu. |

## 2. Prepare the server

```bash
ssh root@YOUR_SERVER_IP

# Docker
curl -fsSL https://get.docker.com | sh

# Firewall
ufw allow OpenSSH && ufw allow 80 && ufw allow 443 && ufw enable
```

## 3. Get the code and configure

```bash
git clone <your-repo> /opt/qrshop && cd /opt/qrshop
cp .env.example .env
nano .env          # fill in every value
```

`.env` must contain a strong `DB_PASSWORD`, a random `JWT_KEY`
(`openssl rand -base64 48`), your `PUBLIC_BASE_URL`, and the six
`VITE_FIREBASE_*` values from the Firebase console.

## 4. Run it

**Plain HTTP first** (confirm it works at `http://YOUR_SERVER_IP`):

```bash
docker compose up -d --build
docker compose logs -f api
```

**Then add HTTPS** once DNS for your domain points at the server:

```bash
# in .env
WEB_PORT=8080
DOMAIN=yourdomain.com
ACME_EMAIL=you@example.com
PUBLIC_BASE_URL=https://yourdomain.com

docker compose -f docker-compose.yml -f docker-compose.tls.yml up -d --build
```

Caddy requests and renews Let's Encrypt certificates automatically.

## 5. Firebase

Add your domain in **Firebase Console → Authentication → Settings →
Authorized domains**, or login will fail in production.

## How it fits together

```
Internet → Caddy :443 (TLS)
             └→ web  :80   nginx: serves React, SPA fallback
                    ├ /api/     → api:8080
                    └ /uploads/ → api:8080  (product images, QR codes)
                                     └→ db:3306  MySQL
```

The frontend calls `/api` on its **own origin**, so there is no CORS
configuration and no hostname baked into the JavaScript bundle.

## Data that must survive redeploys

Two named volumes hold all persistent state:

- `mysql-data` — the database
- `uploads` — product images, logos, shop-act certificates, QR codes

Never `docker compose down -v` on the server; `-v` deletes both.

## Backups

```bash
# Database
docker compose exec db sh -c \
  'exec mysqldump -uroot -p"$MYSQL_ROOT_PASSWORD" QRShopDb' > backup-$(date +%F).sql

# Uploaded files
docker run --rm -v qrbaseddigitalshop_uploads:/u -v $(pwd):/out alpine \
  tar czf /out/uploads-$(date +%F).tar.gz -C /u .
```

Restore the database with:

```bash
docker compose exec -T db sh -c \
  'exec mysql -uroot -p"$MYSQL_ROOT_PASSWORD" QRShopDb' < backup-YYYY-MM-DD.sql
```

## Updating

```bash
git pull && docker compose up -d --build
```

EF Core migrations run automatically at API startup.

## Notes

- **Uploads are stored on the server's disk**, which is correct here because
  a Hetzner VM has a persistent disk. This would break on ephemeral hosts
  (Heroku/Render/App Service) or if the API is ever scaled past one replica.
- **Upload size** is capped at 10 MB in the API, 12 MB in nginx and Caddy.
  Raise all three together if needed.
