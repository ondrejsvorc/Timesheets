#!/usr/bin/env sh
set -eu

host="${PUBLIC_HOST:-vykazy.ondrejsvorc.cz}"
live_dir="/etc/letsencrypt/live/${host}"

target="/etc/nginx/conf.d/default.conf"

ensure_https_cert() {
  if [ -s "${live_dir}/fullchain.pem" ] && [ -s "${live_dir}/privkey.pem" ]; then
    return 0
  fi

  echo "No TLS certificate found for ${host}. Generating a temporary self-signed certificate..."
  mkdir -p "${live_dir}"

  # Self-signed cert: good enough to keep HTTPS working until Let's Encrypt is available again.
  openssl req -x509 -nodes -newkey rsa:2048 \
    -days 2 \
    -subj "/CN=${host}" \
    -keyout "${live_dir}/privkey.pem" \
    -out "${live_dir}/fullchain.pem" >/dev/null 2>&1
}

ensure_https_cert

if [ -s "${live_dir}/fullchain.pem" ] && [ -s "${live_dir}/privkey.pem" ]; then
  echo "Using HTTPS nginx config for ${host}"
  cp /etc/nginx/templates/vykazy.conf "${target}"
else
  echo "Using HTTP bootstrap nginx config for ${host}"
  cp /etc/nginx/templates/vykazy.bootstrap.conf "${target}"
fi

exec nginx -g "daemon off;"

