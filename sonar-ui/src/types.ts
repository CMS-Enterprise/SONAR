import { HealthCheckDefinition } from "api/data-contracts"

// TODO BATAPI-238
export interface IHealthCheckDefinition extends HealthCheckDefinition {
  duration: string,
  expression: string,
  conditions: IHealthCheckCondition[] | IHealthCheckHttpCondition[]
}

export interface IHealthCheckCondition {
  operator?: string,
  threshold?: number,
  status: string
}

export interface IHealthCheckHttpCondition {
  statusCodes?: number[],
  responseTime?: string,

  status: string,
  type?: string

}