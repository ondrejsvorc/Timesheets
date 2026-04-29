#!/usr/bin/env sh
set -eu

if [ -z "${PUBLIC_HOST:-}" ]; then
  echo "PUBLIC_HOST is required (e.g. vykazy.ondrejsvorc.cz)" >&2
  exit 1
fi

if [ -z "${LETSENCRYPT_EMAIL:-}" ]; then
  echo "LETSENCRYPT_EMAIL is required (e.g. ondrejsvorc@email.cz)" >&2
  exit 1
fi

echo "Issuing certificate for ${PUBLIC_HOST}..."

certbot certonly \
  --webroot -w /var/www/certbot \
  -d "${PUBLIC_HOST}" \
  --email "${LETSENCRYPT_EMAIL}" \
  --agree-tos \
  --non-interactive

echo "Done."

