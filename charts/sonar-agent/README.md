# SONAR Agent Helm Chart

This Helm Chart deploys a single instance of the `sonar-agent` microservice as a single instance `StatefulSet` along with `ConfigMaps` for both application configuration (i.e. settings for connecting to the SONAR API and other dependencies), and for environment/tenant/service configuration which controls the services that this SONAR Agent instance will monitor.

## Values

### Kubernetes Configuration

|Key|Type|Description|
|---|---|---|
|nameOverride|string|Overrides the name of the StatefulSet and Service created by this chart.|
|image.repository|string|The repository/image name of the container image to use.|
|image.tag|string|The tag of the sonar-agent container image to use.|
|image.pullPolicy|string|An [ImagePullPolicy](https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/pod-v1/#image) string governing when the image should be pulled from the registry.|
|networkPolicies|object|See the [Network Policies section](#values_network_policies) below.|
|imagePullSecrets|list|A list of names of pre-existing Secrets containing container registry credentials to be used when pulling container images.|
|registryCredentials|object|See the [Registry Credentials section](#values_registry_credentials) below.|
|podAnnotations|object|A object containing an arbitrary set of key/string pairs to apply to the created pods as annotations.|
|resources|object|A [ResourceRequirements](https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/pod-v1/#resources) specification to be applied to the created pods.|
|tolerations|list|A list of [Toleration](https://kubernetes.io/docs/reference/kubernetes-api/workload-resources/pod-v1/#scheduling) specifications to be applied to the created pods.|
|kubernetesServiceConfig.enabled|boolean|When set to true, sonar agent will attempt to load tenant service configuration from Kubernetes ConfigMaps via the kubernetes API|
|serviceAccountName|string|When specified, creates a service account for the sonar-agent pod, and binds it to a cluster role with permission to read Namespaces and ConfigMaps. This is required for the sonar-agent to automatically load configuration from Kubernetes ConfigMaps. (default: null)|

### Application Configuration

| Key                         |Type|Description|
|-----------------------------|---|---|
| apiConfig.environment       |string|The name of the environment for which this agent is reporting information.|
| apiConfig.tenant            |string|The name of the tenant for which this agent is reporting information.|
| apiConfig.baseUrl           |string|The base URL of the SONAR API to report information to.|
| apiConfig.apiKey            |string|The API key to use to authenticate requests to the SONAR API.|
| apiConfig.reportingInterval |number|The minimum interval (in seconds) between service health checks.|
| apiconfig.isNonProd         |Boolean|Flag to determine if environment in production or not.|
| prometheus.host             |string|The host name to use when connecting to Prometheus.|
| prometheus.protocol         |string|The protocol to use when connecting to Prometheus (default: http).|
| prometheus.port             |number|The port to use when connecting to Prometheus (default: 9090).|
| loki.host                   |string|The host name to use when connecting to Loki.|
| loki.protocol               |string|The protocol to use when connecting to Loki (default: http).|
| loki.port                   |number|The port to use when connecting to Loki (default: 3100).|
| configs                     |object|One or more [sonar-agent service configurations](#values_sonar_service_configuration) which define the services being monitored and the checks used to determine their health.|

### <a name="values_sonar_service_configuration" />SONAR Service Configuration

The `configs` value contains a map of config names to SONAR Service Configuration definitions. Each of these consists of three keys:

|Key|Type|Description|
|---|---|---|
|order|number|\[Optional] The order in which this configuration should be merged.|
|services|object|A mapping of service names to service definitions.|
|rootServices|list|A list of service names that should be returned as "root" services (i.e. the top of the hierarchy).|

#### Service Definitions consist of the following keys:

|Key|Type|Description|
|---|---|---|
|description|string|\[Optional] A human readable description of the service.|
|url|string|\[Optional] The URL that a human would use to access the service.|
|children|list|\[Optional] A list of service names that are components or dependencies of this service. Each name must have a corresponding entry in the `services` object.|
|healthChecks|object|\[Optional] A list of health checks used to monitor the service.|

#### Health Checks consist of the following keys:

|Key|Type|Description|
|---|---|---|
|description|string|\[Optional] A human readable description of the health check.|
|type|string|One of the following health check types: `"httpRequest"`, `"prometheusMetric"`, or `"lokiMetric"`.|
|definition|object|The definition of the health check.|

#### Common Health Check Definition Keys:

|Key|Type|Description|
|---|---|---|
|conditions|list|A list of conditions that map observed values to health statuses.|

#### Prometheus Health Check Definition Keys:

|Key|Type|Description|
|---|---|---|
|duration|string|A duration string in the format `d.HH:mm:ss.fff` for which to evaluate the metric query.|
|expression|string|A [PromQL range vector query](https://prometheus.io/docs/prometheus/latest/querying/basics/) returning a single time series.|

#### Loki Health Check Definition Keys:

|Key|Type|Description|
|---|---|---|
|duration|string|A duration string in the format `d.HH:mm:ss.fff` for which to evaluate the metric query.|
|expression|string|A [LogQL metric query](https://grafana.com/docs/loki/latest/logql/metric_queries/) returning a single time series.|

#### Loki and Prometheus Conditions:

|Key|Type|Description|
|---|---|---|
|operator|string|The operator expression to use to compare the metric value to the threshold value.|
|threshold|number|The threshold value to compare the metric to.|
|status|string|The status that the service has if the condition is met.|

When conditions are evaluated for Loki and Prometheus health checks the following rules are applied: For each condition, if any data point in the time window specified by `duration` meets the `operator`/`threshold` criteria, then the service is considered to have that status. Conditions are evaluated in order, and once one condition is met evaluation stops, so it is important to list conditions from most severe to the least severe (See the [SONAR Health Status section](#sonar_health_status) below). If no conditions are met the service is considered to be `Online`

#### Http Health Check Definition Keys:

|Key|Type|Description|
|---|---|---|
|url|string|The URL to send an HTTP GET request to.|
|followRedirects|boolean|Indicates whether or not HTTP redirects should be automatically followed (defaults to `true` if not specified).|
|authorizationHeader|string|\[Optional] The value of the `Authorization` header to specify when making the request.|
|skipCertificateValidation|string|When set to `true` the SSL certificate supplied by the server will not be validated (even if it is expired, untrusted, or has a name mismatch, the response will still be evaluated by the health check conditions). The default behavior is for SSL certificate validation issues to cause the service to be considered `Offline`.|

#### Http Health Check Conditions:

|Key|Type|Description|
|---|---|---|
|type|string|The type of condition, either `httpStatusCode` or `httpResponseTime`.|
|statusCodes|list|For `httpStatusCode` conditions, a list of specific HTTP status code numbers to match.|
|responseTime|string|For `httpResponseTime` conditions, a duration string (format `HH:mm:dd.fff`) indicating the _minimum_ response time, over which the condition will match|
|status|string|The status that the service has if the condition is met.|

All `httpStatusCode` conditions are evaluated first. If not `httpStatusCode` conditions are specified then a default condition matching status codes `200` and `204` will be used. If no `httpStatusCode` conditions match, the service is considered to be `Offline`. Once `httpStatusCode` conditions are evaluated, `httpResponseTime` conditions are evaluated. For any `httpResponseTime` conditions that evaluate to true, the most severe status is used. Note: there is also a hard request timeout set to the SONAR Agent's reporting interval. If any request takes longer than this amount of time then that service is considered to be `Offline`.

#### <a name="sonar_health_status" />SONAR Health Status

SONAR supports the following statuses ordered from most severe to least severe.

|Status|Description|
|---|---|
|Unknown|It was not possible to ascertain the health of the service.|
|Offline|The service is unreachable or not functioning.|
|Degraded|The service is reachable but may not be performing as desired, some requests my fail, time out, or be delayed.|
|AtRisk|The service is reachable and performing as expected, but there are signs that it may begin encountering issues in the future if unaddressed.|
|Online|The service is reachable and functioning as expected.|

#### Example:

```yaml
configs:
  my-config-name:
    order: 1
    services:
      my-service:
        description: "string"
        url: "https://example/"
        healthChecks:
          metricExample:
            type: "prometheusMetric"
            description: "string"
            definition:
              duration: "0.00:05:00"
              expression: "max(container_memory_working_set_bytes{namespace=\"gitlab\", container=\"webservice\"})"
              conditions:
                - operator: "GreaterThan"
                  threshold: 3e+9
                  status: "AtRisk"
          httpExample:
            type: "httpRequest"
            definition:
              url: "https://example/api/ready"
              followRedirects: false
              authorizationHeader: "Bearer XYZ"
              conditions:
                - type: "httpStatusCode"
                  statusCodes: [ 200, 204, 301, 302 ]
                  status: "Online"
                - type: "httpResponseTime"
                  responseTime: "00:00:02"
                  status: "Degraded"
    rootServices: ["my-service"]
  other-config:
    order: 2
    services:
      another-service:
        description: "string"
        url: "https://alternate/"
        healthChecks:
          logsExample:
            type: "lokiMetric"
            definition:
              duration: "0.00:59:00"
              expression: "sum(count_over_time({container=\"alternate-example\", level=\"Error\"}[1m])"
              conditions:
                - operator: "GreaterThanOrEqual"
                  threshold: 1
                  status: "AtRisk"
```

#### Service Configuration Merging

Multiple configurations may be specified, and they will be merged together in the order specified by the `order` value (from least to greatest). When configurations are merged any service with the same name as a service declared in a previous configuration will **completely replace** the previous settings for that service. The rootServices arrays, on the other hand, will be combined by a set union. This multiple configuration capability is primarily intended for situations where you have multiple sets of unrelated services applicable for different environments. For more complex scenarios requiring full patch semantics it is recommended that you implement that externally, prior to providing values to this Helm Chart.

### <a name="values_network_policies" />Network Policies

When enabled (which it is by default), network policies will be created that limit the connectivity allowed to and from pods created as part of this helm chart. The default egress behavior is to allow connections to be made to the KubeDNS service and to any other pods within the Kubernetes cluster, but block all connections to endpoints outside the cluster. The default ingress behavior is to block all incoming connections. These rules can be customized via the following values:

|Key|Type|Description|
|---|---|---|
|networkPolicies.enabled|boolean|When set to false, disables network policy creation|
|networkPolicies.defaultAllowWithinCluster|boolean|When set to false, disables the default rule allowing connections to all other Pods in the cluster|
|additionalEgressRules|list|A list of [NetworkPolicyEgressRules](https://kubernetes.io/docs/reference/kubernetes-api/policy-resources/network-policy-v1/#NetworkPolicySpec) to be applied in addition to the defaults|
|additionalIngressRules|list|A list of [NetworkPolicyIngressRules](https://kubernetes.io/docs/reference/kubernetes-api/policy-resources/network-policy-v1/#NetworkPolicySpec) to be applied|

### <a name="values_registry_credentials" />Registry Credentials

When specified, registry credentials are used to create a `kubernetes.io/dockerconfigjson` type Secret which will then be used when fetching container images. Note: this setting is not compatible with `imagePullSecrets` and only one or the other should be used.

|Key|Type|Description|
|---|---|---|
|registry|string|The host name of the registry the credential is for|
|username|string|The username of the credential|
|password|string|The password of the credential|
|email|string|The email address of the user|
