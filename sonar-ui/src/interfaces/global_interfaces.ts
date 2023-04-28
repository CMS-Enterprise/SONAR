import { DateTimeHealthStatusValueTuple, ServiceHierarchyHealth } from '../api/data-contracts';

export interface StatusHistoryView {
  serviceData: ServiceHierarchyHealth,
  statusTimestampTuple: DateTimeHealthStatusValueTuple
}
