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

live_dir="/etc/letsencrypt/live/${PUBLIC_HOST}"

if [ ! -s "${live_dir}/fullchain.pem" ] || [ ! -s "${live_dir}/privkey.pem" ]; then
  echo "No existing certificate found for ${PUBLIC_HOST}. Issuing a new one..."
  certbot certonly \
    --webroot -w /var/www/certbot \
    -d "${PUBLIC_HOST}" \
    --email "${LETSENCRYPT_EMAIL}" \
    --agree-tos \
    --non-interactive
else
  echo "Existing certificate found for ${PUBLIC_HOST}."
fi

echo "Starting renew loop..."
trap exit TERM
while :; do
  sleep 12h & wait $!
  certbot renew --webroot -w /var/www/certbot
done

