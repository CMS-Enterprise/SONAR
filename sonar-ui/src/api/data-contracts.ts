/* eslint-disable */
/* tslint:disable */
/*
 * ---------------------------------------------------------------
 * ## THIS FILE WAS GENERATED VIA SWAGGER-TYPESCRIPT-API        ##
 * ##                                                           ##
 * ## AUTHOR: acacode                                           ##
 * ## SOURCE: https://github.com/acacode/swagger-typescript-api ##
 * ---------------------------------------------------------------
 */

export interface ApiKey {
  /**
   * @minLength 0
   * @maxLength 44
   */
  key?: string | null;
  type?: ApiKeyType;
  /** @format uuid */
  environmentId?: string | null;
  /** @format uuid */
  tenantId?: string | null;
}

export interface ApiKeyConfiguration {
  apiKey?: string | null;
  apiKeyType?: ApiKeyType;
  environment?: string | null;
  tenant?: string | null;
}

export interface ApiKeyDetails {
  apiKeyType?: ApiKeyType;
  environment?: string | null;
  tenant?: string | null;
}

export enum ApiKeyType {
  Admin = "Admin",
  Standard = "Standard",
}

/**
 * @maxItems 2
 * @minItems 2
 */
export type DateTimeDoubleValueTuple = (string | number)[];

/**
 * @maxItems 2
 * @minItems 2
 */
export type DateTimeHealthStatusValueTuple = [string, HealthStatus];

export interface EnvironmentHealth {
  environmentName?: string | null;
  /** @format date-time */
  timestamp?: string | null;
  aggregateStatus?: HealthStatus;
}

export type HealthCheckDefinition = object;

export interface HealthCheckModel {
  name?: string | null;
  description?: string | null;
  type?: HealthCheckType;
  definition?: HealthCheckDefinition;
}

export enum HealthCheckType {
  PrometheusMetric = "PrometheusMetric",
  LokiMetric = "LokiMetric",
  HttpRequest = "HttpRequest",
}

export enum HealthStatus {
  Unknown = "Unknown",
  Online = "Online",
  AtRisk = "AtRisk",
  Degraded = "Degraded",
  Offline = "Offline",
}

export interface ProblemDetails {
  type?: string | null;
  title?: string | null;
  /** @format int32 */
  status?: number | null;
  detail?: string | null;
  instance?: string | null;
  [key: string]: any;
}

export interface ServiceConfiguration {
  name?: string | null;
  displayName?: string | null;
  description?: string | null;
  /** @format uri */
  url?: string | null;
  healthChecks?: HealthCheckModel[] | null;
  children?: string[] | null;
}

export interface ServiceHealth {
  /** @format date-time */
  timestamp?: string;
  aggregateStatus?: HealthStatus;
  healthChecks?: Record<string, HealthStatus>;
}

export interface ServiceHealthData {
  healthCheckSamples?: Record<string, DateTimeDoubleValueTuple[]>;
  /** @format int32 */
  totalHealthChecks?: number;
  /** @format int32 */
  totalSamples?: number;
}

export interface ServiceHierarchyConfiguration {
  services?: ServiceConfiguration[] | null;
  rootServices?: string[] | null;
}

export interface ServiceHierarchyHealth {
  name?: string | null;
  displayName?: string | null;
  description?: string | null;
  /** @format uri */
  url?: string | null;
  /** @format date-time */
  timestamp?: string | null;
  aggregateStatus?: HealthStatus;
  healthChecks?: Record<string, DateTimeHealthStatusValueTuple>;
  children?: ServiceHierarchyHealth[] | null;
}

export interface ServiceHierarchyHealthHistory {
  name?: string | null;
  displayName?: string | null;
  description?: string | null;
  /** @format uri */
  url?: string | null;
  aggregateStatus?: DateTimeHealthStatusValueTuple[] | null;
  children?: ServiceHierarchyHealthHistory[] | null;
}

export interface TenantHealth {
  environmentName?: string | null;
  tenantName?: string | null;
  /** @format date-time */
  timestamp?: string | null;
  aggregateStatus?: HealthStatus;
  rootServices?: ServiceHierarchyHealth[] | null;
}

export interface TimeSpan {
  /** @format int64 */
  ticks?: number;
  /** @format int32 */
  days?: number;
  /** @format int32 */
  hours?: number;
  /** @format int32 */
  milliseconds?: number;
  /** @format int32 */
  microseconds?: number;
  /** @format int32 */
  nanoseconds?: number;
  /** @format int32 */
  minutes?: number;
  /** @format int32 */
  seconds?: number;
  /** @format double */
  totalDays?: number;
  /** @format double */
  totalHours?: number;
  /** @format double */
  totalMilliseconds?: number;
  /** @format double */
  totalMicroseconds?: number;
  /** @format double */
  totalNanoseconds?: number;
  /** @format double */
  totalMinutes?: number;
  /** @format double */
  totalSeconds?: number;
}

export interface UptimeModel {
  name?: string | null;
  /** @format double */
  percentUptime?: number;
  totalUptime?: TimeSpan;
  currentUptime?: TimeSpan;
  unknownDuration?: TimeSpan;
  children?: UptimeModel[] | null;
}
