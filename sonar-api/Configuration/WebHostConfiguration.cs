using System;

namespace Cms.BatCave.Sonar.Configuration;

public record WebHostConfiguration(
  String[] AllowedOrigins,
  BindOption BindOptions = BindOption.Ipv4);
