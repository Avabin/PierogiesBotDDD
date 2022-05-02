FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Services/Guilds.WebApi/Guilds.WebApi.csproj", "./Services/Guilds.WebApi/"]
COPY ["Modules/Shared.Guilds/Shared.Guilds.csproj", "./Modules/Shared.Guilds/"]
COPY ["Modules/Shared.Core/Shared.Core.csproj", "./Modules/Shared.Core/"]
COPY ["Modules/Guilds.Infrastructure/Guilds.Infrastructure.csproj", "./Modules/Guilds.Infrastructure/"]
RUN dotnet restore "Services/Guilds.WebApi/Guilds.WebApi.csproj"
COPY . .
WORKDIR "/src/Services/Guilds.WebApi"

FROM build AS publish
RUN dotnet publish "Guilds.WebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Guilds.WebApi.dll"]
