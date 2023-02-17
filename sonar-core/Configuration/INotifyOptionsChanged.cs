using System;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Configuration;

public interface INotifyOptionsChanged<out T> : IOptions<T> where T : class {
  event EventHandler<EventArgs> OptionsChanged;
}
