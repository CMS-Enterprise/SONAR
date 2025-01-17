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
  ActiveAdHocMaintenanceView,
  ActiveScheduledMaintenanceView,
  AdHocMaintenanceConfiguration,
  AgentErrorLevel,
  AgentErrorType,
  AlertSilenceDetails,
  ApiKeyConfiguration,
  ApiKeyDetails,
  CurrentUserView,
  DateTimeHealthStatusValueTuple,
  EnvironmentHealth,
  EnvironmentModel,
  ErrorReportDetails,
  HealthCheckHistory,
  MetricDataCollection,
  PermissionConfiguration,
  PermissionDetails,
  ProblemDetails,
  ServiceAlert,
  ServiceHealth,
  ServiceHealthData,
  ServiceHierarchyConfiguration,
  ServiceHierarchyHealth,
  ServiceHierarchyHealthHistory,
  ServiceVersion,
  ServiceVersionDetails,
  ServiceVersionHistory,
  TenantInfo,
  UptimeModel,
  UserPermissionsView,
} from "./data-contracts";
import { ContentType, HttpClient, RequestParams } from "./http-client";

export class Api<SecurityDataType = unknown> extends HttpClient<SecurityDataType> {
  /**
 * No description
 *
 * @tags ApiKey
 * @name V2KeysCreate
 * @summary Creates and records configuration for new API key. The user performing this request must have sufficient
permissions to the requested environment/tenant.
 * @request POST:/api/v2/keys
 */
  v2KeysCreate = (data: ApiKeyDetails, params: RequestParams = {}) =>
    this.request<ApiKeyConfiguration, ProblemDetails>({
      path: `/api/v2/keys`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
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
    this.request<ApiKeyConfiguration[], ProblemDetails>({
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
   * @summary Fetch a list of all environments and their current sonar aggregate health status.
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
   * @name UpdateEnvironment
   * @summary Update environment.
   * @request PUT:/api/v2/environments/{environment}
   */
  updateEnvironment = (environment: string, data: EnvironmentModel, params: RequestParams = {}) =>
    this.request<EnvironmentModel, ProblemDetails | void>({
      path: `/api/v2/environments/${environment}`,
      method: "PUT",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Environment
   * @name GetEnvironment
   * @summary Fetch a single environment's current sonar aggregate health status.
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
   * No description
   *
   * @tags Environment
   * @name DeleteEnvironment
   * @request DELETE:/api/v2/environments/{environment}
   */
  deleteEnvironment = (environment: string, params: RequestParams = {}) =>
    this.request<void, ProblemDetails>({
      path: `/api/v2/environments/${environment}`,
      method: "DELETE",
      ...params,
    });
  /**
   * No description
   *
   * @tags Environment
   * @name GetEnvironmentsView
   * @summary Fetch a list of all environments without health data
   * @request GET:/api/v2/environments/view
   */
  getEnvironmentsView = (params: RequestParams = {}) =>
    this.request<EnvironmentModel[], ProblemDetails>({
      path: `/api/v2/environments/view`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ErrorReports
   * @name CreateErrorReport
   * @request POST:/api/v2/error-reports/{environment}
   */
  createErrorReport = (environment: string, data: ErrorReportDetails, params: RequestParams = {}) =>
    this.request<ErrorReportDetails, ProblemDetails>({
      path: `/api/v2/error-reports/${environment}`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ErrorReports
   * @name ListErrorReports
   * @request GET:/api/v2/error-reports/{environment}
   */
  listErrorReports = (
    environment: string,
    query?: {
      serviceName?: string;
      healthCheckName?: string;
      errorLevel?: AgentErrorLevel;
      errorType?: AgentErrorType;
      /** @format date-time */
      start?: string;
      /** @format date-time */
      end?: string;
    },
    params: RequestParams = {},
  ) =>
    this.request<ErrorReportDetails[], ProblemDetails | void>({
      path: `/api/v2/error-reports/${environment}`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ErrorReports
   * @name ListErrorReportsForTenant
   * @request GET:/api/v2/error-reports/{environment}/tenants/{tenant}
   */
  listErrorReportsForTenant = (
    environment: string,
    tenant: string,
    query?: {
      serviceName?: string;
      healthCheckName?: string;
      errorLevel?: AgentErrorLevel;
      errorType?: AgentErrorType;
      /** @format date-time */
      start?: string;
      /** @format date-time */
      end?: string;
    },
    params: RequestParams = {},
  ) =>
    this.request<ErrorReportDetails[], ProblemDetails | void>({
      path: `/api/v2/error-reports/${environment}/tenants/${tenant}`,
      method: "GET",
      query: query,
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
   * @request GET:/api/v2/health/{environment}/tenants/sonar-internal
   */
  getSonarHealth = (environment: string, params: RequestParams = {}) =>
    this.request<ServiceHierarchyHealth[], ProblemDetails>({
      path: `/api/v2/health/${environment}/tenants/sonar-internal`,
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
   * @tags HealthCheckHistory
   * @name GetHealthCheckResultForService
   * @summary Retrieves the instantaneous prometheus health status for each healthcheck given the service at a specific time.
   * @request GET:/api/v2/health-check-history/{environment}/tenants/{tenant}/services/{service}/health-check-result
   */
  getHealthCheckResultForService = (
    environment: string,
    tenant: string,
    service: string,
    query?: {
      /** @format date-time */
      timeQuery?: string;
    },
    params: RequestParams = {},
  ) =>
    this.request<Record<string, DateTimeHealthStatusValueTuple>, ProblemDetails | void>({
      path: `/api/v2/health-check-history/${environment}/tenants/${tenant}/services/${service}/health-check-result`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
  /**
 * No description
 *
 * @tags HealthCheckHistory
 * @name GetHealthCheckResultsForService
 * @summary Retrieves the prometheus health status time series for each healthcheck given the
service. Filters out samples outside of the given start and end date time.
 * @request GET:/api/v2/health-check-history/{environment}/tenants/{tenant}/services/{service}/health-check-results
 */
  getHealthCheckResultsForService = (
    environment: string,
    tenant: string,
    service: string,
    query?: {
      /** @format date-time */
      start?: string;
      /** @format date-time */
      end?: string;
      /** @format int32 */
      step?: number;
    },
    params: RequestParams = {},
  ) =>
    this.request<HealthCheckHistory, ProblemDetails>({
      path: `/api/v2/health-check-history/${environment}/tenants/${tenant}/services/${service}/health-check-results`,
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
   * @tags Maintenance
   * @name GetActiveScheduledEnvironmentMaintenance
   * @request GET:/api/v2/maintenance/environments/scheduled
   */
  getActiveScheduledEnvironmentMaintenance = (params: RequestParams = {}) =>
    this.request<ActiveScheduledMaintenanceView[], any>({
      path: `/api/v2/maintenance/environments/scheduled`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Maintenance
   * @name GetActiveScheduledTenantMaintenance
   * @request GET:/api/v2/maintenance/tenants/scheduled
   */
  getActiveScheduledTenantMaintenance = (params: RequestParams = {}) =>
    this.request<ActiveScheduledMaintenanceView[], any>({
      path: `/api/v2/maintenance/tenants/scheduled`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Maintenance
   * @name GetActiveScheduledServiceMaintenance
   * @request GET:/api/v2/maintenance/services/scheduled
   */
  getActiveScheduledServiceMaintenance = (params: RequestParams = {}) =>
    this.request<ActiveScheduledMaintenanceView[], any>({
      path: `/api/v2/maintenance/services/scheduled`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Maintenance
   * @name GetActiveAdHocEnvironmentMaintenance
   * @request GET:/api/v2/maintenance/environments/ad-hoc
   */
  getActiveAdHocEnvironmentMaintenance = (params: RequestParams = {}) =>
    this.request<ActiveAdHocMaintenanceView[], any>({
      path: `/api/v2/maintenance/environments/ad-hoc`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Maintenance
   * @name GetActiveAdHocTenantMaintenance
   * @request GET:/api/v2/maintenance/tenants/ad-hoc
   */
  getActiveAdHocTenantMaintenance = (params: RequestParams = {}) =>
    this.request<ActiveAdHocMaintenanceView[], any>({
      path: `/api/v2/maintenance/tenants/ad-hoc`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Maintenance
   * @name GetActiveAdHocServiceMaintenance
   * @request GET:/api/v2/maintenance/services/ad-hoc
   */
  getActiveAdHocServiceMaintenance = (params: RequestParams = {}) =>
    this.request<ActiveAdHocMaintenanceView[], any>({
      path: `/api/v2/maintenance/services/ad-hoc`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Maintenance
   * @name ToggleAdhocEnvironmentMaintenance
   * @request PUT:/api/v2/maintenance/{environment}/ad-hoc
   */
  toggleAdhocEnvironmentMaintenance = (
    environment: string,
    data: AdHocMaintenanceConfiguration,
    params: RequestParams = {},
  ) =>
    this.request<ActiveAdHocMaintenanceView, ProblemDetails>({
      path: `/api/v2/maintenance/${environment}/ad-hoc`,
      method: "PUT",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Maintenance
   * @name ToggleAdhocTenantMaintenance
   * @request PUT:/api/v2/maintenance/{environment}/tenants/{tenant}/ad-hoc
   */
  toggleAdhocTenantMaintenance = (
    environment: string,
    tenant: string,
    data: AdHocMaintenanceConfiguration,
    params: RequestParams = {},
  ) =>
    this.request<ActiveAdHocMaintenanceView, ProblemDetails>({
      path: `/api/v2/maintenance/${environment}/tenants/${tenant}/ad-hoc`,
      method: "PUT",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Maintenance
   * @name ToggleAdhocServiceMaintenance
   * @request PUT:/api/v2/maintenance/{environment}/tenants/{tenant}/services/{service}/ad-hoc
   */
  toggleAdhocServiceMaintenance = (
    environment: string,
    tenant: string,
    service: string,
    data: AdHocMaintenanceConfiguration,
    params: RequestParams = {},
  ) =>
    this.request<ActiveAdHocMaintenanceView, ProblemDetails>({
      path: `/api/v2/maintenance/${environment}/tenants/${tenant}/services/${service}/ad-hoc`,
      method: "PUT",
      body: data,
      type: ContentType.Json,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ServiceAlert
   * @name GetServiceAlerts
   * @request GET:/api/v2/alerts/{environment}/tenants/{tenant}/services/{servicePath}
   */
  getServiceAlerts = (environment: string, tenant: string, servicePath: string, params: RequestParams = {}) =>
    this.request<ServiceAlert[], ProblemDetails>({
      path: `/api/v2/alerts/${environment}/tenants/${tenant}/services/${servicePath}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags ServiceAlert
   * @name CreateUpdateSilence
   * @request POST:/api/v2/alerts/silences/{environment}/tenants/{tenant}/services/{servicePath}
   */
  createUpdateSilence = (
    environment: string,
    tenant: string,
    servicePath: string,
    data: AlertSilenceDetails,
    params: RequestParams = {},
  ) =>
    this.request<void, ProblemDetails>({
      path: `/api/v2/alerts/silences/${environment}/tenants/${tenant}/services/${servicePath}`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      ...params,
    });
  /**
   * No description
   *
   * @tags ServiceAlert
   * @name RemoveSilence
   * @request PUT:/api/v2/alerts/silences/{environment}/tenants/{tenant}/services/{servicePath}
   */
  removeSilence = (
    environment: string,
    tenant: string,
    servicePath: string,
    data: AlertSilenceDetails,
    params: RequestParams = {},
  ) =>
    this.request<void, ProblemDetails>({
      path: `/api/v2/alerts/silences/${environment}/tenants/${tenant}/services/${servicePath}`,
      method: "PUT",
      body: data,
      type: ContentType.Json,
      ...params,
    });
  /**
 * No description
 *
 * @tags Tenant
 * @name GetTenants
 * @summary Fetch tenant health. Query parameters may be supplied to query by environment and/or tenant. This endpoint
will return all tenants for all environments if query params aren't supplied.
 * @request GET:/api/v2/tenants
 */
  getTenants = (
    query?: {
      /** @default "" */
      environment?: string;
      /** @default "" */
      tenant?: string;
    },
    params: RequestParams = {},
  ) =>
    this.request<TenantInfo[], ProblemDetails>({
      path: `/api/v2/tenants`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Tenant
   * @name GetTenantsView
   * @summary Fetch a list of tenants without health data
   * @request GET:/api/v2/tenants/view
   */
  getTenantsView = (
    query?: {
      /** @default "" */
      environment?: string;
      /** @default "" */
      tenant?: string;
    },
    params: RequestParams = {},
  ) =>
    this.request<TenantInfo[], ProblemDetails>({
      path: `/api/v2/tenants/view`,
      method: "GET",
      query: query,
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
  /**
   * No description
   *
   * @tags User
   * @name V2UserCreate
   * @request POST:/api/v2/user
   */
  v2UserCreate = (params: RequestParams = {}) =>
    this.request<CurrentUserView, ProblemDetails>({
      path: `/api/v2/user`,
      method: "POST",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags User
   * @name V2UserList
   * @request GET:/api/v2/user
   */
  v2UserList = (params: RequestParams = {}) =>
    this.request<CurrentUserView[], any>({
      path: `/api/v2/user`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags User
   * @name GetUserPermissionTree
   * @request GET:/api/v2/user/permission-tree
   */
  getUserPermissionTree = (params: RequestParams = {}) =>
    this.request<UserPermissionsView, ProblemDetails>({
      path: `/api/v2/user/permission-tree`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
 * No description
 *
 * @tags UserPermissions
 * @name V2PermissionsCreate
 * @summary Create a user permission. The user performing the request must have sufficient permissions
to the requested environment/tenant.
 * @request POST:/api/v2/permissions
 */
  v2PermissionsCreate = (data: PermissionDetails, params: RequestParams = {}) =>
    this.request<void, ProblemDetails>({
      path: `/api/v2/permissions`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      ...params,
    });
  /**
   * No description
   *
   * @tags UserPermissions
   * @name GetPermissions
   * @summary Get permissions the current user has access to.
   * @request GET:/api/v2/permissions
   */
  getPermissions = (params: RequestParams = {}) =>
    this.request<PermissionConfiguration[], ProblemDetails | void>({
      path: `/api/v2/permissions`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags UserPermissions
   * @name DeleteUserPermission
   * @summary Delete a user permission.
   * @request DELETE:/api/v2/permissions/{permissionId}
   */
  deleteUserPermission = (permissionId: string, params: RequestParams = {}) =>
    this.request<void, ProblemDetails | void>({
      path: `/api/v2/permissions/${permissionId}`,
      method: "DELETE",
      ...params,
    });
  /**
   * No description
   *
   * @tags UserPermissions
   * @name UpdateUserPermission
   * @summary Update a user permission.
   * @request PUT:/api/v2/permissions/{permissionId}
   */
  updateUserPermission = (permissionId: string, data: PermissionDetails, params: RequestParams = {}) =>
    this.request<void, ProblemDetails | void>({
      path: `/api/v2/permissions/${permissionId}`,
      method: "PUT",
      body: data,
      type: ContentType.Json,
      ...params,
    });
  /**
   * No description
   *
   * @tags UserPermissions
   * @name GetCurrentUser
   * @summary Get permissions of the current user.
   * @request GET:/api/v2/permissions/me
   */
  getCurrentUser = (params: RequestParams = {}) =>
    this.request<PermissionConfiguration[], ProblemDetails | void>({
      path: `/api/v2/permissions/me`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags Version
   * @name RecordServiceVersion
   * @summary Records a single version for the specified service.
   * @request POST:/api/v2/version/{environment}/tenants/{tenant}/services/{service}
   */
  recordServiceVersion = (
    environment: string,
    tenant: string,
    service: string,
    data: ServiceVersion,
    params: RequestParams = {},
  ) =>
    this.request<void, ProblemDetails | void>({
      path: `/api/v2/version/${environment}/tenants/${tenant}/services/${service}`,
      method: "POST",
      body: data,
      type: ContentType.Json,
      ...params,
    });
  /**
   * No description
   *
   * @tags Version
   * @name GetSpecificServiceVersionDetails
   * @request GET:/api/v2/version/{environment}/tenants/{tenant}/services/{servicePath}
   */
  getSpecificServiceVersionDetails = (
    environment: string,
    tenant: string,
    servicePath: string,
    params: RequestParams = {},
  ) =>
    this.request<ServiceVersionDetails[], ProblemDetails | void>({
      path: `/api/v2/version/${environment}/tenants/${tenant}/services/${servicePath}`,
      method: "GET",
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags VersionHistory
   * @name GetServicesVersionHistory
   * @summary Get the version history for all services within the specified Tenant.
   * @request GET:/api/v2/version-history/{environment}/tenants/{tenant}
   */
  getServicesVersionHistory = (
    environment: string,
    tenant: string,
    query?: {
      /**
       * How far back in time values should be fetched (in seconds).
       * @format int32
       */
      duration?: number;
      /**
       * The timestamp at which to sample data.
       * @format date-time
       */
      timeQuery?: string;
    },
    params: RequestParams = {},
  ) =>
    this.request<ServiceVersionHistory[], ProblemDetails>({
      path: `/api/v2/version-history/${environment}/tenants/${tenant}`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
  /**
   * No description
   *
   * @tags VersionHistory
   * @name GetServiceVersionHistory
   * @summary Get the version history for a specific Service, specified by its path in the service hierarchy.
   * @request GET:/api/v2/version-history/{environment}/tenants/{tenant}/services/{servicePath}
   */
  getServiceVersionHistory = (
    environment: string,
    tenant: string,
    servicePath: string,
    query?: {
      /**
       * How far back in time values should be fetched (in seconds).
       * @format int32
       */
      duration?: number;
      /**
       * The timestamp at which to sample data.
       * @format date-time
       */
      timeQuery?: string;
    },
    params: RequestParams = {},
  ) =>
    this.request<ServiceVersionHistory, ProblemDetails>({
      path: `/api/v2/version-history/${environment}/tenants/${tenant}/services/${servicePath}`,
      method: "GET",
      query: query,
      format: "json",
      ...params,
    });
}
