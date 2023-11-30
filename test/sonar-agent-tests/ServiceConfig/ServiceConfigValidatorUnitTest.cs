using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Cms.BatCave.Sonar.Agent.ServiceConfig;
using Cms.BatCave.Sonar.Enumeration;
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

  [Theory]
  [InlineData("prometheus.json")]
  [InlineData("loki.json")]
  [InlineData("http-status.json")]
  [InlineData("http-response-time.json")]
  [InlineData("http-jsonbody.json")]
  [InlineData("http-xmlbody.json")]
  [InlineData("http-body-nonmatch.json")]
  public async Task HealthCheckConditionWithMaintenanceStatus_ThrowsInvalidConfigurationException(String configFile) {
    var configSource = new LocalFileServiceConfigSource(
      tenant: "test",
      filePaths: new[] { "test-inputs/invalid-service-config-maintenance-status-" + configFile }
    );

    var configs = await configSource.GetConfigurationLayersAsync(tenant: "test", cancellationToken: default)
      .ToArrayAsync();

    var ex = Assert.Throws<InvalidConfigurationException>(() => ServiceConfigValidator.ValidateServiceConfig(configs[0]));

    Assert.NotNull(ex.Data["errors"]);
    var errors = Assert.IsType<List<ValidationResult>>(ex.Data["errors"]);
    Assert.Single(errors);
    var error = errors[0];
    Assert.StartsWith("Services[0].HealthChecks[0].Definition.Conditions[0]", error.ErrorMessage);
    Assert.Contains(nameof(HealthStatus.Maintenance), error.ErrorMessage);
  }

}
