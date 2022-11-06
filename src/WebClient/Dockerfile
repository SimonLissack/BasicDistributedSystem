FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-webclient
WORKDIR /src

copy . ./
RUN dotnet publish WebClient -c Release -o /App/published --os linux

FROM mcr.microsoft.com/dotnet/aspnet:6.0 as docker-webclient
WORKDIR /App
COPY --from=build-webclient /App/published .
ENTRYPOINT ["dotnet", "WebClient.dll"]
