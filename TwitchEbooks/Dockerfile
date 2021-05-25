FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /source
ARG GH_USERNAME
ARG GH_PAT

# set up credprovider
ENV NUGET_CREDENTIALPROVIDER_SESSIONTOKENCACHE_ENABLED true
ENV VSS_NUGET_EXTERNAL_FEED_ENDPOINTS "{\"endpointCredentials\": [{\"endpoint\":\"https://nuget.pkg.github.com/Anybotty/index.json\", \"username\":\"${GH_USERNAME}\", \"password\":\"${GH_PAT}\"}]}"
RUN wget -qO- https://raw.githubusercontent.com/Microsoft/artifacts-credprovider/master/helpers/installcredprovider.sh | bash

# copy csprojs & sln and restore as distinct layers
COPY */*.csproj ./  
RUN for file in $(ls *.csproj); do mkdir -p ${file%.*}/ && mv $file ${file%.*}/; done
COPY *.sln .
RUN dotnet restore -s "https://nuget.pkg.github.com/Anybotty/index.json" -s "https://api.nuget.org/v3/index.json"

# copy and publish app and libraries
COPY . .
RUN dotnet publish "TwitchEbooks/TwitchEbooks.csproj" -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 80
ENTRYPOINT ["dotnet", "TwitchEbooks.dll"]