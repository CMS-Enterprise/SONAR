using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cms.BatCave.Sonar.Models.Validation;

public interface IRecursivePropertyValidator {

  /// <summary>
  /// Validates the given object and it's child objects recursively. Only recurses into <c>get</c>-able reference-type
  /// properties; value-type properties and non-property data members are ignored for recursion (but their direct
  /// constraints are validated). Any validation errors found are added to <paramref name="validationResults"/>.
  /// </summary>
  /// <remarks>
  /// This validator exists because System.ComponentModel.DataAnnotations.<see cref="Validator"/> doesn't handle
  /// object trees, it only validates attributes directly on it's target instance, not it's children. So this validator
  /// provides that recursion. This validator is primarily geared toward the property-bag type API model classes in the
  /// <c>Cms.BatCave.Sonar.Models</c> namespace; it assumes the models expose their data members as properties only,
  /// and this should be kept in mind when adding data members to these models in the future.
  /// </remarks>
  /// <param name="obj">The object to validate.</param>
  /// <param name="validationResults">A collection to hold each failed validation.</param>
  /// <returns>True if the object has no validation errors, false if it does.</returns>
  public Boolean TryValidateObjectProperties(Object obj, ICollection<ValidationResult> validationResults);

}
