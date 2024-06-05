#! /usr/bin/env bash

kubectl create configmap -n sonar sonar-alertmanager-templates \
  --from-file=../alertmanager-templates \
  --dry-run=client -o yaml | \
  kubectl apply -f -
