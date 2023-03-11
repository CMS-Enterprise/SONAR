#!/usr/bin/env bash

PG_SECRET="postgres-creds"
API_SECRET="default-api-key"
NS="${1:-sonar}"

if ! kubectl get namespace "$NS" > /dev/null 2>&1; then
  kubectl create namespace "$NS"
fi

if [ -z "$POSTGRES_USER" ]; then read -rp "Enter Postgres username: " POSTGRES_USER; fi
if [ -z "$POSTGRES_USER" ]; then read -rp "Enter Postgres database: " POSTGRES_DB; fi
while [ -z "$POSTGRES_PASSWORD" ]
do
  read -rp "Enter Postgres password: " -s POSTGRES_PASSWORD
  echo ""
done

while [ -z "$DEFAULT_API_KEY" ]
do
  read -rp "Enter SONAR default API Key: " -s DEFAULT_API_KEY
  echo ""
done

if kubectl get secret -n "$NS" "$PG_SECRET" > /dev/null 2>&1; then
  kubectl delete secret -n "$NS" "$PG_SECRET"
fi

kubectl create secret generic "$PG_SECRET" -n "$NS" \
  --from-literal POSTGRES_DB="${POSTGRES_DB:-sonar}" \
  --from-literal POSTGRES_USER="${POSTGRES_USER:-root}" \
  --from-literal POSTGRES_PASSWORD="${POSTGRES_PASSWORD}"

if kubectl get secret -n "$NS" "$API_SECRET" > /dev/null 2>&1; then
  kubectl delete secret -n "$NS" "$API_SECRET"
fi

kubectl create secret generic "$API_SECRET" -n "$NS" \
  --from-literal ApiKey="${DEFAULT_API_KEY}"
