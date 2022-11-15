using System;
using Cms.BatCave.Sonar.Configuration;

namespace Cms.BatCave.Sonar.Tests;

public record TestDatabaseConfiguration(
  String Host,
  UInt16 Port = 5432,
  String Username = "root",
  String Password = "password",
  String Database = "sonar",
  Boolean DbLogging = false) : DatabaseConfiguration(Host, Port, Username, Password, Database, DbLogging);
