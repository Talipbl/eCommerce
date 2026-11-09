# Derleme aşaması: SDK image'ında restore ve publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Önce sadece proje dosyaları kopyalanır; kod değişince restore katmanı cache'ten gelir
COPY Directory.Build.props .editorconfig ./
COPY src/ECommerce.API/ECommerce.API.csproj                     src/ECommerce.API/
COPY src/ECommerce.Application/ECommerce.Application.csproj     src/ECommerce.Application/
COPY src/ECommerce.Core/ECommerce.Core.csproj                   src/ECommerce.Core/
COPY src/ECommerce.Domain/ECommerce.Domain.csproj               src/ECommerce.Domain/
COPY src/ECommerce.Persistence/ECommerce.Persistence.csproj     src/ECommerce.Persistence/
COPY marketplaces/Trendyol.Client/Trendyol.Client.csproj        marketplaces/Trendyol.Client/
RUN dotnet restore src/ECommerce.API/ECommerce.API.csproj

COPY src/ src/
COPY marketplaces/Trendyol.Client/ marketplaces/Trendyol.Client/
RUN dotnet publish src/ECommerce.API/ECommerce.API.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Migration bundle aşaması: tüm migration'lar tek bir çalıştırılabilir dosyada
FROM build AS migrations-build
COPY dotnet-tools.json ./
RUN dotnet tool restore
RUN dotnet ef migrations bundle \
      --project src/ECommerce.Persistence/ECommerce.Persistence.csproj \
      --startup-project src/ECommerce.Persistence/ECommerce.Persistence.csproj \
      --configuration Release \
      --output /app/efbundle

# Migrator image'ı: bundle'ı çalıştırıp kapanır
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS migrator
WORKDIR /app
COPY --from=migrations-build /app/efbundle .
USER $APP_UID
ENTRYPOINT ["./efbundle"]

# Çalışma aşaması: sadece ASP.NET runtime; hedef verilmeyen build bu son aşamayı üretir
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /app
RUN mkdir -p /app/media && chown "$APP_UID" /app/media
COPY --from=build /app/publish .

USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD curl -fsS http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "ECommerce.API.dll"]