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
  private static readonly Regex GuidLiteralRegex =
    new("'[a-z0-9]{8}(-[a-z0-9]{4}){3}-[a-z0-9]{12}'", RegexOptions.Compiled);
  private static readonly Regex GuidArrayLiteralRegex =
    new(
      "'[a-z0-9]{8}(-[a-z0-9]{4}){3}-[a-z0-9]{12}'(, '[a-z0-9]{8}(-[a-z0-9]{4}){3}-[a-z0-9]{12}')*",
      RegexOptions.Compiled
    );

  private static readonly Meter DbCommandMeter = new("Sonar.EntityFramework.DbCommands");

  private static readonly Counter<Int32> CommandExecutedMetric =
    DbCommandMeter.CreateCounter<Int32>("db_command_executed");

  private static readonly Counter<Double> ExecutionDurationMetric = DbCommandMeter.CreateCounter<Double>(
    "db_command_execution_time",
    "milliseconds"
  );

  private const String DbCommandTypeLabel = "db_command_type";
  private const String CommandTextLabel = "command_text";
  private const String IsInTransactionLabel = "is_in_transaction";
  private const String DatabaseLabel = "database";
  private const String IsAsyncLabel = "is_async";

  public DbDataReader ReaderExecuted(
    DbCommand command,
    CommandExecutedEventData eventData,
    DbDataReader result) {
    AddCommandMetrics(command, eventData, label: "Reader", isAsync: false);
    return result;
  }

  public ValueTask<DbDataReader> ReaderExecutedAsync(
    DbCommand command,
    CommandExecutedEventData eventData,
    DbDataReader result,
    CancellationToken cancellationToken = new CancellationToken()) {
    AddCommandMetrics(command, eventData, label: "Reader", isAsync: true);
    return ValueTask.FromResult(result);
  }

  public DbDataReader ScalarExecuted(
    DbCommand command,
    CommandExecutedEventData eventData,
    DbDataReader result) {
    AddCommandMetrics(command, eventData, label: "Scalar", isAsync: false);
    return result;
  }

  public ValueTask<DbDataReader> ScalarExecutedAsync(
    DbCommand command,
    CommandExecutedEventData eventData,
    DbDataReader result,
    CancellationToken cancellationToken = new CancellationToken()) {
    AddCommandMetrics(command, eventData, label: "Scalar", isAsync: true);
    return ValueTask.FromResult(result);
  }

  public DbDataReader NonQueryExecuted(
    DbCommand command,
    CommandExecutedEventData eventData,
    DbDataReader result) {
    AddCommandMetrics(command, eventData, label: "NonQuery", isAsync: false);
    return result;
  }

  public ValueTask<DbDataReader> NonQueryExecutedAsync(
    DbCommand command,
    CommandExecutedEventData eventData,
    DbDataReader result,
    CancellationToken cancellationToken = new CancellationToken()) {
    AddCommandMetrics(command, eventData, label: "NonQuery", isAsync: true);
    return ValueTask.FromResult(result);
  }

  private static void AddCommandMetrics(
    DbCommand command,
    CommandExecutedEventData eventData,
    String label,
    Boolean isAsync) {

    // Clean up command text
    var cmdText = GuidLiteralRegex.Replace(GuidArrayLiteralRegex.Replace(command.CommandText, "@array"), "@guid");

    CommandExecutedMetric.Add(
      1,
      new KeyValuePair<String, Object?>(DbCommandTypeLabel, label),
      new KeyValuePair<String, Object?>(CommandTextLabel, cmdText),
      new KeyValuePair<String, Object?>(IsInTransactionLabel, command.Transaction != null),
      new KeyValuePair<String, Object?>(DatabaseLabel, command.Connection?.Database),
      new KeyValuePair<String, Object?>(IsAsyncLabel, isAsync)
    );

    ExecutionDurationMetric.Add(
      eventData.Duration.TotalMilliseconds,
      new KeyValuePair<String, Object?>(DbCommandTypeLabel, label),
      new KeyValuePair<String, Object?>(CommandTextLabel, cmdText),
      new KeyValuePair<String, Object?>(IsInTransactionLabel, command.Transaction != null),
      new KeyValuePair<String, Object?>(DatabaseLabel, command.Connection?.Database),
      new KeyValuePair<String, Object?>(IsAsyncLabel, isAsync)
    );
  }
}
