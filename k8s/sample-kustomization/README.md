# Testing Existence of SONAR Version Information in Local Cluster

## Local Testing

To test that SONAR's version information can be found in a local K3D cluster, follow the steps below.

### Prerequisites

1. Docker
2. [kubectl](https://kubernetes.io/docs/tasks/tools/)
3. [k3d](https://k3d.io/)
4. [fluxcd](https://github.com/fluxcd/flux2/)
5. [k9s](https://k9scli.io/) (optional)

### Manual Steps

Within this `k8s/sample-kustomization` directory...:

#### 1. Create K3d Cluster

```shell
k3d cluster create sonar-test --registry-create sonar-registry  -p "8088:80@loadbalancer"
```

#### 2. Install fluxcd on Cluster

```shell
flux install
```

#### 3. Create Test Namespace in Cluster

```shell
kubectl create ns sample-kustomization
```

#### 4. Create Test Secret

```shell
kubectl create secret generic basic-access-auth -n sample-kustomization --from-literal=username=<YOUR_EUA_ID> --from-literal=password='<YOUR_GITLAB_PERSONAL_ACCESS_TOKEN>'
```

#### 5. Deploy Test Manifest to Cluster
```shell
kubectl apply -k .
```

#### 6. Get IP Address
```shell
sed -n 's/nameserver \(.*\)/\1/p' < /etc/resolv.conf
```

#### 7. Edit coredns ConfigMap in Namespace kube-system*

Add the IP address obtained from step 6 to the end of the line with `forward ./etc/resolv.conf`, such that it reads as

```shell
forward ./etc/resolv.conf <IP_ADDRESS>
```

#### 8. Confirm Syncing of GitRepository and Kustomization With SONAR Repository*

Note: Reconciliation may take ~1-2 minutes.

The READY state of the CRDs should both be 'True'. The GitRepository STATUS should be
```shell
stored artifact for revision `<branch-name>@<commit SHA>`
```
and the Kustomization STATUS should be
```shell
Applied revision: <branch-name>@<commit SHA>
```

In the Kustomization, you should also see the version information in `status`' `Last Applied Revision`.

*k9s can be used here.



