using System;
using System.Collections.Generic;

namespace Cms.BatCave.Sonar.Configuration;

public record WebHostConfiguration(
  string[] AllowedOrigins);
