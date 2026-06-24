FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /src
COPY . .
WORKDIR /src/Fantasy.Server
RUN dotnet restore
ENTRYPOINT ["dotnet", "watch", "run", "--urls", "http://+:8080"]
