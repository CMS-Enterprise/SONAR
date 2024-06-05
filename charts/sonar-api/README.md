# SONAR API Helm Chart

## Installation Instruction

### 1. Create a k3d cluster

```shell
k3d cluster create sonar-test --registry-create sonar-registry  -p "8088:80@loadbalancer" --k3s-arg "--disable=traefik@server:0"
````

Confirm the coredns configmap contains host.k3d.internal in list of NodeHosts. Note that spinning up and down clusters via docker may remove
these configs as these are injected by k3d. Proper way to spin up and down a cluster is by using `k3d cluster start <name>` and `k3d cluster stop <name>`

| IP             | Hostname                | Description      |
|----------------|-------------------------|------------------|
| 192.168.65.254 | host.k3d.internal       | Docker host name |
| 192.168.112.2  | k3d-sonar-test-tools    |                  |
| 192.168.112.3  | k3d-sonar-test-server-0 |                  |
| 192.168.112.4  | k3d-sonar-test-serverlb |                  |
| 192.168.112.5  | sonar-registry          |                  |

### 2. Install using istioctl
Install the necessary custom resource definitions for istio which is used for managing network traffic. Istio is the default network controller for BLZ.
```shell
brew install istioctl
istioctl install
```

### 3. Tag Docker image and push to registry
```shell
docker build -f Dockerfile.api . -t sonar-api:testing
docker tag sonar-api:testing sonar-registry:<REGISTRY_PORT>/sonar-api:testing
docker push sonar-registry:<REGISTRY_PORT>/sonar-api:testing
```

### 4. Create sonar namespace
```shell
kubectl create ns sonar
```

### 5. Installing Helmcharts
```shell
helm install sonar-api ./charts/sonar-api  --set image.repository=sonar-registry:<REGISTRY_PORT>/sonar-api --set image.tag=testing --namespace sonar
```
To upgrade or delete helmcharts
```shell
helm upgrade sonar-api ./charts/sonar-api  --set image.repository=sonar-registry:<REGISTRY_PORT>/sonar-api --set image.tag=testing --namespace sonar
helm uninstall sonar-api --namespace sonar
```

### 6. Apply istio-gateway.yaml
Navigate to the k8s folder and apply the istio-gateway. Creation of the gateway is intended for testing purposes within the k3d environment. Actual deployment will use an existing gateway.
```shell
kubectl apply -f istio-gateway.yaml
```

### 7. Applying example values
```shell
helm upgrade sonar-api ./charts/sonar-api -f ./examples/values.prometheus.yaml --set image.repository=sonar-registry:<REGISTRY_PORT>/sonar-api --set image.tag=testing --namespace sonar
```

## Expectations
Upon following the installation guide a local deployment of sonar-api, sonar-api-alertmanager, and sonar-api-prometheus-server will be running within a k3d cluster.
Note that postgresql needs be running locally for SONAR to be in a good working state. It is up to the user to deploy SONAR's postgresql database manually or deploy along with sonar helmcharts.
This can be done by modifying the `sonarDatabase.enabled` flag and updating the host to use the internal cluster name `sonar-api-postgresql.sonar.svc.cluster.local`.

To curl into the k3d cluster from the host. If we rather not use the virtual service host filter, we can modify virtual service hosts to accept any hostname by using a *(wildcard).
```shell
curl -HHost:sonar.k3d.com "http://localhost:8088/api/ready"
```


