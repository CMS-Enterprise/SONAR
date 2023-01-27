#! /usr/bin/env bash

### Variables ###
EXISTING_CLUSTERS=$(k3d cluster list --no-headers)
SONAR_REGISTRY_NAME=sonar-registry
SERVICE_CONFIG="./charts/sonar-agent/examples/values.example-service-config.yaml"
RECREATE=false
UPGRADE=false
EXISTS=false

usage() {
  echo "Usage Info"
  echo "Name of cluster is required"
  echo "-f     Designate a single service-config file."
  echo "-r     Recreate cluster if exists. Cannot be combined with (-u)"
  echo "-u     Upgrade helmchart. Cannot be combined with (-r)"
}

#Read Inputs/Options
while getopts "f:ru" options; do
  case $options in
    f) SERVICE_CONFIG=${OPTARG} ;;
    r) RECREATE=true ;;
    u) UPGRADE=true ;;
    *)
      usage
      exit 1 ;;
  esac
done

#Read next argument
shift $(($OPTIND - 1))
CLUSTER_NAME=$1

### Functions ###
function checkArguments() {
  error=false
  #Validate option combinations and arguments
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
  #Search specified name in list of clusters, performs word splitting using spaces and delimiters.
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
    #Cluster does not exist, create cluster
    echo Creating Cluster: $CLUSTER_NAME
    k3d cluster create $CLUSTER_NAME --registry-create $SONAR_REGISTRY_NAME
  fi
}

function getPort() {
  #Get Port Number
  echo Getting port number of "${SONAR_REGISTRY_NAME}"...
  #Some Bash Expansion
  PORT_NUMBER=$(docker ps -f name=$SONAR_REGISTRY_NAME --format "{{.Ports}}")
  PORT_NUMBER=${PORT_NUMBER#*:}
  PORT_NUMBER=${PORT_NUMBER%-*}
}

function dockerBuild() {
  #Built at root level.
  echo Building Docker image, tag and push to registry.
  docker build . -f Dockerfile.agent -t sonar-agent:testing
  docker tag sonar-agent:testing sonar-registry:${PORT_NUMBER}/sonar-agent:testing
  docker push sonar-registry:${PORT_NUMBER}/sonar-agent:testing
}

function runHelmchartTask() {
  if [[ $UPGRADE == true ]]; then
    echo Upgrading Helmchart...
    helm upgrade my-sonar-agent ./charts/sonar-agent -f ${SERVICE_CONFIG} --set image.repository=${SONAR_REGISTRY_NAME}:${PORT_NUMBER}/sonar-agent --set image.tag=testing
  else
    echo Installing Helmchart...
    helm install my-sonar-agent ./charts/sonar-agent -f ${SERVICE_CONFIG} --set image.repository=${SONAR_REGISTRY_NAME}:${PORT_NUMBER}/sonar-agent --set image.tag=testing
  fi
}

### Script Start ###
checkArguments
#Check /etc/hosts and prompt user to add hosts if needed
checkHosts
#Create or recreate cluster based on flags
createCluster
#Obtain port #
getPort
#Docker Build, Tag, Push
dockerBuild
#Install/Upgrade Helm Chart
runHelmchartTask
