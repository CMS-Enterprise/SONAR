using System;
using System.Linq;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.ServiceConfig;
using Cms.BatCave.Sonar.Exceptions;
using Xunit;

namespace Cms.BatCave.Sonar.Agent.Tests.ServiceConfig;

public class ServiceConfigValidatorUnitTest {

  [Theory]
  [InlineData("test-inputs/invalid-service-config-empty-health-check-definition.json")]
  public async Task EmptyHealthCheckDefinition_ThrowsInvalidConfigurationException(String configFile) {
    var configSource = new LocalFileServiceConfigSource(tenant: "test", filePaths: new[] { configFile });
    var configs = await configSource.GetConfigurationLayersAsync(tenant: "test", cancellationToken: default)
      .ToArrayAsync();

    Assert.Throws<InvalidConfigurationException>(() => ServiceConfigValidator.ValidateServiceConfig(configs[0]));
  }

}
