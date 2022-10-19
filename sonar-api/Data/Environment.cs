using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Cms.BatCave.Sonar.Data;

[Table("environment")]
[Index(propertyNames: new[] { nameof(Name) }, IsUnique = true)]
public class Environment {
  public Guid Id { get; init; }

  [StringLength(100)]
  public String Name { get; init; }

  public Environment(
    Guid id,
    String name) {

    this.Id = id;
    this.Name = name;
  }
}
