using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Exceptions;
using Cms.BatCave.Sonar.Models;
using Cms.BatCave.Sonar.Models.Validation;

namespace Cms.BatCave.Sonar.Agent.ServiceConfig;

public class ServiceConfigValidator {
  public static void ValidateServiceConfig(ServiceHierarchyConfiguration serviceConfig) {
    var validator = new RecursivePropertyValidator();
    var validationResults = new List<ValidationResult>();
    var isValid = validator.TryValidateObjectProperties(serviceConfig, validationResults);

    if (!isValid) {
      throw new InvalidConfigurationException(
        message: "Invalid JSON service configuration: One or more validation errors occurred.",
        new Dictionary<String, Object?> { ["errors"] = validationResults });
    }
  }
}
