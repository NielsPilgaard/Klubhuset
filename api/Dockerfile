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
    -p:CI=true # Set CI=true to disable OpenAPI generation during publish, since it modifies source files

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Skoleplanen.Api.dll"]
