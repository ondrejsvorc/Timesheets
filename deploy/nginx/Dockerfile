FROM nginx:alpine

COPY vykazy.bootstrap.conf /etc/nginx/templates/vykazy.bootstrap.conf
COPY vykazy.conf /etc/nginx/templates/vykazy.conf
COPY vykazy.selfsigned.conf /etc/nginx/templates/vykazy.selfsigned.conf
COPY security-headers.conf /etc/nginx/security-headers.conf
COPY csp.conf /etc/nginx/csp.conf
COPY entrypoint.sh /entrypoint.sh

RUN apk add --no-cache openssl \
  && sed -i 's/\r$//' /entrypoint.sh \
  && chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]

