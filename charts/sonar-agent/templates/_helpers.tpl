{{/*
Expand the name of the chart.
*/}}
{{- define "sonar-agent.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
If release name contains chart name it will be used as a full name.
*/}}
{{- define "sonar-agent.fullname" -}}
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
{{- define "sonar-agent.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "sonar-agent.labels" -}}
helm.sh/chart: {{ include "sonar-agent.chart" . }}
{{ include "sonar-agent.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "sonar-agent.selectorLabels" -}}
app.kubernetes.io/name: {{ include "sonar-agent.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
container registry crednetials
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