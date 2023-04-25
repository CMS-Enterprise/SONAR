# SONAR Automated Tests

## Test Projects

These are the main unit and integration test projects for SONAR:

* [sonar-api-tests](sonar-api-tests/): Unit and Integration tests for SONAR API.
* [sonar-agent-tests](sonar-agent-tests/): Unit and Integration tests for SONAR Agent.
* [sonar-core-tests](sonar-core-tests/): Unit and Integration tests for SONAR Core.

## Live Helper Apps

There are also a couple of helper apps that can be used for live integration testing. These are not automated tests, rather they are test applications that run as monitoring targets for testing SONAR in the Docker compose/k3d environment.

* [http-metric-test-app](http-metric-test-app/): An application that exposes HTTP metrics for SONAR monitoring.
* [test-metric-app](test-metric-app/): An application that exposes Loki and Prometheus metrics for SONAR monitoring.

## Running Tests

### Without code coverage (just run the tests and get a pass/fail indicator).

Run `dotnet test` from the root of any of the test projects.

### With code coverage (run the tests and generate a coverage report using Coverlet).

Run `dotnet msbuild -t:test-with-coverage` from the root of any of the test projects.

_Note: This command is a custom MSBuild target. The target is defined in `test-with-coverage.targets` and is imported by the `.csproj` file in each of the test projects._
