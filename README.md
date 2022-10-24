# SONAR

SONAR is an API endpoint and dashboard UI for monitoring the health of both BatCAVE core services and BatCAVE deployed applications. SONAR actively monitors and records the health of services running in BatCAVE using a combination of Prometheus queries (for checks of system and application metrics), Loki queries (for checks of log messages), HTTP/S status code checks, DNS checks, and TLS certificate checks.

## Prerequisites

Install the [dotnet SDK 6.x](https://dotnet.microsoft.com/en-us/download) (note: in some cases just the dotnet CLI will suffice, but for development it is usually preferable to have the SDK installed).

On MacOS you can also install the .Net SDK via Homebrew:

```
brew install dotnet-sdk
```

## Build (Optional)

From the root of the repository run:

```
dotnet build
```

By default this will build the Debug version of all components. To generate a Release build (which is optimized and has less debugging information) use:

```
dotnet build -c Release
```

## Run the Agent or API

From the root of the repository run:

```
dotnet run --project sonar-agent
```
or
```
dotnet run --project sonar-api
```

The API will now be listening for connections on `localhost:8081`

Alternatively, from the `sonar-api` or the `sonar-agent` folder you can just use.

```
dotnet run
```

### Live Reloading

To run the API with live reloading while doing active development use:

```
dotnet watch run --project sonar-api -- serve
```

_Note: When using live reloading, code changes to the application start up code will not automatically take effect, so if you change code invoked during application startup you will still need to force reload with `Ctrl+R`_

### Launch Profiles

Launch profiles configure how dotnet starts the project when using `dotnet run` by default the "Development" profile and environment settings will be used. To run with the "Production" environment settings and a Release build you can use:

```
dotnet run --project sonar-api --launch-profile production
```

Additional launch profiles may be configured in [`launcSettings.json`](sonar-api/Properties/launchSettings.json)

## Database Connectivity

The SONAR API requires connectivity to a PostgreSQL database. There is a [`docker-compose.yml`](docker-compose.yml) file that can be used to run PostgreSQL 14 for development purposes.

### Database Configuration

Configuration settings for the SONAR API database connectivity are defined in [`DatabaseConfiguration.cs`](sonar-api/Configuration/DatabaseConfiguration.cs). Values for the configuration settings defined in this file can be supplied via the `appsettings.{environmnet}.json` file for the applicable environment, environment variables, or command line arguments.

```shell
# Via environment variable:
Database__Port=1234 dotnet run --project sonar-api

# Via command line argument:
dotnet run --project sonar-api -- serve --Database:Port 1234
```

For more information see the ASP.Net [Core documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-6.0).

### Database Initialization

To initialize the the database without starting SONAR API run:

```
dotnet run --project sonar-api -- init

# If you want to force the database to be dropped and recreated:
# dotnet run --project sonar-api -- init --force
```

### Database Query Logging

To run the SONAR API with SQL query logging enabled, run:

```
dotnet run --project sonar-api -- serve --Database:DbLogging true
```

This should only be used in development environments, but it can be helpful for analyzing Linq query generation and performance.

## Versioning

### Assembly & Package Versioning

All components in SONAR are versioned together, following semantic versioning practices, via the `VersionPrevix` property in [shared.props](shared.props). When a new version is released this file should be updated and the commit tagged with just the version number (i.e. `2.0.0`). Test releases may be tagged with a version suffix such as `-beta1` using the `VersionSuffix` property:

```
dotnet build /p:VersionSuffix=beta1
```

### API Versioning

Versioned API routes should always start with `/api/v{major-version-number}/...` so that in the event that we need to make a breaking change to an existing API, such as introducing a new required parameter or removing or renaming a previously returned property in a JSON body, we can introduce the new behavior at a new URL while preserving the existing behavior at the old endpoint.

It is imperative that all updates to the SONAR API are backward compatible with both earlier versions of the SONAR Agent, and external API consumers.
