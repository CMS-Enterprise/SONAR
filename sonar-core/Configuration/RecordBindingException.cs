using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Cms.BatCave.Sonar.Configuration;

public class RecordBindingException : Exception {
  public Type RecordType { get; }
  public ConstructorInfo Constructor { get; }
  public ICollection<ParameterInfo> MissingParameters { get; }

  public RecordBindingException(
    Type recordType,
    ConstructorInfo constructor,
    ICollection<ParameterInfo> missingParameters) : base(GenerateMessage(recordType, missingParameters)) {

    this.RecordType = recordType;
    this.Constructor = constructor;
    this.MissingParameters = missingParameters;
  }

  private static String GenerateMessage(
    Type recordType,
    ICollection<ParameterInfo> missingParameters) {

    var missingParameterStr =
      String.Join(", ", missingParameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
    return $"Unable to construct {recordType.Name} from provided configuration. Missing parameters: {missingParameterStr}";
  }
}
