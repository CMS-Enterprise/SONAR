# Helm Charts

This folder contains helm charts for deploying SONAR components. Currently, our only Helm deployed component is the SONAR agent.

## Local Testing

To test the SONAR agent in kubernetes locally you can deploy it to a local K3D cluster by following these steps:

### 0. Prerequisites

1. Docker
2. [kubectl](https://kubernetes.io/docs/tasks/tools/)
3. [helm](https://helm.sh/docs/intro/install/)
4. [k3d](https://k3d.io/)

### 1. Create K3D Cluster

```shell
k3d cluster create sonar-test --registry-create sonar-registry
```

### 2. Get the TCP port for your Registry

```shell
docker ps -f name=sonar-registry
```

### 3. Add sonar-registry to your `/etc/hosts` file

In order to access the k3d docker registry via the host name `sonar-registry` we need to make that host name resolve to localhost (127.0.0.1). To do so add the following to your `/etc/hosts` file (note: you will need to launch your text editor with admin privileges using `sudo`)

```
# k3d docker registry for sonar
127.0.0.1 sonar-registry
```

### 4. Build, Tag, and Push your sonar-agent Container

From the root of the repository

```shell
docker build -f Dockerfile.agent . -t sonar-agent:testing
docker tag sonar-agent:testing sonar-registry:{PORT_NUMBER_FROM_STEP_2}/sonar-agent:testing
docker push sonar-registry:{PORT_NUMBER_FROM_STEP_2}/sonar-agent:testing
```

### 5. Install the Helm Chart Using the local image

```shell
helm install my-sonar-agent ./charts/sonar-agent -f ./charts/sonar-agent/examples/values.example-service-config.yaml --set image.repository=sonar-registry:{PORT_NUMBER_FROM_STEP_2}/sonar-agent --set image.tag=testing
```
