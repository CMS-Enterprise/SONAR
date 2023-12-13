## Testing Flux HelmRelease Version Information in Local Cluster (K3D)

podinfo.yaml is an example helmRelease, which can be deployed in a K3D cluster.
In order to test locally you can deploy a k3d instance of a cluster and deploy podinfo.yaml

### 1. Create K3D cluster
```shell
k3d cluster create sonar-test --registry-create sonar-registry  -p "8088:80@loadbalancer"
```

### 2. Install fluxcd on Cluster
```shell
flux install
```

### 3. Confirm HelmRelease version CRD's
In k9s view the CRDs and confirm
```helmreleases.helm.toolkit.fluxcd.io``` and verify version to be either ```v2beta1``` or ```v2beta2```

### 4. Apply podinfo helmRelease
```shell
kubectl apply -f podinfo.yaml
```

### 5. Confirm podInfo version
In k9s navigate to helmreleases, describe podinfo and confirm last applied version.

### 6. Launch SONAR locally
Launch SONAR (API and UI).

Launch SONAR-agent with the service config found in ```/k8s/sample-helmrelease/service-config.json``` with the ```--kubernetes-configuration``` flag set.
Assuming kubeconfig is configured, sonar-agent will be able to sample the flux kubernetes controller from the K3D cluster
and retrieve the helm release version noted in step 5.

### 7. Verify helmRelease version in the UI.
