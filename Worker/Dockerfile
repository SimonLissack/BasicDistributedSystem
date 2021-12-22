FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-worker
WORKDIR /App

copy . ./
RUN dotnet publish -c Release -o published

FROM mcr.microsoft.com/dotnet/aspnet:6.0 as docker-worker
WORKDIR /App
COPY --from=build-worker /App/published .
ENTRYPOINT ["dotnet", "Worker.dll"]
