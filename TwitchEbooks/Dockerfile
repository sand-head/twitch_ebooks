#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
ARG PAT
RUN wget -qO- https://raw.githubusercontent.com/Microsoft/artifacts-credprovider/master/helpers/installcredprovider.sh | bash
ENV NUGET_CREDENTIALPROVIDER_SESSIONTOKENCACHE_ENABLED true
ENV VSS_NUGET_EXTERNAL_FEED_ENDPOINTS "{\"endpointCredentials\": [{\"endpoint\":\"https://pkgs.dev.azure.com/sand-head/_packaging/Justin-Has-a-NuGet-Feed/nuget/v3/index.json\", \"password\":\"${PAT}\"}]}"
COPY ["TwitchEbooks.Database/TwitchEbooks.Database.csproj", "TwitchEbooks.Database/"]
COPY ["TwitchEbooks/TwitchEbooks.csproj", "TwitchEbooks/"]
RUN dotnet restore -s "https://pkgs.dev.azure.com/sand-head/_packaging/Justin-Has-a-NuGet-Feed/nuget/v3/index.json" -s "https://api.nuget.org/v3/index.json" "TwitchEbooks/TwitchEbooks.csproj"
COPY . .
WORKDIR "/src/TwitchEbooks"
RUN dotnet build "TwitchEbooks.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TwitchEbooks.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
LABEL traefik.enable=true
LABEL traefik.http.routers.twitch-ebooks.rule="Host(`robo.schweigert.dev`)"
LABEL traefik.http.routers.twitch-ebooks.tls=true
LABEL traefik.http.routers.twitch-ebooks.tls.certresolver="le"
LABEL traefik.http.routers.twitch-ebooks.tls.domains[0].main="robo.schweigert.dev"
ENTRYPOINT ["dotnet", "TwitchEbooks.dll"]