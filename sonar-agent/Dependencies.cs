using System;
using Cms.BatCave.Sonar.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cms.BatCave.Sonar.Agent;

public class Dependencies {
  public RecordOptionsManager<TOptions> CreateRecordOptions<TOptions>(
    IConfigurationRoot configRoot,
    String configSection,
    ILoggerFactory loggerFactory) where TOptions : class {

    return new RecordOptionsManager<TOptions>(
      configRoot.GetSection(configSection),
      loggerFactory.CreateLogger<RecordOptionsManager<TOptions>>());
  }
}
