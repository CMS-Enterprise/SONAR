FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine3.17 as build

WORKDIR /http-metric-test-app
COPY http-metric-test-app.csproj .
RUN dotnet restore
COPY . .
RUN dotnet build -c Release

FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine3.17

WORKDIR /http-metric-test-app
COPY --from=build /http-metric-test-app/bin/Release/net7.0/* .
EXPOSE 8082
ENTRYPOINT /http-metric-test-app/http-metric-test-app.exe
