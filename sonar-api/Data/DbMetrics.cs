using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.Metrics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Cms.BatCave.Sonar.Data;

public class DbMetrics : IDbCommandInterceptor {
  private readonly ILogger<DbMetrics> _logger;
  private static readonly ConcurrentDictionary<DbCommand, DateTime> CommandLookup = new();
  private static readonly Meter DbCommandMeter = new("Sonar.EntityFramework.DbCommands");
  private static readonly Counter<Int32> CommandExecutedMetric = DbCommandMeter.CreateCounter<Int32>("db_command_executed");
  private static readonly Counter<Double> ExecutionDurationMetric = DbCommandMeter.CreateCounter<Double>(
    "db_command_execution_time",
    "milliseconds"
  );
  private const String DbCommandTypeLabel = "db_command_type";
  private const String CommandTextLabel = "command_text";
  private const String IsInTransactionLabel = "is_in_transaction";
  private const String DatabaseLabel = "database";
  private const String IsAsyncLabel = "is_async";

  public DbMetrics(
    ILogger<DbMetrics> logger
  ) {
    this._logger = logger;
  }

  public InterceptionResult<DbDataReader> ReaderExecuting(
    DbCommand command,
    CommandEventData eventData,
    InterceptionResult<DbDataReader> result) {
    CommandLookup.TryAdd(command, DateTime.UtcNow);
    return result;
  }

  public ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
    DbCommand command,
    CommandEventData eventData,
    InterceptionResult<DbDataReader> result,
    CancellationToken cancellationToken = new CancellationToken()) {
    CommandLookup.TryAdd(command, DateTime.UtcNow);
    return ValueTask.FromResult(result);
  }

  public DbDataReader ReaderExecuted(
    DbCommand command,
    CommandExecutedEventData eventData,
    DbDataReader result) {
    this.AddCommandMetrics(command, "Reader", false);
    return result;
  }

  public ValueTask<DbDataReader> ReaderExecutedAsync(
    DbCommand command,
    CommandExecutedEventData eventData,
    DbDataReader result,
    CancellationToken cancellationToken = new CancellationToken()) {
    this.AddCommandMetrics(command, "Reader", true);
    return ValueTask.FromResult(result);
  }

  public InterceptionResult<DbDataReader> ScalarExecuting(
    DbCommand command,
    CommandEventData eventData,
    InterceptionResult<DbDataReader> result) {
    CommandLookup.TryAdd(command, DateTime.UtcNow);
    return result;
  }

  public ValueTask<InterceptionResult<DbDataReader>> ScalarExecutingAsync(
    DbCommand command,
    CommandEventData eventData,
    InterceptionResult<DbDataReader> result,
    CancellationToken cancellationToken = new CancellationToken()) {
    CommandLookup.TryAdd(command, DateTime.UtcNow);
    return ValueTask.FromResult(result);
  }

  public DbDataReader ScalarExecuted(
    DbCommand command,
    CommandExecutedEventData eventData,
    DbDataReader result) {
    this.AddCommandMetrics(command, "Scalar", false);
    return result;
  }

  public ValueTask<DbDataReader> ScalarExecutedAsync(
    DbCommand command,
    CommandExecutedEventData eventData,
    DbDataReader result,
    CancellationToken cancellationToken = new CancellationToken()) {
    this.AddCommandMetrics(command, "Scalar", true);
    return ValueTask.FromResult(result);
  }

  public InterceptionResult<DbDataReader> NonQueryExecuting(
    DbCommand command,
    CommandEventData eventData,
    InterceptionResult<DbDataReader> result) {
    CommandLookup.TryAdd(command, DateTime.UtcNow);
    return result;
  }

  public ValueTask<InterceptionResult<DbDataReader>> NonQueryExecutingAsync(
    DbCommand command,
    CommandEventData eventData,
    InterceptionResult<DbDataReader> result,
    CancellationToken cancellationToken = new CancellationToken()) {
    CommandLookup.TryAdd(command, DateTime.UtcNow);
    return ValueTask.FromResult(result);
  }

  public DbDataReader NonQueryExecuted(
    DbCommand command,
    CommandExecutedEventData eventData,
    DbDataReader result) {
    this.AddCommandMetrics(command, "NonQuery", false);
    return result;
  }

  public ValueTask<DbDataReader> NonQueryExecutedAsync(
    DbCommand command,
    CommandExecutedEventData eventData,
    DbDataReader result,
    CancellationToken cancellationToken = new CancellationToken()) {
    this.AddCommandMetrics(command, "NonQuery", true);
    return ValueTask.FromResult(result);
  }

  private void AddCommandMetrics(
    DbCommand command,
    String label,
    Boolean isAsync) {
    CommandExecutedMetric.Add(
      1,
      new KeyValuePair<String, Object?>(DbCommandTypeLabel, label),
      new KeyValuePair<String, Object?>(CommandTextLabel, command.CommandText),
      new KeyValuePair<String, Object?>(IsInTransactionLabel, command.Transaction != null),
      new KeyValuePair<String, Object?>(DatabaseLabel, command.Connection?.Database),
      new KeyValuePair<String, Object?>(IsAsyncLabel, isAsync)
    );

    if (CommandLookup.TryRemove(command, out var start)) {
      ExecutionDurationMetric.Add(
        DateTime.UtcNow.Subtract(start).TotalMilliseconds,
        new KeyValuePair<String, Object?>(DbCommandTypeLabel, label),
        new KeyValuePair<String, Object?>(CommandTextLabel, command.CommandText),
        new KeyValuePair<String, Object?>(IsInTransactionLabel, command.Transaction != null),
        new KeyValuePair<String, Object?>(DatabaseLabel, command.Connection?.Database),
        new KeyValuePair<String, Object?>(IsAsyncLabel, isAsync)
      );
    } else {
      this._logger.LogWarning($"{label} (UNKNOWN ID)");
    }
  }
}
