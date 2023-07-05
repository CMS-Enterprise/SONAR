using System;
using CommandLine;

namespace Cms.BatCave.Sonar.Options;

[Verb(VerbName, HelpText = "Initialize the database.")]
public class InitOptions : CommonOptions {
  public const String VerbName = "init";

  [Option('f', "force")]
  public Boolean Force { get; }

  public InitOptions(Boolean force, String appSettingsLocation) : base(appSettingsLocation) {
    this.Force = force;
  }
}
