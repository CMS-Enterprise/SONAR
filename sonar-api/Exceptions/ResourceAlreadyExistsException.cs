using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using System.Reflection;

namespace Cms.BatCave.Sonar.Exceptions;

public class ResourceAlreadyExistsException : ProblemDetailException {
  public const String ProblemTypeName = "ResourceAlreadyExists";

  private readonly ImmutableDictionary<String, Object?> _extensions;

  public override String ProblemType => ResourceAlreadyExistsException.ProblemTypeName;

  public ResourceAlreadyExistsException(MemberInfo type, Object conflictingProperties)
    : base(HttpStatusCode.Conflict, $"{type.Name} with the given properties already exists") {
    this._extensions = new Dictionary<String, Object?> {
      ["conflictingProperties"] = conflictingProperties.GetType().GetProperties().ToImmutableDictionary(
        keySelector: p => p.Name,
        elementSelector: p => p.GetValue(conflictingProperties, index: null))
    }.ToImmutableDictionary();
  }

  protected override IDictionary<String, Object?> GetExtensions() => this._extensions;
}
