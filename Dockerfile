ARG DOTNET_SDK_VERSION=10.0.302
ARG DOTNET_RUNTIME_VERSION=10.0.7

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION}-noble AS build
WORKDIR /source
COPY . .
RUN dotnet restore PaperlessScanBridge.slnx --locked-mode \
 && dotnet publish src/PaperlessScanBridge.Web/PaperlessScanBridge.Web.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_RUNTIME_VERSION}-noble AS runtime
RUN adduser --disabled-password --gecos "" --uid 10001 bridge \
 && mkdir -p /app/data /app/temp \
 && chown -R bridge:bridge /app
WORKDIR /app
COPY --from=build --chown=bridge:bridge /app .
USER bridge
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "PaperlessScanBridge.Web.dll"]
