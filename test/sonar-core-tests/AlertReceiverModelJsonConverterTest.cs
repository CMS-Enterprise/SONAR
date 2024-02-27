using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using Cms.BatCave.Sonar.Enumeration;
using Cms.BatCave.Sonar.Models;
using Xunit;
using Xunit.Abstractions;

namespace Cms.BatCave.Sonar.Tests;

public class AlertReceiverModelJsonConverterTest {
  private readonly ITestOutputHelper _testOutputHelper;

  public AlertReceiverModelJsonConverterTest(ITestOutputHelper testOutputHelper) {
    this._testOutputHelper = testOutputHelper;
  }

  [Fact]
  public void Serialize_Deserialize_Alerting_Success() {
    AlertingConfiguration alerting = new AlertingConfiguration(null!) {
      Receivers = ImmutableArray.Create<AlertReceiverConfiguration>(
        new AlertReceiverConfiguration("receiver1", AlertReceiverType.Email, new AlertReceiverOptionsEmail("user1@gmail.com")),
        new AlertReceiverConfiguration("receiver2", AlertReceiverType.Email, new AlertReceiverOptionsEmail("user2@gmail.com"))
      )
    };

    String json = JsonSerializer.Serialize<AlertingConfiguration>(alerting);
    var alerting2 = JsonSerializer.Deserialize<AlertingConfiguration>(json);
    Assert.NotNull(alerting2);
    Assert.Equal(alerting.Receivers.Count, alerting2.Receivers.Count);
    var result = alerting.Receivers.IntersectBy(alerting2.Receivers.Select(x => x.Name), x => x.Name);
    Assert.NotNull(result);
    Assert.Equal(2, result.Count());
  }

  [Fact]
  public void Serialize_Deserialize_Alerting_Missing() {
    AlertingConfiguration alerting = new AlertingConfiguration(null!) {
      Receivers = ImmutableArray.Create<AlertReceiverConfiguration>(
        new AlertReceiverConfiguration("receiver1", AlertReceiverType.Email, new AlertReceiverOptionsEmail("user1@gmail.com")),
        new AlertReceiverConfiguration("receiver2", AlertReceiverType.Email, new AlertReceiverOptionsEmail("user2@gmail.com"))
      )
    };

    AlertingConfiguration alerting2 = new AlertingConfiguration(null!) {
      Receivers = ImmutableArray.Create<AlertReceiverConfiguration>(
        new AlertReceiverConfiguration("receiver1", AlertReceiverType.Email, new AlertReceiverOptionsEmail("user1@gmail.com")),
        new AlertReceiverConfiguration("receiver2", AlertReceiverType.Email, new AlertReceiverOptionsEmail("user2@gmail.com")),
        new AlertReceiverConfiguration("receiver3", AlertReceiverType.Email, new AlertReceiverOptionsEmail("user3@gmail.com"))
      )
    };

    String json = JsonSerializer.Serialize<AlertingConfiguration>(alerting);
    String json2 = JsonSerializer.Serialize<AlertingConfiguration>(alerting2);
    var alerting3 = JsonSerializer.Deserialize<AlertingConfiguration>(json2);
    Assert.NotNull(alerting3);
    Assert.NotEqual(alerting.Receivers.Count, alerting3.Receivers.Count);
    var result = alerting.Receivers.IntersectBy(alerting2.Receivers.Select(x => x.Name), x => x.Name);
    Assert.NotNull(result);
    var result2 = alerting.Receivers.ExceptBy(alerting3.Receivers.Select(x => x.Name), x => x.Name);
    Assert.Empty(result2);
    var result3 = alerting3.Receivers.ExceptBy(alerting.Receivers.Select(x => x.Name), x => x.Name);
    Assert.Single(result3);
  }

}
