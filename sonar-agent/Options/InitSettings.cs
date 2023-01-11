using System;
using System.Collections.Generic;
using CommandLine;

namespace Cms.BatCave.Sonar.Agent.Options;

public class InitSettings {
  //[Value(0,  HelpText = "AppSetting file Location.")]
  [Option("appsetting-location", HelpText = "AppSetting file Location.", Default = ".")]
  public String AppSettingLocation { get; set; }

  [Value(0, Min = 1, HelpText = "Service Config Files.")]
  public IEnumerable<String> ServiceConfigFiles { get; set; }
}
