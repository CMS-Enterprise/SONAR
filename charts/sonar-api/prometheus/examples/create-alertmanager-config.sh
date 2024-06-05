#! /usr/bin/env bash

kubectl create configmap -n sonar sonar-alertmanager-config \
  --from-file=alertmanager-config.yaml=./alertmanager-config.yaml \
  --dry-run=client -o yaml | \
  kubectl label -f- --dry-run=client -o yaml --local versionNumber='1'| \
  kubectl apply -f -

