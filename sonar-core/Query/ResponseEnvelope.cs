using System;

namespace Cms.BatCave.Sonar.Query;

public record ResponseEnvelope<TData>(
  ResponseStatus Status,
  TData? Data,
  String? ErrorType,
  String? Error,
  String[]? Warnings);
