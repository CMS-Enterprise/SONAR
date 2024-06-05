{{/*
Expand the name of the chart.
*/}}
{{- define "sonar-api.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
If release name contains chart name it will be used as a full name.
*/}}
{{- define "sonar-api.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "sonar-api.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "sonar-api.labels" -}}
helm.sh/chart: {{ include "sonar-api.chart" . }}
{{ include "sonar-api.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "sonar-api.selectorLabels" -}}
app.kubernetes.io/name: {{ include "sonar-api.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Create the name of the service account to use tehre should always be a service account
*/}}
{{- define "serviceAccountName" -}}
{{- if .Values.serviceAccountName -}}
{{ include "sonar-api.fullname" . }}-service-account
{{- end }}
{{- end }}

{{/*
Create Role Name
*/}}
{{- define "roleName" -}}
{{- if .Values.role.nameOverride -}}
{{ .Values.role.nameOverride }}
{{- else -}}
{{ include "sonar-api.fullname" . }}-role
{{- end }}
{{- end }}

{{/*
container registry credentials
*/}}
{{- define "containerRegistrySecret" }}
  {{- $credType := typeOf .Values.registryCredentials -}}
        {{- /* If we have a list, embed that here directly. This allows for complex configuration from configmap, downward API, etc. */ -}}
  {{- if eq $credType "[]interface {}" -}}
  {{- include "multipleCreds" . | b64enc }}
  {{- else if eq $credType "map[string]interface {}" }}
    {{- /* If we have a map, treat those as key-value pairs. */ -}}
    {{- if and .Values.registryCredentials.username .Values.registryCredentials.password }}
    {{- with .Values.registryCredentials }}
    {{- printf "{\"auths\":{\"%s\":{\"username\":\"%s\",\"password\":\"%s\",\"email\":\"%s\",\"auth\":\"%s\"}}}" .registry .username .password .email (printf "%s:%s" .username .password | b64enc) | b64enc }}
    {{- end }}
    {{- end }}
  {{- end -}}
{{- end }}

{{- define "multipleCreds" -}}
{
  "auths": {
    {{- range $i, $m := .Values.registryCredentials }}
    {{- /* Only create entry if resulting entry is valid */}}
    {{- if and $m.registry $m.username $m.password }}
    {{- if $i }},{{ end }}
    "{{ $m.registry }}": {
      "username": "{{ $m.username }}",
      "password": "{{ $m.password }}",
      "email": "{{ $m.email | default "" }}",
      "auth": "{{ printf "%s:%s" $m.username $m.password | b64enc }}"
    }
    {{- end }}
    {{- end }}
  }
}
{{- end }}

{{/*
define host names
*/}}

{{- define "dbHost" -}}
{{- if .Values.sonarDatabase.host -}}
{{ .Values.sonarDatabase.host }}
{{- else -}}
{{ print "sonar-api-postgresql." .Release.Namespace ".svc.cluster.local" }}
{{- end }}
{{- end }}

{{- define "prometheusHost" -}}
{{- if .Values.sonarPrometheus.host -}}
{{ .Values.sonarPrometheus.host }}
{{- else -}}
{{ print "sonar-api-prometheus-server-headless." .Release.Namespace ".svc.cluster.local" }}
{{- end }}
{{- end }}

{{- define "alertmanagerHost" -}}
{{- if .Values.sonarAlertmanager.host -}}
{{ .Values.sonarAlertmanager.host }}
{{- else -}}
{{ print "sonar-api-alertmanager-headless." .Release.Namespace ".svc.cluster.local" }}
{{- end }}
{{- end }}

