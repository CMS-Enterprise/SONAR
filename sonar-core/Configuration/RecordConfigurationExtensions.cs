using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Configuration;

public static class RecordConfigurationExtensions {
  /// <summary>
  /// Performs the necessary dependency injection for initializing the specified type from
  /// configuration.
  /// </summary>
  /// <remarks>
  /// To use the <typeparamref name="TOptions" /> instance that this method configures, use
  /// <see cref="IOptions{TOptions}" /> as your dependency.
  /// </remarks>
  /// <param name="services">The dependency service collection.</param>
  /// <param name="configuration">The application configuration.</param>
  /// <typeparam name="TOptions">The type to make available from configuration.</typeparam>
  public static void ConfigureRecord<TOptions>(
    this IServiceCollection services,
    IConfiguration configuration) where TOptions : class {

    services.AddSingleton<IOptions<TOptions>>(new RecordOptionsManager<TOptions>(configuration));
  }

  /// <summary>
  /// Instantiates an object from an <see cref="IConfiguration"/> instance by matching and invoking
  /// a constructor.
  /// </summary>
  /// <remarks>
  /// In .Net 6.0 in order to bind configuration to an object the object must have a default
  /// constructor and be mutable. This method allows configuration binding to be performed on
  /// immutable types such as records.
  /// </remarks>
  /// <param name="configuration">The configuration to read values from.</param>
  /// <typeparam name="T">The type of object to initialize.</typeparam>
  /// <returns>
  /// An instance of the type <typeparamref name="T" /> that has been initialized using values from
  /// the specified <paramref name="configuration" />.
  /// </returns>
  /// <exception cref="InvalidOperationException">
  /// The configuration could not be converted to the specified type.
  /// </exception>
  [return: NotNull]
  public static T BindCtor<T>(this IConfiguration configuration) {
    var constructors =
      typeof(T).GetConstructors().OrderByDescending(c => c.GetParameters().Length).ToList();

    ConstructorInfo? match = null;
    IImmutableDictionary<String, Object?>? parameters = null;

    // Find the best match constructor for the set of parameters available.
    foreach (var ctor in constructors) {
      match = ctor;
      parameters = ImmutableDictionary<String, Object?>.Empty;

      foreach (var param in ctor.GetParameters()) {
        Debug.Assert(param.Name != null);

        if (param.ParameterType.IsClass &&
          param.ParameterType != typeof(String) &&
          param.ParameterType != typeof(Uri)) {

          var subSection = configuration.GetSection(param.Name);
          if (subSection != null) {
            if (param.ParameterType.GetConstructors().Any(c => c.GetParameters().Length == 0)) {
              var paramValue = Activator.CreateInstance(param.ParameterType);
              if (paramValue == null) {
                throw new InvalidOperationException(
                  $"Unable to construct object of type {param.ParameterType.Name}"
                );
              }

              subSection.Bind(paramValue);
              parameters = parameters.Add(param.Name, paramValue);
            } else {
              parameters = parameters.Add(param.Name, subSection.BindCtor<T>());
            }
          } else if (!param.HasDefaultValue && !param.IsOptional) {
            match = null;
            break;
          }
        } else {
          var value = configuration[param.Name];
          if (value != null) {
            if (param.ParameterType == typeof(String)) {
              parameters = parameters.Add(param.Name, value);
            } else if (param.ParameterType == typeof(Uri)) {
              parameters = parameters.Add(param.Name, new Uri(value));
            } else if (param.ParameterType == typeof(Guid)) {
              parameters = parameters.Add(param.Name, Guid.Parse(value));
            } else {
              parameters = parameters.Add(param.Name, Convert.ChangeType(value, param.ParameterType));
            }
          } else if (!param.HasDefaultValue && !param.IsOptional) {
            match = null;
            break;
          }
        }
      }
    }

    if (match == null) {
      throw new InvalidOperationException(
        $"No matching constructor could be found for the configuration type {typeof(T).Name}"
      );
    }

    Debug.Assert(parameters != null, "parameters != null");

    var result = match.Invoke(
      match.GetParameters().Select(p => {
        Debug.Assert(p.Name != null, "p.Name != null");
        return parameters!.GetValueOrDefault(p.Name, p.DefaultValue);
      }).ToArray()
    );

    Debug.Assert(result != null, "result != null");

    return (T)result;
  }
}
