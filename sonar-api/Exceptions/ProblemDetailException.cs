using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using System.Runtime.Serialization;
using Cms.BatCave.Sonar.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Cms.BatCave.Sonar.Exceptions;

public abstract class ProblemDetailException : Exception {
  public HttpStatusCode Status { get; }

  public abstract String ProblemType { get; }

  protected ProblemDetailException(HttpStatusCode status, String message) : base(message) {
    this.Status = status;
  }

  protected ProblemDetailException(SerializationInfo info, StreamingContext context) : base(info, context) {
    this.Status = (HttpStatusCode)info.GetInt32(nameof(this.Status));
  }

  public override void GetObjectData(SerializationInfo info, StreamingContext context) {
    base.GetObjectData(info, context);
    info.AddValue(nameof(this.Status), this.Status);
  }

  public ProblemDetails ToProblemDetails() {
    var detail = new ProblemDetails {
      Status = (Int32)this.Status,
      Title = this.Message,
      Type = this.ProblemType
    };
    foreach (var kvp in this.GetExtensions()) {
      detail.Extensions.Add(kvp.Key.ToCamelCase(), kvp.Value);
    }

    return detail;
  }

  protected virtual IDictionary<String, Object?> GetExtensions() {
    return ImmutableDictionary<String, Object?>.Empty;
  }
}
