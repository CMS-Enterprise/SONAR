# SONAR Agent

The SONAR Agent is responsible for collecting configuration and service health information for a local environment (i.e. a single Kubernetes cluster) and pushing it to the SONAR API.

## Client Code Generation

Since the SONAR API exposes OpenApi documentation metadata, it is possible to auto-generate client C# code to access the API. Currently this is done using [NSwag](https://github.com/RicoSuter/NSwag/wiki/NSwag.MSBuild). Code generation only needs to be performed when the API changes, and can be done with the following command line:

```shell
# Note: in order for this command to work you need to have the API running and
# accessible via http://localhost:8081/
dotnet msbuild -target:NSwag
```

## Run SONAR Agent

Prequisites: database and SONAR API must be up.

To run the SONAR Agent, add the configuration file path as the first command line argument.
Example:
```
dotnet run service-config.json
```
To run the SONAR Agent with layered configuration (multiple config files), follow the example below:
```
dotnet run service-config.json service-config2.json service-config3.json
```
