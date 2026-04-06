FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY api/Skoleplanen.Api/Skoleplanen.Api.csproj api/Skoleplanen.Api/
COPY infrastructure/aspire/Skoleplanen.ServiceDefaults/Skoleplanen.ServiceDefaults.csproj infrastructure/aspire/Skoleplanen.ServiceDefaults/
RUN dotnet restore api/Skoleplanen.Api/Skoleplanen.Api.csproj

COPY api/ api/
COPY infrastructure/aspire/Skoleplanen.ServiceDefaults/ infrastructure/aspire/Skoleplanen.ServiceDefaults/
RUN dotnet publish api/Skoleplanen.Api/Skoleplanen.Api.csproj \
    --no-restore \
    --configuration Release \
    --output /app/publish \
    -p:CI=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Skoleplanen.Api.dll"]
