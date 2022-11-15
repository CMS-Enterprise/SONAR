using Cms.BatCave.Sonar.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Tests;

public class TestDataContext : DataContext {
  public TestDataContext(
    IOptions<TestDatabaseConfiguration> configuration,
    ILoggerFactory loggerFactory) :
    base(configuration, loggerFactory) {
  }
}
