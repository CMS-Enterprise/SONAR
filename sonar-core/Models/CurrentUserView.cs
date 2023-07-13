using System;

namespace Cms.BatCave.Sonar.Models;

public record CurrentUserView(
  String FirstName,
  String LastName,
  String Email
);
