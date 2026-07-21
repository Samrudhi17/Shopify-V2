# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (cached unless the csproj changes)
COPY QRShop.API.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/publish .

# Uploads live here; docker-compose mounts a named volume over it so files
# survive redeploys.
RUN mkdir -p /app/wwwroot/uploads/products \
             /app/wwwroot/uploads/logos \
             /app/wwwroot/uploads/certificates \
             /app/wwwroot/uploads/qrcodes

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "QRShop.API.dll"]
