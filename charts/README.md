# Helm Charts

This folder contains helm charts for deploying SONAR components. Currently, our only Helm deployed component is the SONAR agent.

This file contains *internal developer* documentation. For documentation of the normal usage and configuration of the sonar-agent Helm chart see [this readme](./sonar-agent/README.md).

## Local Testing

To test the SONAR agent in kubernetes locally you can deploy it to a local K3D cluster by following these steps:

### Prerequisites

1. Docker
2. [kubectl](https://kubernetes.io/docs/tasks/tools/)
3. [helm](https://helm.sh/docs/intro/install/)
4. [k3d](https://k3d.io/)

### Running k3d-deploy.sh

To create k3d cluster of the sonar-agent with the name "sonar-test" and install the sonar agent helm chart, run the following from the root of the repository:

```shell
./scripts/k3d-deploy.sh sonar-test
```

You can subsequently re-run this command with the `-r` flag to delete and recreate the cluster, or with the `-u` flag to upgrade the previously installed helm chart.

```
Usage: k3d-deply.sh [options] <cluster_name> [helm_args...]
Creates or recreates a K3D cluster and installs or upgrades the sonar-agent helm
chart in that cluster.

Cluster name is required. Any additional arguments will be passed to Helm.

Options:
  -f <file>    Designate a single helm values file. If you want to specify
               multiple files you can pass --values as a helm argument.
  -t <tag>     The tag to apply to the docker image.
  -r           Recreate cluster if exists. Cannot be combined with (-u).
  -u           Upgrade the helm chart. Cannot be combined with (-r).
  -a           Install sonar API, dependencies, and test apps as well.
```

#### Installing SONAR API, dependencies, and test apps

When run with the `-a` flag, this script use the kustomization and kubernetes resources defined in the [k8s](../k8s) folder to install the SONAR API and the test-metric and http-metric-test apps to enable end-to-end testing.

### Accessing Services

Once your k3d cluster is up and running you can access services running within k3d via port `8088`. Because services running in k3d are all exposed via a single port, routing is done based on path prefix:

| Path | Service              |
|------|----------------------|
| [/](http://localhost:8088/api/ready) | sonar-ui             |
| [/api/*](http://localhost:8088/api/ready) | sonar-api            |
| [/metrics](http://localhost:8088/metrics) | test-metric-app      |
| [/test/*](http://localhost:8088/test/ready) | http-metric-test-app |
| [/prometheus/*](http://localhost:8088/prometheus/graph) | prometheus           |

### Manual Steps

These steps are automated by the  [k3d-deploy.sh](../scripts/k3d-deploy.sh) script, but if you are really curious, here are the manual steps that you can use to set up a k3d cluster.

#### 1. Create K3D Cluster

```shell
k3d cluster create sonar-test --registry-create sonar-registry  -p "8088:80@loadbalancer"
```

#### 2. Get the TCP port for your Registry

```shell
docker ps -f name=sonar-registry
```

#### 3. Add sonar-registry to your `/etc/hosts` file

In order to access the k3d docker registry via the host name `sonar-registry` we need to make that host name resolve to localhost (127.0.0.1). To do so add the following to your `/etc/hosts` file (note: you will need to launch your text editor with admin privileges using `sudo`)

```
# k3d docker registry for sonar
127.0.0.1 sonar-registry
```

#### 4. Build, Tag, and Push your sonar-agent Container

From the root of the repository

```shell
docker build -f Dockerfile.agent . -t sonar-agent:testing
docker tag sonar-agent:testing sonar-registry:{PORT_NUMBER_FROM_STEP_2}/sonar-agent:testing
docker push sonar-registry:{PORT_NUMBER_FROM_STEP_2}/sonar-agent:testing
```

#### 5. Install the Helm Chart Using the local image

```shell
helm install my-sonar-agent ./charts/sonar-agent -f ./charts/sonar-agent/examples/values.example-service-config.yaml --set image.repository=sonar-registry:{PORT_NUMBER_FROM_STEP_2}/sonar-agent --set image.tag=testing
```
