using System;

namespace Cms.BatCave.Sonar.Configuration;

public record WebHostConfiguration(
  String[]? AllowedOrigins = null,
  BindOption BindOptions = BindOption.Ipv4);
