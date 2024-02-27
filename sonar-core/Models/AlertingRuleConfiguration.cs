using System;
using System.ComponentModel.DataAnnotations;
using Cms.BatCave.Sonar.Enumeration;

namespace Cms.BatCave.Sonar.Models;

public record AlertingRuleConfiguration {

  public AlertingRuleConfiguration(
    String name,
    HealthStatus threshold,
    String receiverName,
    Int32 delay = 0) {

    this.Name = name;
    this.Threshold = threshold;
    this.ReceiverName = receiverName;
    this.Delay = delay;
  }

  [Required]
  [StringLength(100)]
  [RegularExpression("^[0-9a-zA-Z_-]+$")]
  public String Name { get; }

  /// <summary>
  ///   The threshold at which the alert will fire. When the service has the specified status, or any
  ///   status more severe than that, it will begin firing after the specified <see cref="Delay" />
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Regardless of the status specified, the alert will also fire if the service has the Unknown
  ///     status.
  ///   </para>
  ///   <para>
  ///     Specifying a status of <see cref="HealthStatus.Online" /> is not valid and will result in no
  ///     alert being created.
  ///   </para>
  /// </remarks>
  [Required]
  public HealthStatus Threshold { get; }

  [Required] public String ReceiverName { get; }

  /// <summary>
  ///   The number of seconds that the service must be in a matching status before it causes the alert to
  ///   fire.
  /// </summary>
  public Int32 Delay { get; }

}
