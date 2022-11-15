using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cms.BatCave.Sonar.Data;

public static class DataDependencyRegistration {
  public static void RegisterDependencies<TDataContext>(IServiceCollection services)
    where TDataContext : DataContext {
    services.AddDbContext<DataContext, TDataContext>();
    var entityTypes =
      typeof(DataContext).Assembly.GetTypes()
        .Where(t => t.IsClass && !t.IsAbstract && (t.GetCustomAttribute<TableAttribute>() != null));
    foreach (var entityType in entityTypes) {
      DataDependencyRegistration.RegisterDbSetMethod
        .MakeGenericMethod(entityType).Invoke(null, new Object[] { services });
    }
  }

  private static readonly MethodInfo RegisterDbSetMethod =
    new Action<IServiceCollection>(DataDependencyRegistration.RegisterDbSet<Object>).Method
      .GetGenericMethodDefinition();

  private static void RegisterDbSet<TEntity>(IServiceCollection services) where TEntity : class {
    services.AddScoped(provider => provider.GetRequiredService<DataContext>().Set<TEntity>());
  }
}
