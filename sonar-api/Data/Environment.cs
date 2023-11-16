using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.EntityFrameworkCore;

namespace Cms.BatCave.Sonar.Data;

[Table("environment")]
[Index(nameof(Name), IsUnique = true)]
public class Environment {
  public Guid Id { get; init; }

  [StringLength(100)]
  public String Name { get; init; }

  public Boolean IsNonProd { get; set; }

  public Environment(
    Guid id,
    String name,
    Boolean isNonProd = false) {

    this.Id = id;
    this.Name = name;
    this.IsNonProd = isNonProd;
  }

  public static Environment New(String name, Boolean isNonProduction = false) => new Environment(Guid.Empty, name, isNonProduction);
}
