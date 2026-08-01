FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY PtcHub.API/PtcHub.API.csproj PtcHub.API/
RUN dotnet restore PtcHub.API/PtcHub.API.csproj
COPY . .
WORKDIR /src/PtcHub.API
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENTRYPOINT ["dotnet", "PtcHub.API.dll"]
