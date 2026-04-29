## Deploy to a VPS (Hostinger) with Docker + nginx

This repo is set up to serve:

- `https://vykazy.ondrejsvorc.cz/` (frontend)
- `https://vykazy.ondrejsvorc.cz/api/*` (backend)

### Prereqs

- DNS A record: `vykazy.ondrejsvorc.cz` → your VPS public IP
- VPS has Docker + docker compose
- Ports 80/443 open

### 1) Create `.env` on the VPS (do not commit)

Use `.env.example` as a template. Minimum:

- `POSTGRES_PASSWORD`
- `PUBLIC_HOST` (e.g. `vykazy.ondrejsvorc.cz`)
- `LETSENCRYPT_EMAIL`

### 2) Boot containers in HTTP mode (for initial cert issuance)

The production compose starts nginx in **HTTP-only** mode using `deploy/nginx/vykazy.bootstrap.conf`.

```bash
docker compose -f docker-compose.prod.yml up -d --build
```

At this point `http://vykazy.ondrejsvorc.cz` should load (no TLS yet).

### 3) Issue the first Let's Encrypt certificate

Run certbot in the existing container context:

```bash
docker compose -f docker-compose.prod.yml run --rm \
  -e PUBLIC_HOST="$PUBLIC_HOST" \
  -e LETSENCRYPT_EMAIL="$LETSENCRYPT_EMAIL" \
  --entrypoint /bin/sh \
  timesheets.certbot \
  -c "sh /scripts/issue-cert.sh"
```

If the path above is awkward in your environment, you can instead run:

```bash
docker compose -f docker-compose.prod.yml run --rm --entrypoint certbot timesheets.certbot \
  certonly --webroot -w /var/www/certbot -d "$PUBLIC_HOST" --email "$LETSENCRYPT_EMAIL" \
  --agree-tos --non-interactive
```

### 4) Switch nginx to HTTPS config

Update the nginx bind mount in `docker-compose.prod.yml` from:

- `deploy/nginx/vykazy.bootstrap.conf` → `deploy/nginx/vykazy.conf`

Then restart nginx:

```bash
docker compose -f docker-compose.prod.yml up -d
```

### 5) Ongoing renewals

The `timesheets.certbot` container runs `certbot renew` periodically.

