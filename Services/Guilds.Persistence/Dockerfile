FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Services/Guilds.Persistence/Guilds.Persistence.csproj", "./Services/Guilds.Persistence/"]
COPY ["Modules/Guilds.Infrastructure/Guilds.Infrastructure.csproj", "./Modules/Guilds.Infrastructure/"]
COPY ["Modules/Shared.Core/Shared.Core.csproj", "./Modules/Shared.Core/"]
COPY ["Modules/Shared.Guilds/Shared.Guilds.csproj", "./Modules/Shared.Guilds/"]
COPY ["Modules/Guilds.Mongo/Guilds.Mongo.csproj", "./Modules/Guilds.Mongo/"]
COPY ["Modules/Guilds.Domain/Guilds.Domain.csproj", "./Modules/Guilds.Domain/"]
COPY ["Modules/Shared.Mongo/Shared.Mongo.csproj", "./Modules/Shared.Mongo/"]
COPY ["Modules/Guilds.Api/Guilds.Api.csproj", "./Modules/Guilds.Api/"]
RUN dotnet restore "Services/Guilds.Persistence/Guilds.Persistence.csproj"
COPY . .
WORKDIR "/src/Services/Guilds.Persistence"

FROM build AS publish
RUN dotnet publish "Guilds.Persistence.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Guilds.Persistence.dll"]
