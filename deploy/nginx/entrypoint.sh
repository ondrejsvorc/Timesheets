#!/usr/bin/env sh
set -eu

host="${PUBLIC_HOST:-vykazy.ondrejsvorc.cz}"
live_dir="/etc/letsencrypt/live/${host}"
self_dir="/etc/nginx/self-signed/${host}"

target="/etc/nginx/conf.d/default.conf"

has_le_cert() {
  [ -s "${live_dir}/fullchain.pem" ] && [ -s "${live_dir}/privkey.pem" ]
}

ensure_self_signed() {
  if [ -s "${self_dir}/fullchain.pem" ] && [ -s "${self_dir}/privkey.pem" ]; then
    return 0
  fi

  echo "Generating a temporary self-signed certificate for ${host}..."
  mkdir -p "${self_dir}"
  openssl req -x509 -nodes -newkey rsa:2048 \
    -days 2 \
    -subj "/CN=${host}" \
    -keyout "${self_dir}/privkey.pem" \
    -out "${self_dir}/fullchain.pem" >/dev/null 2>&1
}

use_le_config() {
  echo "Using Let's Encrypt certificate for ${host}"
  cp /etc/nginx/templates/vykazy.conf "${target}"
}

use_self_signed_config() {
  ensure_self_signed
  echo "Using self-signed certificate for ${host} (waiting for Let's Encrypt)"
  cp /etc/nginx/templates/vykazy.selfsigned.conf "${target}"
}

if has_le_cert; then
  use_le_config
else
  # Start HTTPS immediately, but keep ACME HTTP challenge on port 80 working.
  # Self-signed cert lives outside /etc/letsencrypt so it doesn't block certbot.
  use_self_signed_config
fi

# Watch for LE cert to appear and upgrade nginx config without container restart.
if ! has_le_cert; then
  (
    echo "Waiting for Let's Encrypt certificate for ${host}..."
    while :; do
      if has_le_cert; then
        break
      fi
      sleep 10
    done
    use_le_config
    nginx -s reload || true
  ) &
fi

exec nginx -g "daemon off;"

