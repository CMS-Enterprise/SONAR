[![pipeline status](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/badges/main/pipeline.svg)](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/sonar/-/commits/main) | Sandbox: [![Sandbox Deployment Status](https://argocd.batcave-impl.internal.cms.gov/api/badge?name=sonar&revision=true)](https://argocd.batcave-impl.internal.cms.gov/applications/sonar)

# SONAR

SONAR is an API endpoint and dashboard UI for monitoring the health of both BatCAVE core services and BatCAVE deployed applications. SONAR actively monitors and records the health of services running in BatCAVE using a combination of Prometheus queries (for checks of system and application metrics), Loki queries (for checks of log messages), HTTP/S status code checks, DNS checks, and TLS certificate checks.

## Components

 * [sonar-api](sonar-api/)
 * [sonar-agent](sonar-agent/) - An agent process that performs monitoring tasks within an environment and pushes data to the API.
 * [sonar-core](sonar-core/) - A shared library with data model types used by both components and general utility classes.

## Prerequisites

### dotnet SDK

Install the [dotnet SDK 7.x](https://dotnet.microsoft.com/en-us/download) (note: in some cases just the dotnet CLI will suffice, but for development it is usually preferable to have the SDK installed).

On MacOS you can also install the .Net SDK via Homebrew:

```
brew install dotnet-sdk
```

### snappy

The snappy compression library is a prerequisite for the SONAR API (use for Prometheus integration). To install this library on MacOS:

#### MacOS Installation

```
brew install snappy
```

If using Homebrew on Apple Silicon, you will need to set the `DYLD_FALLBACK_LIBRARY_PATH` environment variable to `/opt/homebrew/lib`. Where you set this depends on where you want the variable to be available.

For shells, it's fine to export it from .bashrc or .zshrc:

```shell
export DYLD_FALLBACK_LIBRARY_PATH=/opt/homebrew/lib
```

For GUI applications, the situation is more complicated. Some options are:
- Set up a custom Launch Agent for use with launchd/launchctl.
- Modify the application's Info.plist to include the variable.
- Use a feature built-in to the application for setting environment variables in the proper context (such as application/unit test launch profile in your IDE).

High-level explanation: On non-Apple silicon Macs, Homebrew's default installation prefix is `/usr/local/` which is in the system-default search path for binaries and libraries. However On Apple silicon Macs, Homebrew's default installation prefix is `/opt/homebrew/` and Homebrew modifies your PATH to include `/opt/homebrew/bin` but doesn't update the system library search path; this means dylibs installed by Homebrew won't automatically be available to non-Homebrew installed binaries, so we have to wire up the library search path manually.

Deeper explanation: Finding the *right* solution to this issue leads to quite the rabbit trail regarding MacOS Homebrew behavior, system-default search paths, and the proper way to set environment variables. Start with the the following if you really want to read more:
- https://github.com/Homebrew/brew/issues/13481.
- `man dyld`
- https://superuser.com/a/541068
- https://apple.stackexchange.com/questions/454430/set-environment-variable-for-the-whole-gui-session-aka-without-using-zshenv

#### Linux Installation
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
dotnet run -- -f ./service-config.json
```

## Dependencies & Docker Compose

The SONAR API and Agent have two critical dependencies (PostgreSQL and Prometheus) and one optional dependency (Loki). All of these can be run via Docker Compose using the `docker-compose.yml` file in the root of the repo:

```
docker-compose up -d prometheus postgresql loki
```

There is also an example application that generates Prometheus metrics that can be used when testing SONAR health checks. To run this application in in Docker as well run:

```
docker-compose up -d test-metric-app
```

To build Docker images via Docker Compose, run the following command:

```
docker-compose build
```

To selectively run SONAR services via Docker Compose, run the following command:

```
docker-compose up sonar-api sonar-agent
```

## Running in Kubernetes

In production environments the SONAR agent is installed via it's Helm chart by batcave-landing-zone, and the SONAR API is deployed via ArgoCD based on the [manifests repo](https://code.batcave.internal.cms.gov/ado-repositories/oit/waynetech/manifests/). However, it is possible to test SONAR locally in a k3d or other small Kubernetes cluster. For detailed instructions see the [helm chart developer readme](./charts/README.md).

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
