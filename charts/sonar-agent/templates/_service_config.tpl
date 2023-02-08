{{- define "serviceConfig" -}}
{
  "services": [
    {{- $ix := 0 }}
    {{- range $key, $value := .services }}
    {{-   if $value }}
    {{- /* Convert healthCheck dict to list */ -}}
    {{-     $healthChecks := list }}
    {{-     range $hcKey, $hcValue := $value.healthChecks }}
    {{-       $healthChecks = append $healthChecks (mergeOverwrite $hcValue (dict "name" $hcKey)) }}
    {{-     end }}
    {{-     if gt $ix 0 }},{{ end }}
    {{ toPrettyJson (mergeOverwrite $value (dict "name" $key "healthChecks" $healthChecks)) | indent 4 | trim }}
    {{-     $ix = add1 $ix  }}
    {{-   end }}
    {{- end }}
  ],
  "rootServices": {{ toJson .rootServices }}
}
{{- end }}