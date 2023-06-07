#! /usr/bin/env bash

set -e

### Variables ###
EXISTING_CLUSTERS=$(k3d cluster list --no-headers)
SONAR_REGISTRY_NAME=sonar-registry
HELM_VALUES_FILE=
TAG=testing
RECREATE=false
UPGRADE=false
EXISTS=false
API=false
HELP=false

usage() {
  echo "Usage: k3d-deply.sh [options] <cluster_name> [helm_args...]"
  echo "Creates or recreates a K3D cluster and installs or upgrades the sonar-agent helm"
  echo "chart in that cluster."
  echo ""
  echo "Cluster name is required. Any additional arguments will be passed to Helm."
  echo ""
  echo "Options:"
  echo "  -f <file>    Designate a single helm values file. If you want to specify"
  echo "               multiple files you can pass --values as a helm argument."
  echo "  -t <tag>     The tag to apply to the docker image."
  echo "  -r           Recreate cluster if exists. Cannot be combined with (-u)."
  echo "  -u           Upgrade the helm chart. Cannot be combined with (-r)."
  echo "  -a           Install sonar API, dependencies, and test apps as well."
  echo ""
  echo "  -h           Display this help screen."
}

# Read Inputs/Options
while getopts "f:t:ruah" options; do
  case $options in
    f) HELM_VALUES_FILE=${OPTARG} ;;
    t) TAG=${OPTARG} ;;
    r) RECREATE=true ;;
    u) UPGRADE=true ;;
    a) API=true ;;
    h) HELP=true ;;
    *)
      usage
      exit 1 ;;
  esac
done

if [ ${HELP} == true ]; then
  usage
  exit 0
fi

if [ ! -d ./charts/sonar-agent ]; then
  echo "k3d-deploy.sh should be run from the root of the repository."
  exit 1
fi

shift $(($OPTIND - 1))
CLUSTER_NAME=$1
shift
echo "Helm arguments: " "$@"

### Functions ###
function checkArguments() {
  error=false
  # Validate option combinations and arguments
  if [ ${UPGRADE} == true ] && [ ${RECREATE} = true ]; then
    echo "Upgrade (-u) and Recreate (-r) options cannot be combined. Initial helmchart install must be performed."
    error=true
  fi

  if [ -z ${CLUSTER_NAME} ]; then
    echo Cluster Name required.
    echo "e.g. \"./charts/sonar-agent/k3d-deploy.sh sonar-test\""
    error=true
  fi

  if [ ${error} == true ]; then
    usage
    exit 1
  fi
}
function checkHosts() {
  if grep -xq "127.0.0.1 ${SONAR_REGISTRY_NAME}" /etc/hosts; then
    echo ${SONAR_REGISTRY_NAME} found in /etc/hosts.
  else
    echo ${SONAR_REGISTRY_NAME} not found in /etc/hosts. Please add \"127.0.0.1 ${SONAR_REGISTRY_NAME}\" to /etc/hosts
    exit 1
  fi
}

function createCluster() {
  # Search specified name in list of clusters, performs word splitting using spaces and delimiters.
  for word in $EXISTING_CLUSTERS
  do
    if [ $CLUSTER_NAME == $word ]; then
      echo Cluster Already Exists...
      EXISTS=true

      if [ $RECREATE == true ]; then
        echo "Cluster recreate (-r) flag set. Deleting cluster..."
        k3d cluster delete $CLUSTER_NAME
        EXISTS=false
      fi
    fi
  done

  if [ $EXISTS != true ]; then
    # Cluster does not exist, create cluster
    echo Creating Cluster: $CLUSTER_NAME
    k3d cluster create $CLUSTER_NAME --registry-create $SONAR_REGISTRY_NAME -p "8088:80@loadbalancer"
  fi
}

function getPort() {
  # Get Port Number
  echo Getting port number of "${SONAR_REGISTRY_NAME}"...
  # Some Bash Expansion
  PORT_NUMBER=$(docker ps -f name=$SONAR_REGISTRY_NAME --format "{{.Ports}}")
  # Remove 0.0.0.0: prefix
  PORT_NUMBER=${PORT_NUMBER#*:}
  # Remove ->5000/tcp suffix
  PORT_NUMBER=${PORT_NUMBER%-*}
}

function dockerBuild() {
  # Built at root level.
  echo Building Agent Docker image, tag and push to registry.
  docker build . -f Dockerfile.agent -t sonar-agent:latest
  docker tag sonar-agent:latest sonar-registry:${PORT_NUMBER}/sonar-agent:${TAG}
  docker push sonar-registry:${PORT_NUMBER}/sonar-agent:${TAG}
  if [ $API == true ]; then
    echo Building API Docker image, tag and push to registry.
    docker build . -f Dockerfile.api -t sonar-api:latest
    docker tag sonar-api:latest sonar-registry:${PORT_NUMBER}/sonar-api:${TAG}
    docker push sonar-registry:${PORT_NUMBER}/sonar-api:${TAG}

    docker build ./test/test-metric-app/ -t test-metric-app:latest
    docker tag test-metric-app:latest sonar-registry:${PORT_NUMBER}/test-metric-app:${TAG}
    docker push sonar-registry:${PORT_NUMBER}/test-metric-app:${TAG}

    docker build ./test/http-metric-test-app/ -t http-metric-test-app:latest
    docker tag http-metric-test-app:latest sonar-registry:${PORT_NUMBER}/http-metric-test-app:${TAG}
    docker push sonar-registry:${PORT_NUMBER}/http-metric-test-app:${TAG}
  fi
}

function installApiAndDependencies() {
  if [ $API == true ]; then
    # this is used by kustomize to configure the image for the sonar-api Deployment
    echo "SONAR_API_IMAGE=sonar-registry:${PORT_NUMBER}/sonar-api:${TAG}" > ./k8s/.env
    ./k8s/create-namespace-and-secret.sh
    kubectl apply -k ./k8s

    echo "TEST_METRIC_APP_IMAGE=sonar-registry:${PORT_NUMBER}/test-metric-app:${TAG}" > ./k8s/test-apps/.env
    echo "HTTP_METRIC_TEST_APP_IMAGE=sonar-registry:${PORT_NUMBER}/http-metric-test-app:${TAG}" >> ./k8s/test-apps/.env
    kubectl apply -k ./k8s/test-apps
  fi
}

function runHelmchartTask() {
  FILE_ARGS=()
  if [ -n "$HELM_VALUES_FILE" ]; then
    FILE_ARGS=("-f" "$HELM_VALUES_FILE")
  fi
  if [[ $UPGRADE == true ]]; then
    echo Upgrading Helmchart...
    helm upgrade sonar-agent ./charts/sonar-agent "${FILE_ARGS[@]}" \
      --namespace sonar \
      --set image.repository=${SONAR_REGISTRY_NAME}:${PORT_NUMBER}/sonar-agent \
      --set image.tag=$TAG --set image.pullPolicy=Always "$@"
  else
    echo Installing Helmchart...
    helm install sonar-agent ./charts/sonar-agent "${FILE_ARGS[@]}" \
      --namespace sonar \
      --set image.repository=${SONAR_REGISTRY_NAME}:${PORT_NUMBER}/sonar-agent \
      --set image.tag=$TAG --set image.pullPolicy=Always "$@"
  fi
}

### Script Start ###
checkArguments
# Check /etc/hosts and prompt user to add hosts if needed
checkHosts
# Create or recreate cluster based on flags
createCluster
# Obtain port #
getPort
# Docker Build, Tag, Push
dockerBuild
# Install API & dependencies
installApiAndDependencies
# Install/Upgrade Helm Chart
runHelmchartTask "$@"
