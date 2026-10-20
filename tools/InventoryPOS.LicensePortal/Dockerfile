FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/InventoryPOS.Domain/InventoryPOS.Domain.csproj src/InventoryPOS.Domain/
COPY src/InventoryPOS.Application/InventoryPOS.Application.csproj src/InventoryPOS.Application/
COPY src/InventoryPOS.Licensing.Issuer/InventoryPOS.Licensing.Issuer.csproj src/InventoryPOS.Licensing.Issuer/
COPY tools/InventoryPOS.LicensePortal/InventoryPOS.LicensePortal.csproj tools/InventoryPOS.LicensePortal/

RUN dotnet restore tools/InventoryPOS.LicensePortal/InventoryPOS.LicensePortal.csproj

COPY src/InventoryPOS.Domain/ src/InventoryPOS.Domain/
COPY src/InventoryPOS.Application/ src/InventoryPOS.Application/
COPY src/InventoryPOS.Licensing.Issuer/ src/InventoryPOS.Licensing.Issuer/
COPY tools/InventoryPOS.LicensePortal/ tools/InventoryPOS.LicensePortal/

RUN dotnet publish tools/InventoryPOS.LicensePortal/InventoryPOS.LicensePortal.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=build /app/publish .
RUN mkdir -p /data/keys /data/issued-licenses /data/releases
ENTRYPOINT ["dotnet", "InventoryPOS.LicensePortal.dll"]
