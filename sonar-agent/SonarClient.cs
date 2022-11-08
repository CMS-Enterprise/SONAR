using System.Text.Json;
using System.Text.Json.Serialization;
using Cms.BatCave.Sonar.Json;

namespace Cms.BatCave.Sonar.Agent;

public partial class SonarClient {
  partial void UpdateJsonSerializerSettings(JsonSerializerOptions settings) {
    settings.PropertyNameCaseInsensitive = true;
    settings.Converters.Add(new JsonStringEnumConverter());
    settings.Converters.Add(new ArrayTupleConverterFactory());
  }
}
