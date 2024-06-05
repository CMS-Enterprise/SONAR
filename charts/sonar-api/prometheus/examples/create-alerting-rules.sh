#! /usr/bin/env bash

kubectl create configmap -n sonar sonar-alerting-rules \
  --from-file=alerting-rules.yaml=./alerting-rules.yaml \
  --dry-run=client -o yaml | \
  kubectl label -f- --dry-run=client -o yaml --local versionNumber='1'| \
  kubectl apply -f -
