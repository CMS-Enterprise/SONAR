import {
  DateTimeHealthStatusValueTuple,
  ServiceHierarchyHealth
} from '../api/data-contracts';

export interface StatusHistoryView {
  serviceData: ServiceHierarchyHealth,
  servicePath: string,
  statusTimestampTuple: DateTimeHealthStatusValueTuple,
  rangeInSeconds: number
}
