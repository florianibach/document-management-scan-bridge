ARG DOTNET_SDK_VERSION=10.0.302
ARG DOTNET_RUNTIME_VERSION=10.0.7
ARG GIT_COMMIT=unknown

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION}-noble AS build
WORKDIR /source
COPY . .
RUN dotnet restore PaperlessScanBridge.slnx --locked-mode \
 && dotnet publish src/PaperlessScanBridge.Web/PaperlessScanBridge.Web.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_RUNTIME_VERSION}-noble AS runtime
ARG GIT_COMMIT
WORKDIR /app
RUN apt-get update \
 && apt-get install --yes --no-install-recommends curl gosu sane-utils sane-airscan \
 && rm -rf /var/lib/apt/lists/* \
 && mkdir -p /app/data/sane.d /app/temp \
 && ln -sfn /app/data/sane.d/airscan.conf /etc/sane.d/airscan.conf \
 && chown -R "$APP_UID:$APP_UID" /app
COPY --from=build --chown=$APP_UID:$APP_UID /app .
COPY --chmod=755 docker-entrypoint.sh /usr/local/bin/docker-entrypoint.sh
COPY --chmod=755 scripts/configure-http-port.sh /usr/local/lib/scan-bridge/configure-http-port.sh
ENV APPLICATION_HTTP_PORT=8080
ENV Build__Commit=$GIT_COMMIT
LABEL org.opencontainers.image.revision=$GIT_COMMIT
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 CMD curl --fail --silent --show-error "http://127.0.0.1:${APPLICATION_HTTP_PORT}/health" || exit 1
ENTRYPOINT ["docker-entrypoint.sh"]
CMD ["dotnet", "PaperlessScanBridge.Web.dll"]
