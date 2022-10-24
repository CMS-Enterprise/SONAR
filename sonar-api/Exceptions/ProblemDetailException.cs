using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;

namespace Cms.BatCave.Sonar.Exceptions;

public abstract class ProblemDetailException : Exception {
  public HttpStatusCode Status { get; }

  public abstract String ErrorType { get; }

  public ProblemDetailException(HttpStatusCode status, String message) : base(message) {
    this.Status = status;
  }

  protected ProblemDetailException(SerializationInfo info, StreamingContext context) : base(info, context) {
    this.Status = (HttpStatusCode)info.GetInt32(nameof(this.Status));
  }

  public override void GetObjectData(SerializationInfo info, StreamingContext context) {
    base.GetObjectData(info, context);
    info.AddValue(nameof(this.Status), this.Status);
  }

  public virtual IDictionary<String, Object> GetExtensions() {
    return new Dictionary<String, Object>();
  }
}
