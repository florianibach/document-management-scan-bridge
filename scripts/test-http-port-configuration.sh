#!/bin/sh
set -eu

root=$(CDPATH= cd -- "$(dirname "$0")/.." && pwd)
. "$root/scripts/configure-http-port.sh"

dockerfile_healthcheck=$(grep '^HEALTHCHECK ' "$root/Dockerfile")
printf '%s\n' "$dockerfile_healthcheck" | grep -F ' CMD curl ' >/dev/null
if printf '%s\n' "$dockerfile_healthcheck" | grep -F 'CMD-SHELL' >/dev/null; then
    echo 'Dockerfile HEALTHCHECK must use Dockerfile shell-form CMD, not the unsupported Compose test type CMD-SHELL.' >&2
    exit 1
fi
printf '%s\n' "$dockerfile_healthcheck" | grep -F '${APPLICATION_HTTP_PORT}/health' >/dev/null

assert_valid() {
    expected=$1
    APPLICATION_HTTP_PORT=${2-}
    export APPLICATION_HTTP_PORT
    configure_http_port >/dev/null
    [ "$ASPNETCORE_HTTP_PORTS" = "$expected" ]
}
assert_invalid() {
    APPLICATION_HTTP_PORT=$1
    export APPLICATION_HTTP_PORT
    if configure_http_port >/dev/null 2>&1; then
        echo "Expected APPLICATION_HTTP_PORT '$1' to fail validation." >&2
        exit 1
    fi
}

unset APPLICATION_HTTP_PORT
configure_http_port >/dev/null
[ "$ASPNETCORE_HTTP_PORTS" = "8080" ]
assert_valid 49152 49152
assert_invalid ''
assert_invalid abc
assert_invalid 0
assert_invalid 65536
assert_invalid 999999999999999999999999999999999999
unset APPLICATION_HTTP_PORT

compose_default=$(docker compose --file "$root/compose.yaml" config)
printf '%s\n' "$compose_default" | grep -F 'APPLICATION_HTTP_PORT: "8080"' >/dev/null
printf '%s\n' "$compose_default" | grep -F 'http://127.0.0.1:8080/health' >/dev/null
compose_override=$(APPLICATION_HTTP_PORT=49152 docker compose --file "$root/compose.yaml" config)
printf '%s\n' "$compose_override" | grep -F 'APPLICATION_HTTP_PORT: "49152"' >/dev/null
printf '%s\n' "$compose_override" | grep -F 'http://127.0.0.1:49152/health' >/dev/null
if printf '%s\n' "$compose_override" | grep -Eq '^[[:space:]]+ports:'; then
    echo 'Compose must not publish ports when host networking is enabled.' >&2
    exit 1
fi

echo 'HTTP port validation and generated Compose configuration passed.'
