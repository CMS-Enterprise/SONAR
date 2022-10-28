# SONAR

SONAR is an API endpoint and dashboard UI for monitoring the health of both BatCAVE core services and BatCAVE deployed applications. SONAR actively monitors and records the health of services running in BatCAVE using a combination of Prometheus queries (for checks of system and application metrics), Loki queries (for checks of log messages), HTTP/S status code checks, DNS checks, and TLS certificate checks.

## Components

 * [sonar-api](sonar-api/)
 * [sonar-agent](sonar-agent/) - An agent process that performs monitoring tasks within an environment and pushes data to the API.
 * [sonar-core](sonar-core/) - A shared library with data model types used by both components and general utility classes.

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

For more information on configuring and running the API see [the sonar-api README](sonar-api/README.md).

## Versioning

### Assembly & Package Versioning

All components in SONAR are versioned together, following semantic versioning practices, via the `VersionPrevix` property in [shared.props](shared.props). When a new version is released this file should be updated and the commit tagged with just the version number (i.e. `2.0.0`). Test releases may be tagged with a version suffix such as `-beta1` using the `VersionSuffix` property:

```
dotnet build /p:VersionSuffix=beta1
```

### API Versioning

Versioned API routes should always start with `/api/v{major-version-number}/...` so that in the event that we need to make a breaking change to an existing API, such as introducing a new required parameter or removing or renaming a previously returned property in a JSON body, we can introduce the new behavior at a new URL while preserving the existing behavior at the old endpoint.

It is imperative that all updates to the SONAR API are backward compatible with both earlier versions of the SONAR Agent, and external API consumers.
