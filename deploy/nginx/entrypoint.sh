#!/usr/bin/env sh
set -eu

host="${PUBLIC_HOST:-vykazy.ondrejsvorc.cz}"
live_dir="/etc/letsencrypt/live/${host}"

target="/etc/nginx/conf.d/default.conf"

if [ -s "${live_dir}/fullchain.pem" ] && [ -s "${live_dir}/privkey.pem" ]; then
  echo "Using HTTPS nginx config for ${host}"
  cp /etc/nginx/templates/vykazy.conf "${target}"
else
  echo "Using HTTP bootstrap nginx config for ${host}"
  cp /etc/nginx/templates/vykazy.bootstrap.conf "${target}"
fi

exec nginx -g "daemon off;"

