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

Configuration settings for the SONAR API database connectivity are defined in [`DatabaseConfiguration.cs`](sonar-api/Configuration/DatabaseConfiguration.cs). Values for the configuration settings defined in this file can be supplied via the `appsettings.{environment}.json` file for the applicable environment, environment variables, or command line arguments.

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

## Authentication

All API endpoints that create, modify, or delete resources require authentication.

### API Key Authentication

Authentication can be performed using an API Key which is specified via the `ApiKey` HTTP Header. SONAR API can be configured to use a default Admin API key, either using the `ApiKey` environment variable, or the `"ApiKey"` entry in an `appsettings` file. Additional API keys can also be created via the [ApiController](./Controllers/ApiKeyController.cs).

Api Keys can be scoped to a particular Environment or a particular Tenant, which limits what operations they can perform.

## Testing with Postman

There is a [Postman](https://www.postman.com/) collection included in this repository, [`/docs/sonar.postman_collection.json`](../docs/sonar.postman_collection.json), which can be used to test the SONAR API. The requests in this collection are parameterized with `{{VARIABLE}}` templates which are populated based on values specified in your Postman Environment. A [sample environment](../docs/sonar-localhost.postman_environment.json) for testing on localhost has also been included. This collection also has authentication settings so that you can globally configure the API Key to use for testing, and all the requests that require authentication will inherit this setting.

![Screenshot of Postman's Authentication Settings for the Collection](../docs/postman-collection-auth-settings.png)

![Screenshot of the sample localhost Environment for Postman](../docs/postman-environment.png)

When you are executing a request it is important to have to correct environment selected:

![Screenshot of the Postman environment selection dropdown](../docs/postman-select-environment.png)
