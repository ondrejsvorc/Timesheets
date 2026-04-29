FROM nginx:alpine

COPY vykazy.bootstrap.conf /etc/nginx/templates/vykazy.bootstrap.conf
COPY vykazy.conf /etc/nginx/templates/vykazy.conf
COPY entrypoint.sh /entrypoint.sh

RUN chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]

