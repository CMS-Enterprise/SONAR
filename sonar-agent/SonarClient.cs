using System.Text.Json;

namespace Cms.BatCave.Sonar.Agent;

public partial class SonarClient {
  partial void UpdateJsonSerializerSettings(JsonSerializerOptions settings) {
    settings.PropertyNameCaseInsensitive = true;
  }
}
