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

issue_cert_if_missing() {
  if [ -s "${live_dir}/fullchain.pem" ] && [ -s "${live_dir}/privkey.pem" ]; then
    echo "Existing certificate found for ${PUBLIC_HOST}."
    return 0
  fi

  echo "No existing certificate found for ${PUBLIC_HOST}. Issuing a new one..."

  # Don't let a failure crash the container and cause a restart loop that spams Let's Encrypt.
  set +e
  certbot certonly \
    --webroot -w /var/www/certbot \
    -d "${PUBLIC_HOST}" \
    --email "${LETSENCRYPT_EMAIL}" \
    --agree-tos \
    --non-interactive
  rc=$?
  set -e

  if [ $rc -ne 0 ]; then
    echo "Certificate issuance failed (exit code $rc). Backing off before retry to avoid rate limits..."
    return $rc
  fi

  echo "Certificate issued successfully for ${PUBLIC_HOST}."
  return 0
}

# Try once at startup; if it fails, back off and retry daily.
while ! issue_cert_if_missing; do
  sleep 24h
done

echo "Starting renew loop..."
trap exit TERM
while :; do
  sleep 12h & wait $!
  certbot renew --webroot -w /var/www/certbot
done

