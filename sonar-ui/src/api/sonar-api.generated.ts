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
  ApiKey,
  ApiKeyConfiguration,
  ApiKeyDetails,
  EnvironmentHealth,
  MetricData,
  ProblemDetails,
  ServiceHealth,
  ServiceHierarchyConfiguration,
  ServiceHierarchyHealth,
  TenantHealth,
  UptimeModel,
} from "./data-contracts";
import { ContentType, HttpClient, RequestParams } from "./http-client";

export class Api<SecurityDataType = unknown> extends HttpClient<SecurityDataType> {
  /**
   * No description
   *
   * @tags Admin
   * @name V2AdminInitializeCreate
   * @request POST:/api/v2/admin/initialize
   */
  v2AdminInitializeCreate = (
    query?: {
      confirmation?: string;
      /** @default false */
      force?: boolean;
    },
    params: RequestParams = {},
  ) =>
    this.request<void, any>({
      path: `/api/v2/admin/initialize`,
      method: "POST",
      query: query,
      ...params,
    });
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
   * @name V2KeysUpdate
   * @summary Updates configuration for existing API key.
   * @request PUT:/api/v2/keys
   */
  v2KeysUpdate = (data: ApiKeyConfiguration, params: RequestParams = {}) =>
    this.request<ApiKey, ProblemDetails | void>({
      path: `/api/v2/keys`,
      method: "PUT",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ApiKey
   * @name V2KeysDelete
   * @summary Deletes existing API key.
   * @request DELETE:/api/v2/keys
   */
  v2KeysDelete = (data: ApiKeyConfiguration, params: RequestParams = {}) =>
    this.request<ApiKey, ProblemDetails | void>({
      path: `/api/v2/keys`,
      method: "DELETE",
      body: data,
      type: ContentType.Json,
      format: "json",
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
    this.request<ServiceHierarchyConfiguration, any>({
      path: `/api/v2/config/${environment}/tenants/${tenant}`,
      method: "DELETE",
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
   * @tags Readiness
   * @name ReadyList
   * @request GET:/api/ready
   */
  readyList = (params: RequestParams = {}) =>
    this.request<void, any>({
      path: `/api/ready`,
      method: "GET",
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
   * @tags TestData
   * @name SaveData
   * @request POST:/api/v2/test/data
   */
  saveData = (data: MetricData, params: RequestParams = {}) =>
    this.request<void, any>({
      path: `/api/v2/test/data`,
      method: "POST",
      body: data,
      type: ContentType.Json,
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
