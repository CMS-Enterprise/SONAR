FROM mcr.microsoft.com/dotnet/sdk:6.0 as build

WORKDIR /test-metric-app
COPY test-metric-app.csproj .
RUN dotnet restore
COPY *.cs .
RUN dotnet build -c Release

FROM mcr.microsoft.com/dotnet/aspnet:6.0

WORKDIR /test-metric-app
COPY --from=build /test-metric-app/bin/Release/net6.0/* .
EXPOSE 2020
ENTRYPOINT /test-metric-app/test-metric-app.exe
