using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Configuration;

public class RecordOptionsManager<T> : IOptions<T> where T : class {
  public T Value { get; }

  public RecordOptionsManager(IConfiguration configuration) {
    // TODO(BATAPI-88): support caching and updates
    this.Value = configuration.BindCtor<T>();
  }
}
