using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;

namespace Cms.BatCave.Sonar.Exceptions;

public class InvalidConfigurationException : Exception {
  public InvalidConfigurationErrorType ErrorType { get; }

  private readonly ImmutableDictionary<String, Object?>? _data;

  public InvalidConfigurationException(String message, InvalidConfigurationErrorType errorType)
    : base(message) {
    this.ErrorType = errorType;
  }

  public InvalidConfigurationException(
    String message,
    InvalidConfigurationErrorType errorType,
    Exception innerException)
    : base(message, innerException) {

    this.ErrorType = errorType;
  }

  public InvalidConfigurationException(
    String message,
    InvalidConfigurationErrorType errorType,
    IDictionary<String, Object?> data)
    : base(message) {

    this.ErrorType = errorType;
    this._data = data.ToImmutableDictionary();
  }

  protected InvalidConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context) {
    this.ErrorType = (InvalidConfigurationErrorType)info.GetInt32(nameof(InvalidConfigurationException.ErrorType));
    this._data =
      ImmutableDictionary.CreateRange(
        base.Data.Keys.Cast<Object>()
          .Where(key => key is String)
          .Select(key => new KeyValuePair<String, Object?>((String)key, base.Data[key]))
      );
  }

  public override void GetObjectData(SerializationInfo info, StreamingContext context) {
    // Copy our Data to serialized version of Data
    if (this._data != null) {
      foreach (var kvp in this._data) {
        base.Data[kvp.Key] = kvp.Value;
      }
    }

    base.GetObjectData(info, context);

    info.AddValue(nameof(InvalidConfigurationException.ErrorType), (Int32)this.ErrorType);
  }

  public override IDictionary Data => this._data ?? ImmutableDictionary<String, Object?>.Empty;

}
