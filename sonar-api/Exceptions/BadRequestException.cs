using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;

namespace Cms.BatCave.Sonar.Exceptions;

public class BadRequestException : ProblemDetailException {
  public const String DefaultProblemTypeName = "BadRequest";

  private readonly ImmutableDictionary<String, Object?> _data;
  public override String ProblemType { get; }

  public override IDictionary Data => this._data;

  public BadRequestException(String message) : this(message, BadRequestException.DefaultProblemTypeName) {
    this._data = ImmutableDictionary<String, Object?>.Empty;
  }

  public BadRequestException(String message, String problemType) : base(HttpStatusCode.BadRequest, message) {
    this.ProblemType = problemType;
    this._data = ImmutableDictionary<String, Object?>.Empty;
  }

  public BadRequestException(String message, String problemType, IDictionary<String, Object?> data) : base(HttpStatusCode.BadRequest, message) {
    this.ProblemType = problemType;
    this._data = ImmutableDictionary.CreateRange(data);
  }

  public BadRequestException(SerializationInfo info, StreamingContext context) : base(info, context) {
    this.ProblemType = info.GetString(nameof(this.ProblemType)) ?? BadRequestException.DefaultProblemTypeName;
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

    info.AddValue(nameof(this.ProblemType), this.ProblemType);
  }

  protected override IDictionary<String, Object?> GetExtensions() {
    return this._data;
  }
}
