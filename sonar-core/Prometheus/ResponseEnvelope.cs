using System;

namespace Cms.BatCave.Sonar.Prometheus;

public record ResponseEnvelope<TData>(
  ResponseStatus Status,
  TData? Data,
  String? ErrorType,
  String? Error,
  String[]? Warnings);
