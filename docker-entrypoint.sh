#!/bin/sh
set -eu

. /usr/local/lib/scan-bridge/configure-http-port.sh
configure_http_port

mkdir -p /app/data /app/temp
chown -R "$APP_UID:$APP_UID" /app/data /app/temp

exec gosu "$APP_UID:$APP_UID" "$@"
