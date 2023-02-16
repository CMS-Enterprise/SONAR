using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Cms.BatCave.Sonar.Configuration;

public class ConfigurationWatcher {
  private readonly IConfigurationProvider _configProvider;

  public ConfigurationWatcher(IConfigurationProvider configProvider) {
    this._configProvider = configProvider;
  }

  public FileSystemWatcher CreateConfigWatcher(String directoryPath, String configFileName) {
    var watcher = new FileSystemWatcher(directoryPath);
    watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
    watcher.IncludeSubdirectories = true;
    watcher.Filter = configFileName;
    watcher.Changed += OnChanged;
    watcher.EnableRaisingEvents = true;
    return watcher;
  }

  private void OnChanged(object sender, FileSystemEventArgs e) {
    if (e.ChangeType != WatcherChangeTypes.Changed) {
      return;
    }
    this._configProvider.Load();
  }
}
