#!/bin/sh

# Validates the operator-facing host-network listener before ASP.NET Core starts.
configure_http_port() {
    port=${APPLICATION_HTTP_PORT-8080}

    case "$port" in
        *[!0-9]*|'')
            echo "Invalid APPLICATION_HTTP_PORT '$port': expected an integer from 1 to 65535." >&2
            return 64
            ;;
    esac

    if ! port=$(awk -v value="$port" 'BEGIN { if (value < 1 || value > 65535) exit 1; printf "%d", value }'); then
        echo "Invalid APPLICATION_HTTP_PORT '$port': expected an integer from 1 to 65535." >&2
        return 64
    fi

    port_hex=$(printf '%04X' "$port")
    for sockets in /proc/net/tcp /proc/net/tcp6; do
        if [ -r "$sockets" ] && awk -v port=":$port_hex" 'NR > 1 && substr($2, length($2) - 4) == port && $4 == "0A" { found=1 } END { exit !found }' "$sockets"; then
            echo "APPLICATION_HTTP_PORT $port is unavailable: another process is already listening on that TCP port. Choose a free port and recreate the Compose service." >&2
            return 69
        fi
    done

    APPLICATION_HTTP_PORT=$port
    ASPNETCORE_HTTP_PORTS=$port
    export APPLICATION_HTTP_PORT ASPNETCORE_HTTP_PORTS
    echo "Configuring Scan Bridge to listen on HTTP port $port."
}
