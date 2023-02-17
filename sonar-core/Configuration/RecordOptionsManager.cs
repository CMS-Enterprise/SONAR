using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Cms.BatCave.Sonar.Configuration;

public class RecordOptionsManager<T> : INotifyOptionsChanged<T> where T : class {
  public T Value { get; private set; }

  public event EventHandler<EventArgs>? OptionsChanged;

  public RecordOptionsManager(IConfiguration configuration, ILogger<RecordOptionsManager<T>> logger) {
    this.Value = configuration.BindCtor<T>();

    // Update config value when config file is modified
    void OnChange() {
      var updatedConfigValue = configuration.BindCtor<T>();
      if (!this.Value.Equals(updatedConfigValue)) {
        logger.LogDebug("{ConfigurationType} configuration updated", typeof(T));
        this.Value = configuration.BindCtor<T>();
        this.OptionsChanged?.Invoke(this, EventArgs.Empty);
      }
    }

    ChangeToken.OnChange(configuration.GetReloadToken, OnChange);
  }
}
