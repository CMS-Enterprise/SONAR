using System;
using CommandLine;

namespace Cms.BatCave.Sonar.Options;

[Verb(VerbName, HelpText = "Run the SONAR API web server.")]
public class ServeOptions : CommonOptions {
  public const String VerbName = "serve";

  public ServeOptions(String appSettingsLocation) : base(appSettingsLocation) {
  }
}
