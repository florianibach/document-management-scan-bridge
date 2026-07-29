ARG DOTNET_SDK_VERSION=10.0.302
ARG DOTNET_RUNTIME_VERSION=10.0.7

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION}-noble AS build
WORKDIR /source
COPY . .
RUN dotnet restore PaperlessScanBridge.slnx --locked-mode \
 && dotnet publish src/PaperlessScanBridge.Web/PaperlessScanBridge.Web.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_RUNTIME_VERSION}-noble AS runtime
WORKDIR /app
RUN apt-get update \
 && apt-get install --yes --no-install-recommends sane-utils sane-airscan \
 && rm -rf /var/lib/apt/lists/* \
 && mkdir -p /app/data /app/temp \
 && chown -R "$APP_UID:$APP_UID" /app
COPY --from=build --chown=$APP_UID:$APP_UID /app .
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "PaperlessScanBridge.Web.dll"]
