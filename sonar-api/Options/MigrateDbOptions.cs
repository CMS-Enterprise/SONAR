using System;
using CommandLine;

namespace Cms.BatCave.Sonar.Options;

[Verb(VerbName, HelpText = HelpText)]
public class MigrateDbOptions : CommonOptions {
  public const String VerbName = "migratedb";
  public const String HelpText = "Apply pending migrations to the existing database.";

  [Option(
    shortName: 'r',
    longName: "re-create",
    HelpText = "First drop the existing database, then re-create it and apply all migrations.")]
  public Boolean ReCreate { get; }

  public MigrateDbOptions(Boolean reCreate, String? appSettingsLocation)
    : base(appSettingsLocation) {
    this.ReCreate = reCreate;
  }
}
