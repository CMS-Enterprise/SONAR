using System;

namespace Cms.BatCave.Sonar.Configuration;

public record DatabaseConfiguration(
  String Host,
  UInt16 Port = 5432,
  String Username = "root",
  String Password = "password",
  String Database = "sonar");
