 using System;
 using System.Collections.Generic;
 using System.ComponentModel.DataAnnotations;
 using Cms.BatCave.Sonar.Enumeration;

 namespace Cms.BatCave.Sonar.Models;

 public record ApiKeyConfiguration(
   [StringLength(44)] // Base64 encoded String for 32 bytes
   [Required]
   String ApiKey,
   ApiKeyType ApiKeyType,
   String? Environment,
   String? Tenant);
