using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;

namespace Cms.BatCave.Sonar.Exceptions;

public class ResourceNotFoundException : ProblemDetailException {
  private const String IdTypeKey = "IdType";
  public String TypeName { get; }
  public Object ResourceId { get; }

  public override String ErrorType => "ResourceNotFound";

  public ResourceNotFoundException(String typeName, Object resourceId) :
    base(HttpStatusCode.NotFound, $"{typeName} Not Found") {

    this.TypeName = typeName;
    this.ResourceId = resourceId;
  }

  protected ResourceNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) {
    this.TypeName =
      info.GetString(nameof(this.TypeName)) ??
      throw new InvalidDataException($"Required property not found: {nameof(this.TypeName)}");

    var idType = Type.GetType(
      info.GetString(IdTypeKey) ??
      throw new InvalidDataException($"Required property not found: {IdTypeKey}")
    );
    if (idType == null) {
      throw new InvalidDataException($"Unsupported Id Type: {info.GetString(IdTypeKey)}");
    }

    this.ResourceId = info.GetValue(nameof(this.ResourceId), idType) ??
      throw new InvalidDataException($"Required property not found: {nameof(this.ResourceId)}");
  }

  public override void GetObjectData(SerializationInfo info, StreamingContext context) {
    base.GetObjectData(info, context);
    info.AddValue(nameof(this.TypeName), this.TypeName);
    info.AddValue(IdTypeKey, this.ResourceId.GetType().FullName);
    info.AddValue(nameof(this.ResourceId), this.ResourceId);
  }

  protected override IDictionary<String, Object?> GetExtensions() {
    return new Dictionary<String, Object?> {
      { "resourceId", this.ResourceId }
    };
  }
}
