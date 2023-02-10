[![pipeline status](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/badges/main/pipeline.svg)](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commits/main) | Sandbox: [![Sandbox Deployment Status](https://argocd.batcave-impl.internal.cms.gov/api/badge?name=sonar&revision=true)](https://argocd.batcave-impl.internal.cms.gov/applications/sonar)

# SONAR

SONAR is an API endpoint and dashboard UI for monitoring the health of both BatCAVE core services and BatCAVE deployed applications. SONAR actively monitors and records the health of services running in BatCAVE using a combination of Prometheus queries (for checks of system and application metrics), Loki queries (for checks of log messages), HTTP/S status code checks, DNS checks, and TLS certificate checks.

## Components

 * [sonar-api](sonar-api/)
 * [sonar-agent](sonar-agent/) - An agent process that performs monitoring tasks within an environment and pushes data to the API.
 * [sonar-core](sonar-core/) - A shared library with data model types used by both components and general utility classes.

## Prerequisites

### dotnet SDK

Install the [dotnet SDK 6.x](https://dotnet.microsoft.com/en-us/download) (note: in some cases just the dotnet CLI will suffice, but for development it is usually preferable to have the SDK installed).

On MacOS you can also install the .Net SDK via Homebrew:

```
brew install dotnet-sdk
```

### snappy

The snappy compression library is a prerequisite for the SONAR API (use for Prometheus integration). To install this library on MacOS:

```
brew install snappy
```

On linux distros installation steps may vary, but something like:

```
apt-get install libsnappy1v5 libsnappy-dev
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

## Run the API

From the `sonar-api` folder:

```
dotnet run
```

or

```
dotnet run -- serve
```

The API will now be listening for connections on `localhost:8081`
For more information on configuring and running the API see [the sonar-api README](sonar-api/README.md).

## Run the Agent

From the `sonar-agent` folder:

```
dotnet run ./service-config.json
```

## Dependencies & Docker Compose

The SONAR API and Agent have two critical dependencies: PostgreSQL and Prometheus. Both of these can be run via Docker Compose using the `docker-compose.yml` file in the root of the repo:

```
docker-compose up -d prometheus postgresql loki grafana
```

There is also an example application that generates Prometheus metrics that can be used when testing SONAR health checks. To run this application in in Docker as well run:

```
docker-compose up -d test-metric-app
```

To build Docker images via Docker Compose, run the following command:

```
docker-compose build
```

To selectively run services via Docker Compose, run the following command:

```
docker-compose up sonar-api sonar-agent test-metric-app
```
## Versioning

### Assembly & Package Versioning

All components in SONAR are versioned together, following semantic versioning practices, via the `VersionPrevix` property in [shared.props](shared.props). When a new version is released this file should be updated and the commit tagged with just the version number (i.e. `2.0.0`). Test releases may be tagged with a version suffix such as `-beta1` using the `VersionSuffix` property:

```
dotnet build /p:VersionSuffix=beta1
```

### Creating a new Version Release

To create a new release, without any uncommitted changes perform the following steps:

1. Create a branch for the release (e.g. `git checkout -b release-0.0.2`)
1. Run the `./script/version.sh` script with the appropriate argument for the type of release (i.e. `major`, `minor`, or `patch`)
1. Push your branch and open a merge request

Once the merge request has been merged you can create a tag with just the version number (e.g. `0.0.2`) in the GitLab UI and this will automatically run the build pipeline and produce version tagged container images.

### API Versioning

Versioned API routes should always start with `/api/v{major-version-number}/...` so that in the event that we need to make a breaking change to an existing API, such as introducing a new required parameter or removing or renaming a previously returned property in a JSON body, we can introduce the new behavior at a new URL while preserving the existing behavior at the old endpoint.

It is imperative that all updates to the SONAR API are backward compatible with both earlier versions of the SONAR Agent, and external API consumers.
