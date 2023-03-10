using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Configuration;

public static class RecordConfigurationExtensions {
  private static readonly MethodInfo BindCtorMethod =
    new Func<IConfiguration, Object>(BindCtor<Object>).Method.GetGenericMethodDefinition();

  private static readonly MethodInfo ToArrayMethod =
    new Func<List<Object>, Object[]>(ToArray).Method.GetGenericMethodDefinition();

  /// <summary>
  ///   Performs the necessary dependency injection for initializing the specified type from
  ///   configuration.
  /// </summary>
  /// <remarks>
  ///   To use the <typeparamref name="TOptions" /> instance that this method configures, use
  ///   <see cref="IOptions{TOptions}" /> as your dependency.
  /// </remarks>
  /// <param name="services">The dependency service collection.</param>
  /// <param name="configuration">The application configuration.</param>
  /// <typeparam name="TOptions">The type to make available from configuration.</typeparam>
  public static void ConfigureRecord<TOptions>(
    this IServiceCollection services,
    IConfiguration configuration) where TOptions : class {

    services.AddSingleton<IOptions<TOptions>>(provider =>
      new RecordOptionsManager<TOptions>(
        configuration,
        provider.GetRequiredService<ILogger<RecordOptionsManager<TOptions>>>()));
  }

  /// <summary>
  ///   Instantiates an object from an <see cref="IConfiguration" /> instance by matching and invoking
  ///   a constructor.
  /// </summary>
  /// <remarks>
  ///   In .Net 6.0 in order to bind configuration to an object the object must have a default
  ///   constructor and be mutable. This method allows configuration binding to be performed on
  ///   immutable types such as records.
  /// </remarks>
  /// <param name="configuration">The configuration to read values from.</param>
  /// <typeparam name="T">The type of object to initialize.</typeparam>
  /// <exception cref="RecordBindingException">
  ///   The configuration could not be converted to the specified type.
  /// </exception>
  /// <returns>
  ///   An instance of the type <typeparamref name="T" /> that has been initialized using values from
  ///   the specified <paramref name="configuration" />.
  /// </returns>
  /// <exception cref="InvalidOperationException">
  ///   The specified type does not have any public constructors
  /// </exception>
  [return: NotNull]
  public static T BindCtor<T>(this IConfiguration configuration) {
    var constructors =
      typeof(T).GetConstructors().OrderByDescending(c => c.GetParameters().Length).ToList();

    (ConstructorInfo ctor, Int32 paramCount)? match = null;
    (ConstructorInfo ctor, List<ParameterInfo> missingParams, Double fractionMatch)? closestMatch = null;
    IImmutableDictionary<String, Object?>? parameters = null;

    // Find the best match constructor for the set of parameters available.
    foreach (var ctor in constructors) {
      var current = ctor;
      var missingParameters = new List<ParameterInfo>();

      parameters = ImmutableDictionary<String, Object?>.Empty;

      var ctorParameters = ctor.GetParameters();
      foreach (var param in ctorParameters) {
        Debug.Assert(param.Name != null);

        if (param.ParameterType.IsClass &&
          (param.ParameterType != typeof(String)) &&
          (param.ParameterType != typeof(Uri)) &&
          !param.ParameterType.IsArray &&
          !typeof(IEnumerable).IsAssignableFrom(param.ParameterType)) {

          var subSection = configuration.GetSection(param.Name);
          if (subSection != null) {
            if (param.ParameterType.GetConstructors().Any(c => c.GetParameters().Length == 0)) {
              var paramValue = Activator.CreateInstance(param.ParameterType);
              if (paramValue == null) {
                current = null;
                missingParameters.Add(param);
                continue;
              }

              subSection.Bind(paramValue);
              parameters = parameters.Add(param.Name, paramValue);
            } else {

              parameters = parameters.Add(param.Name, subSection.BindCtor(param.ParameterType));
            }
          } else if (!param.HasDefaultValue && !param.IsOptional) {
            current = null;
            missingParameters.Add(param);
          }
        } else {
          var enumerableInterface = param.ParameterType.GetInterfaces()
            .SingleOrDefault(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IEnumerable<>)));
          if ((param.ParameterType != typeof(String)) && (enumerableInterface != null)) {
            var itemType = enumerableInterface.GetGenericArguments()[0];
            // We're binding to a generic collection type;
            if (param.ParameterType.IsArray) {
              var list = Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));
              configuration.GetSection(param.Name).Bind(list);
              parameters = parameters.Add(
                param.Name,
                ToArrayMethod.MakeGenericMethod(itemType).Invoke(obj: null, new[] { list })
              );
            } else if (!param.ParameterType.IsAbstract && !param.ParameterType.IsInterface) {
              var colCtor = GetCollectionConstructor(param.ParameterType, itemType);
              if (colCtor == null) {
                throw new NotSupportedException(
                  $"The specified ParameterType is an generic collection type without a constructor that takes IEnumerable<>: {param.ParameterType.Name}"
                );
              }
              // The parameter type is a concrete, non-array collection like List<Object>
              var list = Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));
              configuration.GetSection(param.Name).Bind(list);
              parameters = parameters.Add(param.Name, colCtor.Invoke(new[] { list }));
            } else {
              throw new NotSupportedException(
                $"The specified parameter type is not supported: {param.ParameterType.Name}"
              );
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
              } else if (param.ParameterType.IsEnum) {
                parameters = parameters.Add(param.Name, Enum.Parse(param.ParameterType, value));
              } else if (
                typeof(Nullable<>).IsAssignableFrom(param.ParameterType) &&
                param.ParameterType.GetGenericArguments()[0].IsEnum) {

                parameters = parameters.Add(param.Name, Enum.Parse(param.ParameterType.GetGenericArguments()[0], value));
              } else {
                parameters = parameters.Add(param.Name, Convert.ChangeType(value, param.ParameterType));
              }
            } else if (!param.HasDefaultValue && !param.IsOptional) {
              current = null;
              missingParameters.Add(param);
            }
          }
        }
      }

      var score = 1.0 - ((Double)missingParameters.Count / ctorParameters.Length);
      if (current != null) {
        if ((match == null) || (ctorParameters.Length > match.Value.paramCount)) {
          match = (current, ctorParameters.Length);
          closestMatch = null;
        }
      } else if ((closestMatch == null) || (score > closestMatch.Value.fractionMatch)) {
        closestMatch = (ctor, missingParameters, score);
      }
    }

    if (match == null) {
      if (closestMatch != null) {
        throw new RecordBindingException(
          typeof(T),
          closestMatch.Value.ctor,
          closestMatch.Value.missingParams
        );
      } else {
        throw new InvalidOperationException(
          $"The configuration type {typeof(T).Name} has no constructors."
        );
      }
    }

    Debug.Assert(parameters != null, "parameters != null");

    var result = match.Value.ctor.Invoke(
      match.Value.ctor.GetParameters().Select(p => {
        Debug.Assert(p.Name != null, "p.Name != null");
        return parameters!.GetValueOrDefault(p.Name, p.DefaultValue);
      }).ToArray()
    );

    Debug.Assert(result != null, "result != null");

    return (T)result;
  }

  public static Object BindCtor(this IConfiguration configuration, Type objectType) {
    return BindCtorMethod.MakeGenericMethod(objectType).Invoke(obj: null, new Object[] { configuration })!;
  }

  private static T[] ToArray<T>(List<T> list) {
    return list.ToArray();
  }

  private static ConstructorInfo? GetCollectionConstructor(Type collectionType, Type itemType) {
    return collectionType.GetConstructors().SingleOrDefault(ctor => {
        var args = ctor.GetParameters();
        return (args.Length == 1) && (args[0].ParameterType == typeof(IEnumerable<>).MakeGenericType(itemType));
      }
    );
  }
}
