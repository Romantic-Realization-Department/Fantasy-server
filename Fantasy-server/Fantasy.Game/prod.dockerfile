FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Fantasy.Game/Fantasy.Game.csproj", "Fantasy.Game/"]
COPY ["Fantasy.Common/Fantasy.Common.csproj", "Fantasy.Common/"]
RUN dotnet restore "Fantasy.Game/Fantasy.Game.csproj"
COPY . .
RUN dotnet publish "Fantasy.Game/Fantasy.Game.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Fantasy.Game.dll"]
