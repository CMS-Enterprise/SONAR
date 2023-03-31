FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine3.17 as build

WORKDIR /test-metric-app
COPY test-metric-app.csproj .

RUN dotnet restore
COPY *.cs .
COPY appsettings.json .
COPY appsettings.*.json .
RUN dotnet build -c Release

FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine3.17

WORKDIR /test-metric-app
COPY --from=build /test-metric-app/bin/Release/net7.0/* .
EXPOSE 2020
ENTRYPOINT /test-metric-app/test-metric-app.exe
