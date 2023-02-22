using System;
using System.Collections.Generic;
using CommandLine;

namespace Cms.BatCave.Sonar.Agent.Options;

public class SonarAgentOptions {

  [Option("appsettings-location", HelpText = "AppSettings file Location.", Default = ".")]
  public String AppSettingsLocation { get; }
  [Option("kubernetes-configuration", Required = false, HelpText = "Kubernetes Configuration Enabled.", Default = false)]
  public Boolean KubernetesConfigurationOption { get; }
  [Option('f', "files", Required = false, HelpText = "Service Config Files.")]
  public IEnumerable<String> ServiceConfigFiles { get; }

  public SonarAgentOptions(String appSettingsLocation, Boolean kubernetesConfigurationOption, IEnumerable<String> serviceConfigFiles) {
    this.AppSettingsLocation = appSettingsLocation;
    this.KubernetesConfigurationOption = kubernetesConfigurationOption;
    this.ServiceConfigFiles = serviceConfigFiles;
  }
}
