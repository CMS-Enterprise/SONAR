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

import {
  ApiKeyDetails,
  EnvironmentHealth,
  EnvironmentModel,
  MetricDataCollection,
  ProblemDetails,
  ServiceHealth,
  ServiceHealthData,
  ServiceHierarchyConfiguration,
  ServiceHierarchyHealth,
  ServiceHierarchyHealthHistory,
  TenantHealth,
  UptimeModel,
} from "./data-contracts";
import { ContentType, HttpClient, RequestParams } from "./http-client";

export class Api<SecurityDataType = unknown> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags ApiKey
   * @name V2KeysCreate
   * @summary Creates and records configuration for new API key.
   * @request POST:/api/v2/keys
   */
  v2KeysCreate = (data: ApiKeyDetails, params: RequestParams = {}) =>
    this.request<void, ProblemDetails>({
      path: `/api/v2/keys`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      ...params,
    });
  /**
   * No description
   *
   * @tags ApiKey
   * @name V2KeysList
   * @summary Get API keys.
   * @request GET:/api/v2/keys
   */
  v2KeysList = (params: RequestParams = {}) =>
    this.request<ApiKeyDetails[], ProblemDetails>({
      path: `/api/v2/keys`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ApiKey
   * @name DeleteApiKey
   * @summary Deletes existing API key.
   * @request DELETE:/api/v2/keys/{keyId}
   */
  deleteApiKey = (keyId: string, params: RequestParams = {}) =>
    this.request<void, ProblemDetails | void>({
      path: `/api/v2/keys/${keyId}`,
      method: "DELETE",
      ...params,
    });
  /**
   * No description
   *
   * @tags Configuration
   * @name GetTenant
   * @summary Retrieves the configuration for the specified environment and tenant.
   * @request GET:/api/v2/config/{environment}/tenants/{tenant}
   */
  getTenant = (environment: string, tenant: string, params: RequestParams = {}) =>
    this.request<ServiceHierarchyConfiguration, ProblemDetails>({
      path: `/api/v2/config/${environment}/tenants/${tenant}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Configuration
   * @name CreateTenant
   * @summary Sets the configuration for a new environment or tenant.
   * @request POST:/api/v2/config/{environment}/tenants/{tenant}
   */
  createTenant = (
    environment: string,
    tenant: string,
    data: ServiceHierarchyConfiguration,
    params: RequestParams = {},
  ) =>
    this.request<ServiceHierarchyConfiguration, ProblemDetails | void>({
      path: `/api/v2/config/${environment}/tenants/${tenant}`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Configuration
   * @name UpdateTenant
   * @summary Updates the configuration for an existing tenant.
   * @request PUT:/api/v2/config/{environment}/tenants/{tenant}
   */
  updateTenant = (
    environment: string,
    tenant: string,
    data: ServiceHierarchyConfiguration,
    params: RequestParams = {},
  ) =>
    this.request<ServiceHierarchyConfiguration, ProblemDetails | void>({
      path: `/api/v2/config/${environment}/tenants/${tenant}`,
      method: "PUT",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Configuration
   * @name DeleteTenant
   * @request DELETE:/api/v2/config/{environment}/tenants/{tenant}
   */
  deleteTenant = (environment: string, tenant: string, params: RequestParams = {}) =>
    this.request<void, any>({
      path: `/api/v2/config/${environment}/tenants/${tenant}`,
      method: "DELETE",
      ...params,
    });
  /**
   * No description
   *
   * @tags Environment
   * @name CreateEnvironment
   * @request POST:/api/v2/environments
   */
  createEnvironment = (data: EnvironmentModel, params: RequestParams = {}) =>
    this.request<EnvironmentModel, ProblemDetails>({
      path: `/api/v2/environments`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Environment
   * @name GetEnvironments
   * @request GET:/api/v2/environments
   */
  getEnvironments = (params: RequestParams = {}) =>
    this.request<EnvironmentHealth[], ProblemDetails>({
      path: `/api/v2/environments`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Environment
   * @name GetEnvironment
   * @request GET:/api/v2/environments/{environment}
   */
  getEnvironment = (environment: string, params: RequestParams = {}) =>
    this.request<EnvironmentHealth, ProblemDetails>({
      path: `/api/v2/environments/${environment}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * @description Service health status information must be recorded in chronological order per-service, and cannot be recorded for timestamps older than 2 hours. Timestamps greater than 2 hours will result in an "out of bounds" error. Health status that is reported out of order will result in an "out of order sample" error.
   *
   * @tags Health
   * @name RecordStatus
   * @summary Records a single health status for the specified service.
   * @request POST:/api/v2/health/{environment}/tenants/{tenant}/services/{service}
   */
  recordStatus = (
    environment: string,
    tenant: string,
    service: string,
    data: ServiceHealth,
    params: RequestParams = {},
  ) =>
    this.request<void, ProblemDetails | void>({
      path: `/api/v2/health/${environment}/tenants/${tenant}/services/${service}`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      ...params,
    });
  /**
   * No description
   *
   * @tags Health
   * @name GetSonarHealth
   * @request GET:/api/v2/health/{environment}/tenants/sonar
   */
  getSonarHealth = (environment: string, params: RequestParams = {}) =>
    this.request<ServiceHierarchyHealth[], ProblemDetails>({
      path: `/api/v2/health/${environment}/tenants/sonar`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Health
   * @name GetServiceHierarchyHealth
   * @request GET:/api/v2/health/{environment}/tenants/{tenant}
   */
  getServiceHierarchyHealth = (environment: string, tenant: string, params: RequestParams = {}) =>
    this.request<ServiceHierarchyHealth[], ProblemDetails | void>({
      path: `/api/v2/health/${environment}/tenants/${tenant}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Health
   * @name GetSpecificServiceHierarchyHealth
   * @request GET:/api/v2/health/{environment}/tenants/{tenant}/services/{servicePath}
   */
  getSpecificServiceHierarchyHealth = (
    environment: string,
    tenant: string,
    servicePath: string,
    params: RequestParams = {},
  ) =>
    this.request<ServiceHierarchyHealth[], ProblemDetails | void>({
      path: `/api/v2/health/${environment}/tenants/${tenant}/services/${servicePath}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
 * No description
 *
 * @tags HealthCheckData
 * @name RecordHealthCheckData
 * @summary Record the given raw Cms.BatCave.Sonar.Models.ServiceHealthData time series samples for the given environment,
tenant, and service in Prometheus. Filters out stale and out-of-order samples prior to calling P8s.
 * @request POST:/api/v2/health-check-data/{environment}/tenants/{tenant}/services/{service}
 */
  recordHealthCheckData = (
    environment: string,
    tenant: string,
    service: string,
    data: ServiceHealthData,
    params: RequestParams = {},
  ) =>
    this.request<ServiceHealthData, ProblemDetails | void>({
      path: `/api/v2/health-check-data/${environment}/tenants/${tenant}/services/${service}`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
 * No description
 *
 * @tags HealthCheckData
 * @name GetHealthCheckData
 * @summary Retrieves the given raw Cms.BatCave.Sonar.Models.ServiceHealthData time series samples for the given environment,
tenant, service, and health check in Prometheus. Filters out samples outside of the given start and end date time
(or if those are not given, filters out samples from more than 10 minutes ago UTC) prior to calling P8s.
 * @request GET:/api/v2/health-check-data/{environment}/tenants/{tenant}/services/{service}/health-check/{healthCheck}
 */
  getHealthCheckData = (
    environment: string,
    tenant: string,
    service: string,
    healthCheck: string,
    query?: {
      /** @format date-time */
      queryStart?: string;
      /** @format date-time */
      queryEnd?: string;
    },
    params: RequestParams = {},
  ) =>
    this.request<MetricDataCollection, ProblemDetails>({
      path: `/api/v2/health-check-data/${environment}/tenants/${tenant}/services/${service}/health-check/${healthCheck}`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags HealthHistory
   * @name GetServicesHealthHistory
   * @summary Get the health history for all services within the specified Tenant.
   * @request GET:/api/v2/health-history/{environment}/tenants/{tenant}
   */
  getServicesHealthHistory = (
    environment: string,
    tenant: string,
    query?: {
      /**
       * The queries first evaluation time.  The start and end time cannot be greater
       * than 24 hours (default is current time)
       * @format date-time
       */
      start?: string;
      /**
       * The queries evaluation time stops on or before this time.  The start and end time
       * cannot be greater than 24 hours (default is current time minus 1 hour)
       * @format date-time
       */
      end?: string;
      /**
       * The number of seconds that is incremented on each step.  Step cannot be greater
       * than 3600 (default 30)
       * @format int32
       */
      step?: number;
    },
    params: RequestParams = {},
  ) =>
    this.request<ServiceHierarchyHealthHistory[], ProblemDetails | void>({
      path: `/api/v2/health-history/${environment}/tenants/${tenant}`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * @description Get the health history for a specific service and its children.
   *
   * @tags HealthHistory
   * @name GetServiceHealthHistory
   * @summary Get the health history for a specific service, specified by its path in the service hierarchy.
   * @request GET:/api/v2/health-history/{environment}/tenants/{tenant}/services/{servicePath}
   */
  getServiceHealthHistory = (
    environment: string,
    tenant: string,
    servicePath: string,
    query?: {
      /**
       * The queries first evaluation time.  The start and end time cannot be greater
       * than 24 hours (default is current time)
       * @format date-time
       */
      start?: string;
      /**
       * The queries evaluation time stops on or before this time.  The start and end time
       * cannot be greater than 24 hours (default is current time minus 1 hour)
       * @format date-time
       */
      end?: string;
      /**
       * The number of seconds that is incremented on each step.  Step cannot be greater
       * than 3600 (default 30)
       * @format int32
       */
      step?: number;
    },
    params: RequestParams = {},
  ) =>
    this.request<ServiceHierarchyHealthHistory, ProblemDetails | void>({
      path: `/api/v2/health-history/${environment}/tenants/${tenant}/services/${servicePath}`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Tenant
   * @name GetTenants
   * @request GET:/api/v2/tenants
   */
  getTenants = (params: RequestParams = {}) =>
    this.request<TenantHealth[], ProblemDetails>({
      path: `/api/v2/tenants`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Uptime
   * @name GetTotalUptime
   * @request GET:/api/v2/uptime/{environment}/tenants/{tenant}/services/{servicePath}
   */
  getTotalUptime = (
    environment: string,
    tenant: string,
    servicePath: string,
    query?: {
      threshold?: string;
      /** @format date-time */
      start?: string;
      /** @format date-time */
      end?: string;
    },
    params: RequestParams = {},
  ) =>
    this.request<UptimeModel[], ProblemDetails | void>({
      path: `/api/v2/uptime/${environment}/tenants/${tenant}/services/${servicePath}`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
}
