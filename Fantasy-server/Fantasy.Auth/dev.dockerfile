FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /src
COPY ["Fantasy.Auth/Fantasy.Auth.csproj", "Fantasy.Auth/"]
COPY ["Fantasy.Common/Fantasy.Common.csproj", "Fantasy.Common/"]
RUN dotnet restore "Fantasy.Auth/Fantasy.Auth.csproj"
COPY . .
WORKDIR /src/Fantasy.Auth
ENV ASPNETCORE_ENVIRONMENT=Development
ENTRYPOINT ["dotnet", "watch", "run", "--no-launch-profile"]
