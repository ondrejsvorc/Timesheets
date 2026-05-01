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
  out_file="$(mktemp)"
  set +e
  certbot certonly \
    --webroot -w /var/www/certbot \
    -d "${PUBLIC_HOST}" \
    --email "${LETSENCRYPT_EMAIL}" \
    --agree-tos \
    --non-interactive \
    2>&1 | tee "${out_file}"
  rc=$?
  set -e

  if [ $rc -ne 0 ]; then
    echo "Certificate issuance failed (exit code $rc). Backing off before retry to avoid rate limits..."
    # If LE returns "retry after <timestamp> UTC", sleep until that moment (plus small buffer).
    retry_after_utc="$(grep -Eo 'retry after [0-9]{4}-[0-9]{2}-[0-9]{2} [0-9]{2}:[0-9]{2}:[0-9]{2} UTC' "${out_file}" | head -n 1 | sed 's/^retry after //')"
    rm -f "${out_file}" || true
    if [ -n "${retry_after_utc}" ]; then
      seconds="$(python -c "import datetime as d,sys; s=sys.argv[1].replace(' UTC',''); t=d.datetime.strptime(s,'%Y-%m-%d %H:%M:%S').replace(tzinfo=d.timezone.utc); now=d.datetime.now(d.timezone.utc); print(max(0,int((t-now).total_seconds())+60))" "${retry_after_utc}" 2>/dev/null || echo 86400)"
      echo "Rate limit window. Sleeping for ${seconds}s until after ${retry_after_utc}..."
      sleep "${seconds}s"
      return $rc
    fi
    return $rc
  fi

  rm -f "${out_file}" || true
  echo "Certificate issued successfully for ${PUBLIC_HOST}."
  return 0
}

# Try once at startup; if it fails, back off (smart if rate-limited).
while ! issue_cert_if_missing; do
  sleep 24h
done

echo "Starting renew loop..."
trap exit TERM
while :; do
  sleep 12h & wait $!
  certbot renew --webroot -w /var/www/certbot
done

