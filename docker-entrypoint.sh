#!/bin/sh
set -eu

mkdir -p /app/data /app/temp
chown -R "$APP_UID:$APP_UID" /app/data /app/temp

exec gosu "$APP_UID:$APP_UID" "$@"
