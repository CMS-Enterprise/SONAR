using System;

namespace Cms.BatCave.Sonar.Configuration;

public record SmtpConfiguration(
  String Sender,
  String Host,
  UInt16 Port = 587,
  String? Username = null,
  String? Password = null);
