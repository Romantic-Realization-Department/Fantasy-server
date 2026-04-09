FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Fantasy-server.sln .
COPY Fantasy.Server/Fantasy.Server.csproj Fantasy.Server/
COPY Fantasy.Test/Fantasy.Test.csproj Fantasy.Test/
RUN dotnet restore Fantasy-server.sln
COPY . .
RUN dotnet publish Fantasy.Server/Fantasy.Server.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Fantasy.Server.dll"]
