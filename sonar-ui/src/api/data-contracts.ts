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

export interface ApiKeyConfiguration {
  /** @format uuid */
  id?: string;
  /**
   * @minLength 0
   * @maxLength 44
   */
  apiKey: string;
  apiKeyType: PermissionType;
  environment?: string | null;
  tenant?: string | null;
  /** @format date-time */
  creation?: string;
  /** @format date-time */
  lastUsage?: string;
}

export interface ApiKeyDetails {
  apiKeyType: PermissionType;
  environment?: string | null;
  tenant?: string | null;
}

export interface CurrentUserView {
  fullName?: string | null;
  email?: string | null;
  isAdmin?: boolean;
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
export type DateTimeHealthStatusValueTuple = (string | HealthStatus)[];

export interface EnvironmentHealth {
  /** @minLength 1 */
  environmentName: string;
  /** @format date-time */
  timestamp?: string | null;
  aggregateStatus?: HealthStatus;
}

export interface EnvironmentModel {
  /**
   * @minLength 0
   * @maxLength 100
   * @pattern ^[0-9a-zA-Z_-]+$
   */
  name: string;
}

export type HealthCheckDefinition = object;

export interface HealthCheckModel {
  /**
   * @minLength 0
   * @maxLength 100
   * @pattern ^[0-9a-zA-Z_-]+$
   */
  name: string;
  description?: string | null;
  type: HealthCheckType;
  definition: HealthCheckDefinition;
}

export enum HealthCheckType {
  PrometheusMetric = "PrometheusMetric",
  LokiMetric = "LokiMetric",
  HttpRequest = "HttpRequest",
  Internal = "Internal",
}

export enum HealthStatus {
  Unknown = "Unknown",
  Online = "Online",
  AtRisk = "AtRisk",
  Degraded = "Degraded",
  Offline = "Offline",
}

export interface MetricDataCollection {
  timeSeries: DateTimeDoubleValueTuple[];
}

export interface PermissionConfiguration {
  /** @format uuid */
  id?: string;
  permission?: PermissionType;
  userEmail?: string | null;
  environment?: string | null;
  tenant?: string | null;
}

export interface PermissionDetails {
  permission: PermissionType;
  /** @minLength 1 */
  userEmail: string;
  environment?: string | null;
  tenant?: string | null;
}

export enum PermissionType {
  Admin = "Admin",
  Standard = "Standard",
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
  /**
   * @minLength 0
   * @maxLength 100
   * @pattern ^[0-9a-zA-Z_-]+$
   */
  name: string;
  /** @minLength 1 */
  displayName: string;
  description?: string | null;
  /** @format uri */
  url?: string | null;
  healthChecks?: HealthCheckModel[] | null;
  children?: string[] | null;
}

export interface ServiceHealth {
  /** @format date-time */
  timestamp: string;
  aggregateStatus: HealthStatus;
  healthChecks: Record<string, HealthStatus>;
}

export interface ServiceHealthData {
  healthCheckSamples: Record<string, DateTimeDoubleValueTuple[]>;
  /** @format int32 */
  totalHealthChecks?: number;
  /** @format int32 */
  totalSamples?: number;
}

export interface ServiceHierarchyConfiguration {
  services: ServiceConfiguration[];
  rootServices: string[];
}

export interface ServiceHierarchyHealth {
  /** @minLength 1 */
  name: string;
  /** @minLength 1 */
  displayName: string;
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
  /** @minLength 1 */
  name: string;
  /** @minLength 1 */
  displayName: string;
  description?: string | null;
  /** @format uri */
  url?: string | null;
  aggregateStatus?: DateTimeHealthStatusValueTuple[] | null;
  children?: ServiceHierarchyHealthHistory[] | null;
}

export interface TenantHealth {
  /** @minLength 1 */
  environmentName: string;
  /** @minLength 1 */
  tenantName: string;
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
  /** @minLength 1 */
  name: string;
  /** @format double */
  percentUptime: number;
  totalUptime: TimeSpan;
  currentUptime: TimeSpan;
  unknownDuration: TimeSpan;
  children: UptimeModel[];
}

export interface UserPermissionsView {
  permissionTree?: Record<string, string[]>;
}
