FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /source

# copy csprojs & sln and restore as distinct layers
COPY */*.csproj ./  
RUN for file in $(ls *.csproj); do mkdir -p ${file%.*}/ && mv $file ${file%.*}/; done
COPY *.sln .
RUN dotnet restore

# copy and publish app and libraries
COPY . .
RUN dotnet publish "TwitchEbooks/TwitchEbooks.csproj" -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine
WORKDIR /app
COPY --from=build /app .
EXPOSE 80
ENTRYPOINT ["dotnet", "TwitchEbooks.dll"]