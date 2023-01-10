using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;

namespace Cms.BatCave.Sonar.Agent.Options;

public class InitSettings {
  [Value(0,  HelpText = "AppSetting file Location.")]
  public String AppSettingLocation { get; set; }

  [Value(1, Min = 1, HelpText = "Service Config Files.")]
  public IEnumerable<String> ServiceConfigFiles { get; set; }
}




