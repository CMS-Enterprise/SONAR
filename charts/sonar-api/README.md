# SONAR API Helm Chart

## Installation Instruction

### 1. Create a k3d cluster

```shell
k3d cluster create sonar-test --registry-create sonar-registry  -p "8088:80@loadbalancer" --k3s-arg "--disable=traefik@server:0"
````

Confirm the coredns configmap contains host.k3d.internal in list of NodeHosts. Note that spinning up and down clusters via docker may remove
these configs as these are injected by k3d. Proper way to spin up and down a cluster is by using `k3d cluster start <name>` and `k3d cluster stop <name>`

| Hostname                | Description       |
|-------------------------|-------------------|
| host.k3d.internal       | docker host name  |
| k3d-sonar-test-tools    |                   |
| k3d-sonar-test-server-0 |                   |
| k3d-sonar-test-serverlb | k3d load balancer |
| sonar-registry          | local registry    |

### 2. Install using istioctl
Install the necessary custom resource definitions for istio which is used for managing network traffic. Istio is the default network controller for BLZ. If
istioctl is already installed on your local machine, you will just need to run `istioctl install` to install it into the cluster. Confirm by entering `y`.
```shell
brew install istioctl
istioctl install
```

### 3. Tag Docker image and push to registry
Container port number can be obtained by running `docker ps -f name=sonar-registry` and is the five digit value after the 0.0.0.0. The following
commands should be ran in the root level directory of sonar.

```shell
docker build -f Dockerfile.api . -t sonar-api:testing
docker tag sonar-api:testing sonar-registry:<REGISTRY_PORT>/sonar-api:testing
docker push sonar-registry:<REGISTRY_PORT>/sonar-api:testing
```
To deploy sonar-ui along with sonar-api, UI image must also be built, tagged, and push into sonar-registry.
```shell
docker build -f Dockerfile.ui . -t sonar-ui:testing
docker tag sonar-ui:testing sonar-registry:<REGISTRY_PORT>/sonar-ui:testing
docker push sonar-registry:<REGISTRY_PORT>/sonar-ui:testing
```

### 4. Create sonar namespace
```shell
kubectl create ns sonar
```
### 5. Helm charts prerequisites
Confirm the necessary charts repository are installed on your local machine by using `helm repo list` and verify bitnami and prometheus-community are present.
If not present, run `helm repo add prometheus-community https://prometheus-community.github.io/helm-charts` or `helm repo add bitnami https://charts.bitnami.com/bitnami`.
Then update and build the dependencies.
```shell
helm dependency update ./charts/sonar-api && helm dependency build ./charts/sonar-api
```

### 5. Installing Helmcharts
```shell
helm install sonar-api ./charts/sonar-api  --set image.repository=sonar-registry:<REGISTRY_PORT>/sonar-api --set image.tag=testing --namespace sonar
```
You can verify the correct services spun up by running `kubectl get pods --namespace sonar`. The following services: sonar-api, sonar-api-alertmanager, and sonar-api-prometheus-server should be up and running.

OR

To install the UI, set the `.Values.sonarConfig.UI.enabled` to `true` and execute the following command which sets the image tag and image repository for the UI.
```shell
helm install sonar-api ./charts/sonar-api  --set image.repository=sonar-registry:<REGISTRY_PORT>/sonar-api --set image.tag=testing --set sonar-ui.image.repository=sonar-registry:<REGISTRY_PORT>/sonar-ui --set sonar-ui.image.tag=testing --namespace sonar
```
You can verify the correct services spun up by running `kubectl get pods --namespace sonar`. The following services: sonar-api, sonar-api-alertmanager, sonar-api-prometheus-server and sonar-ui should be up and running.


### 6. Apply istio-gateway.yaml
Navigate to the k8s folder and apply the istio-gateway. Creation of the gateway is intended for testing purposes within the k3d environment. Actual deployment will use an existing gateway.
```shell
kubectl apply -f k8s/istio-gateway.yaml
```

## Upgrading/Uninstalling
To apply example kubernetes configuration
```shell
helm upgrade sonar-api ./charts/sonar-api -f ./examples/values.prometheus.yaml --set image.repository=sonar-registry:<REGISTRY_PORT>/sonar-api --set image.tag=testing --namespace sonar
```
To upgrade or delete helmcharts
```shell
helm upgrade sonar-api ./charts/sonar-api  --set image.repository=sonar-registry:<REGISTRY_PORT>/sonar-api --set image.tag=testing --namespace sonar
helm uninstall sonar-api --namespace sonar
```
## Expectations
Upon following the installation guide a local deployment of sonar-api, sonar-api-alertmanager, and sonar-api-prometheus-server will be running within a k3d cluster.
Note that postgresql needs be running locally for SONAR to be in a good working state. It is up to the user to deploy SONAR's postgresql database manually or deploy along with the sonar helmcharts.
This can be done by modifying the `sonarDatabase.enabled` flag and updating the host to use the internal cluster name `sonar-api-postgresql.sonar.svc.cluster.local`.

To curl into the k3d cluster from the host. If we rather not use the virtual service host filter, we can modify virtual service hosts to accept any hostname by using a *(wildcard).
```shell
curl -HHost:sonar.k3d.com "http://localhost:8088/api/ready"
```


