using System;
using System.Collections.Generic;
using CommandLine;

namespace Cms.BatCave.Sonar.Agent.Options;

public class SonarAgentOptions {

  [Option("appsettings-location", HelpText = "AppSettings file Location.", Default = ".")]
  public String AppSettingsLocation { get; }

  [Value(0, Min = 1, HelpText = "Service Config Files.")]
  public IEnumerable<String> ServiceConfigFiles { get; }

  public SonarAgentOptions(String appSettingsLocation, IEnumerable<String> serviceConfigFiles) {
    this.AppSettingsLocation = appSettingsLocation;
    this.ServiceConfigFiles = serviceConfigFiles;
  }
}
