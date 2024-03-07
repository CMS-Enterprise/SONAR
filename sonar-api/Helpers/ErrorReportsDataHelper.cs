using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Data;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Extensions;
using Cms.BatCave.Sonar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cms.BatCave.Sonar.Helpers;

public class ErrorReportsDataHelper {
  private readonly DbSet<ErrorReport> _errorReportTable;
  private readonly DbSet<Tenant> _tenantsTable;
  private readonly ILogger<ErrorReportsDataHelper> _logger;

  public ErrorReportsDataHelper(
    DbSet<ErrorReport> errorReportTable,
    DbSet<Tenant> tenantsTable,
    ILogger<ErrorReportsDataHelper> logger) {

    this._errorReportTable = errorReportTable;
    this._tenantsTable = tenantsTable;
    this._logger = logger;
  }

  public async Task<ErrorReport> AddErrorReportAsync(
    ErrorReport errorReport,
    CancellationToken cancellationToken) {

    var result = await this._errorReportTable.AddAsync(
      errorReport,
      cancellationToken);

    return result.Entity;
  }

  public async Task<List<ErrorReportDetails>> GetFilteredErrorReportDetailsByEnvironment(
    Guid environmentId,
    Guid? tenantId,
    String? serviceName,
    String? healthCheckName,
    AgentErrorLevel? level,
    AgentErrorType? errorType,
    DateTime start,
    DateTime end,
    CancellationToken cancellationToken) {
    var query = this._errorReportTable
      .LeftJoin(
        this._tenantsTable,
        leftKeySelector: er => er.TenantId,
        rightKeySelector: t => t.Id,
        resultSelector: (er, t) => new { ErrorReport = er, Tenant = t }
      )
      .Where(r =>
        r.ErrorReport.EnvironmentId == environmentId)
      .Where(r =>
        (r.ErrorReport.Timestamp > start) && (r.ErrorReport.Timestamp <= end));

    // build query with optional params
    if (tenantId != null) {
      query = query.Where(r => r.ErrorReport.TenantId == tenantId);
    }

    if (!String.IsNullOrEmpty(serviceName)) {
      query = query.Where(r => r.ErrorReport.ServiceName == serviceName);
    }

    if (!String.IsNullOrEmpty(healthCheckName)) {
      query = query.Where(r => r.ErrorReport.HealthCheckName == healthCheckName);
    }

    if (level != null) {
      query = query.Where(r => r.ErrorReport.Level == level);
    }

    if (errorType != null) {
      query = query.Where(r => r.ErrorReport.Type == errorType);
    }

    var result = await query.ToListAsync(cancellationToken);
    return result.Select(ep => new ErrorReportDetails(
      ep.ErrorReport.Timestamp,
      ep.Tenant?.Name,
      ep.ErrorReport.ServiceName,
      ep.ErrorReport.HealthCheckName,
      ep.ErrorReport.Level,
      ep.ErrorReport.Type,
      ep.ErrorReport.Message,
      ep.ErrorReport.Configuration,
      ep.ErrorReport.StackTrace)).ToList();
  }

  public void LogErrorReport(LogLevel logLevel, ErrorReport? report, String? environment, String? tenant) {
    this._logger.Log(
      logLevel,
      "Environment:{Environment}, TenantName:{Tenant}, ServiceName:{ServiceName}, " +
      "HealthCheckName:{HealthCheckName}, AgentErrorLevel:{Level}, AgentErrorType:{Type}, Message:{Message}, " +
      "Configuration:{Configuration}, StackTrace:{StackTrace}",
      environment,
      tenant,
      report?.ServiceName,
      report?.HealthCheckName,
      report?.Level,
      report?.Type,
      report?.Message,
      report?.Configuration,
      report?.StackTrace);
  }
}
