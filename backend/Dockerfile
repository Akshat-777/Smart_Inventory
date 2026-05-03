# ── Build stage ──────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution + all project files first (layer cache for restore)
COPY InventoryManagement.sln ./
COPY InventoryManagement.Api/InventoryManagement.Api.csproj              InventoryManagement.Api/
COPY InventoryManagement.Application/InventoryManagement.Application.csproj  InventoryManagement.Application/
COPY InventoryManagement.Domain/InventoryManagement.Domain.csproj        InventoryManagement.Domain/
COPY InventoryManagement.Infrastructure/InventoryManagement.Infrastructure.csproj InventoryManagement.Infrastructure/

RUN dotnet restore InventoryManagement.sln

# Copy everything else and publish
COPY . .
RUN dotnet publish InventoryManagement.Api/InventoryManagement.Api.csproj \
    -c Release -o /app/publish --no-restore

# ── Runtime stage ────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create a directory for the SQLite database
RUN mkdir -p /data

COPY --from=build /app/publish .

# Render expects port 10000 by default
EXPOSE 10000
ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "InventoryManagement.Api.dll"]
