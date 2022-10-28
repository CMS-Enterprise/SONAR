# SONAR API

## Launch Profiles

Launch profiles configure how dotnet starts the project when using `dotnet run` by default the "Development" profile and environment settings will be used. To run with the "Production" environment settings and a Release build you can use:

```
dotnet run --launch-profile production
```

Additional shared launch profiles may be configured in [`launcSettings.json`](sonar-api/Properties/launchSettings.json)

## Live Reloading

To run the API with live reloading while doing active development use:

```
dotnet watch run -- serve
```

_Note: When using live reloading, code changes to the application start up code will not automatically take effect, so if you change code invoked during application startup you will still need to force reload with `Ctrl+R`_

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
dotnet run -- init

# If you want to force the database to be dropped and recreated:
# dotnet run -- init --force
```

### Database Query Logging

To run the SONAR API with SQL query logging enabled, run:

```
dotnet run -- serve --Database:DbLogging true
```

This should only be used in development environments, but it can be helpful for analyzing Linq query generation and performance.

## Prometheus Protocol Buffers Types

The types defined in the `Protobuf/` folder are automatically generated using the `protoc` tool that is part of [Google.Protobuf.Tools package](https://github.com/protocolbuffers/protobuf/tree/main/csharp#usage). To regenerate these source files follow these steps:

```shell
# Initialize or update git submodules:
git submodule update --init

# Run the code generation msbuild task:
dotnet msbuild -target:generate_prometheus_types
```
