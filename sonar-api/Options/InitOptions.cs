using System;
using CommandLine;

namespace Cms.BatCave.Sonar.Options;

[Verb("init", HelpText = "Initialize the database.")]
public class InitOptions : CommonOptions {
  [Option('f', "force" )]
  public Boolean Force { get; }

  public InitOptions(Boolean force) {
    this.Force = force;
  }
}
