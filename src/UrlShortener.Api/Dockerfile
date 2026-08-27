ARG DOTNET_VERSION=10.0-alpine

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build

WORKDIR /src

COPY ["src/UrlShortener.Api/UrlShortener.Api.csproj", "UrlShortener.Api/"]
RUN dotnet restore 'UrlShortener.Api/UrlShortener.Api.csproj'

COPY ["src/UrlShortener.Api/", "UrlShortener.Api/"]

RUN dotnet publish 'UrlShortener.Api/UrlShortener.Api.csproj' -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS run

USER $APP_UID

WORKDIR /app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "UrlShortener.Api.dll"]