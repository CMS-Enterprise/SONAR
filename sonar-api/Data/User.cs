using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Cms.BatCave.Sonar.Data;

[Table("User")]
[Index(nameof(Email), IsUnique = true)]
public class User {
  [Key]
  public Guid Id { get; init; }
  public String Email { get; init; }
  public String FirstName { get; set; }
  public String LastName { get; set; }

  public User(
    Guid id,
    String email,
    String firstName,
    String lastName) {

    this.Id = id;
    this.Email = email;
    this.FirstName = firstName;
    this.LastName = lastName;
  }

}
