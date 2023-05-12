using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Cms.BatCave.Sonar.Models.Validation;

public class RecursivePropertyValidator : IRecursivePropertyValidator {

  /// <inheritdoc/>
  public Boolean TryValidateObjectProperties(Object obj, ICollection<ValidationResult> validationResults) {
    return this.TryValidateObjectPropertiesRecursive(obj, String.Empty, validationResults, new HashSet<Object>());
  }

  private Boolean TryValidateObjectPropertiesRecursive(
    Object obj,
    String objPath,
    ICollection<ValidationResult> validationResults,
    ISet<Object> validatedObjs) {

    if (!validatedObjs.Add(obj)) {
      return true;
    }

    var objContext = new ValidationContext(obj);
    var objResults = new List<ValidationResult>();
    var isValid = Validator.TryValidateObject(obj, objContext, objResults, validateAllProperties: true);

    if (!isValid) {
      var resultPrefix = String.IsNullOrEmpty(objPath) ? String.Empty : $"{objPath}.";
      foreach (var objValidationResult in objResults) {
        validationResults.Add(new ValidationResult(
          $"{resultPrefix}{objValidationResult.MemberNames.FirstOrDefault()}: {objValidationResult.ErrorMessage}",
          objValidationResult.MemberNames));
      }
    }

    var propertyInfos = obj.GetType().GetProperties()
      .Where(propertyInfo =>
        propertyInfo.CanRead &&
        (propertyInfo.GetIndexParameters().Length == 0) &&
        !propertyInfo.PropertyType.IsValueType &&
        (propertyInfo.PropertyType != typeof(String)))
      .ToList();

    foreach (var propertyInfo in propertyInfos) {
      var childObj = propertyInfo.GetValue(obj);

      if (childObj == null) {
        continue;
      }

      var childObjPath = String.IsNullOrEmpty(objPath) ? propertyInfo.Name : $"{objPath}.{propertyInfo.Name}";

      if (childObj is IEnumerable<Object> childObjs) {
        foreach (var (c, p) in childObjs.Select((childObjI, i) => (childObjI, $"{childObjPath}[{i}]"))) {
          Recurse(c, p);
        }
      } else {
        Recurse(childObj, childObjPath);
      }

      void Recurse(Object c, String p) {
        if (!this.TryValidateObjectPropertiesRecursive(c, p, validationResults, validatedObjs)) {
          isValid = false;
        }
      }
    }

    return isValid;
  }
}
