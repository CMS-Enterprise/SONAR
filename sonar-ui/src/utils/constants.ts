export const ToolTipText = {
  environmentTip: "An Environment could be a Kubernetes Cluster, AWS/Azure/GoogleCloud Account, Virtual Private Cloud (VPC), or physical data center. A single SONAR agent process must be deployed to each environment and the process will report on the status of that environment.",
  statusHistory: {
    stepTip: "Also known as query resolution, the step is the amount of time between each instantaneous sample evaluation. The step also determines how many data points you will get for a given range of time.",
    httpCheckConditionsTip: "HTTP health check conditions are the criteria used to evaluate the result of the HTTP health check. The worst condition met is the aggregate result of the HTTP health check.",
    prometheusConditionsTip: "The result of the PromQL query is evaluated based on the health status thresholds defined in the health check. The resulting status is the worst condition that is met.",
    lokiConditionsTip: "The result of the LogQL query is evaluated based on the health status thresholds defined in the health check. The resulting status is the worst condition that is met."
  }
}
