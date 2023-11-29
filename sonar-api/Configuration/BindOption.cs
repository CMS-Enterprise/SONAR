using System;

namespace Cms.BatCave.Sonar.Configuration;

[Flags]
public enum BindOption {
  Ipv4 = 0x01,
  Ipv6 = 0x02,
  Ipv4And6 = Ipv4 | Ipv6
}
