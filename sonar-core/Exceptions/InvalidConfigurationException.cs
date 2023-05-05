using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Cms.BatCave.Sonar.Exceptions;

public class InvalidConfigurationException : Exception {

  private readonly ImmutableDictionary<String, Object?>? _data;

  public InvalidConfigurationException(String message)
    : base(message) {
  }

  public InvalidConfigurationException(String message, Exception innerException)
    : base(message, innerException) {
  }

  public InvalidConfigurationException(String message, IDictionary<String, Object?> data)
    : base(message) {

    this._data = data.ToImmutableDictionary();
  }

  public override IDictionary Data => this._data ?? ImmutableDictionary<String, Object?>.Empty;

}
