using System;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Configuration;

/// <summary>
///   An interface that adds change notification support to the <see cref="IOptions{T}" /> interface.
/// </summary>
public interface INotifyOptionsChanged<out T> : IOptions<T> where T : class {
  event EventHandler<EventArgs> OptionsChanged;
}
