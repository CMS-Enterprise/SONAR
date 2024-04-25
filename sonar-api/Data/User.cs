using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Cms.BatCave.Sonar.Data;

[Table("user")]
[Index(nameof(Email), IsUnique = true)]
public class User {
  [Key]
  public Guid Id { get; init; }
  public String Email { get; init; }
  public String FullName { get; set; }

  public User(
    Guid id,
    String email,
    String fullName) {

    this.Id = id;
    this.Email = email;
    this.FullName = fullName;
  }

  public static User New(
    String email,
    String fullName) =>
    new User(
      Guid.Empty,
      email,
      fullName);

}
