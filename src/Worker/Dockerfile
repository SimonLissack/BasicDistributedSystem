FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-worker
WORKDIR /src

copy . ./
RUN dotnet publish Worker -c Release -o /App/published --os linux

FROM mcr.microsoft.com/dotnet/aspnet:6.0 as docker-worker
WORKDIR /App
COPY --from=build-worker /App/published .
ENTRYPOINT ["dotnet", "Worker.dll"]
