using System;
using CommandLine;

namespace Cms.BatCave.Sonar.Options;

public class CommonOptions {
  [Option("appsettings-location", HelpText = "AppSettings file location.")]
  public String? AppSettingsLocation { get; }

  protected CommonOptions(String? appSettingsLocation) {
    this.AppSettingsLocation = appSettingsLocation;
  }
}
