import {HealthStatus, ServiceConfiguration} from "../api/data-contracts";

export function getService(serviceName: string | null, serviceList: ServiceConfiguration[] | null) {
  if (!serviceName || !serviceList) {
    return null;
  }
  return serviceList.find(service => service.name === serviceName);
}

export function getHealthStatusIndicator(status: HealthStatus | undefined) {
  let result;
  switch (status) {
    case HealthStatus.Unknown:
      result = "purple"
      break;
    case HealthStatus.Online:
      result = "#66FF00";
      break;
    case HealthStatus.Degraded:
      result = "orange";
      break;
    default:
      result = "red";
  }
  return result;
}
