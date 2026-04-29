FROM certbot/certbot

COPY entrypoint.sh /entrypoint.sh
COPY issue-cert.sh /issue-cert.sh

RUN chmod +x /entrypoint.sh /issue-cert.sh

ENTRYPOINT ["/entrypoint.sh"]

