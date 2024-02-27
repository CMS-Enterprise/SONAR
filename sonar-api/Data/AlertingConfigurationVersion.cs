using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Cms.BatCave.Sonar.Data;

[Table("alerting_config_version")]
public class AlertingConfigurationVersion {
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public Int32 VersionNumber { get; init; }
  public DateTime Timestamp { get; set; }


  public AlertingConfigurationVersion(DateTime timestamp) {
    this.Timestamp = timestamp;
  }
}
