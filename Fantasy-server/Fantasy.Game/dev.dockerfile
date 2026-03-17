FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /src
COPY ["Fantasy.Game/Fantasy.Game.csproj", "Fantasy.Game/"]
COPY ["Fantasy.Common/Fantasy.Common.csproj", "Fantasy.Common/"]
RUN dotnet restore "Fantasy.Game/Fantasy.Game.csproj"
COPY . .
WORKDIR /src/Fantasy.Game
ENV ASPNETCORE_ENVIRONMENT=Development
ENTRYPOINT ["dotnet", "watch", "run", "--no-launch-profile"]
