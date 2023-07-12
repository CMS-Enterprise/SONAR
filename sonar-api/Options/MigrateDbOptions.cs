using System;
using CommandLine;

namespace Cms.BatCave.Sonar.Options;

[Verb(VerbName, HelpText = HelpText)]
public class MigrateDbOptions : CommonOptions {
  public const String VerbName = "migratedb";
  public const String HelpText = "Apply migrations to the existing database.";

  [Option(
    shortName: 'r',
    longName: "re-create",
    HelpText = "First drop the existing database, then re-create it and apply migrations.")]
  public Boolean ReCreate { get; }

  [Option(
    shortName: 't',
    longName: "target",
    HelpText = "Migrate the database to the given target migration, instead of applying all migrations.")]
  public String? TargetMigration { get; }

  public MigrateDbOptions(Boolean reCreate, String? targetMigration, String? appSettingsLocation)
    : base(appSettingsLocation) {
    this.ReCreate = reCreate;
    this.TargetMigration = targetMigration;
  }
}
