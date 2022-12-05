FROM mcr.microsoft.com/dotnet/sdk:6.0 as build

WORKDIR /http-metric-test-app
COPY http-metric-test-app.csproj .
RUN dotnet restore
COPY . .
RUN dotnet build -c Release

FROM mcr.microsoft.com/dotnet/aspnet:6.0

WORKDIR /http-metric-test-app
COPY --from=build /http-metric-test-app/bin/Release/net6.0/* .
EXPOSE 8082
ENTRYPOINT /http-metric-test-app/http-metric-test-app.exe
