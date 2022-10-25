using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;

namespace Cms.BatCave.Sonar.Exceptions;

public class BadRequestException : ProblemDetailException {
  private readonly ImmutableDictionary<String, Object?> _data;
  public override String ErrorType => "BadRequest";

  public override IDictionary Data => this._data;

  public BadRequestException(String message) : base(HttpStatusCode.BadRequest, message) {
    this._data = ImmutableDictionary<String, Object?>.Empty;
  }

  public BadRequestException(String message, IDictionary<String, Object?> data) : base(HttpStatusCode.BadRequest, message) {
    this._data = ImmutableDictionary.CreateRange(data);
  }

  public BadRequestException(SerializationInfo info, StreamingContext context) : base(info, context) {
    this._data =
      ImmutableDictionary.CreateRange(
        base.Data.Keys.Cast<Object>()
          .Where(key => key is String)
          .Select(key => new KeyValuePair<String, Object?>((String)key, base.Data[key]))
      );
  }

  public override void GetObjectData(SerializationInfo info, StreamingContext context) {
    // Copy our Data to serialized version of Data
    foreach (var kvp in this._data) {
      base.Data[kvp.Key] = kvp.Value;
    }

    base.GetObjectData(info, context);
  }

  protected override IDictionary<String, Object?> GetExtensions() {
    return this._data;
  }
}
