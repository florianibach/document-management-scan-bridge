FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY . .
RUN dotnet restore PaperlessScanBridge.slnx --locked-mode \
 && dotnet publish src/PaperlessScanBridge.Web/PaperlessScanBridge.Web.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN adduser --disabled-password --gecos "" --uid 10001 bridge \
 && mkdir -p /app/data /app/temp \
 && chown -R bridge:bridge /app
WORKDIR /app
COPY --from=build --chown=bridge:bridge /app .
USER bridge
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "PaperlessScanBridge.Web.dll"]
