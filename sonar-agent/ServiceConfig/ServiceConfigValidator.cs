using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Models.Validation;

namespace Cms.BatCave.Sonar.Agent.ServiceConfig;

public static class ServiceConfigValidator {
  private static readonly RecursivePropertyValidator Validator = new();

  public static void ValidateServiceConfig(
    ServiceHierarchyConfiguration serviceConfig) {
    var validationResults = new List<ValidationResult>();
    var isValid = Validator.TryValidateObjectProperties(serviceConfig, validationResults);

    if (!isValid) {
      throw new InvalidConfigurationException(
        message: "Invalid JSON service configuration: One or more validation errors occurred.",
        InvalidConfigurationErrorType.DataValidationError,
        new Dictionary<String, Object?> { ["errors"] = validationResults });
    }
  }
}
