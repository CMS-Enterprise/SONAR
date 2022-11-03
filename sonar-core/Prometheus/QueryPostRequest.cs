using System;

namespace Cms.BatCave.Sonar.Prometheus;

public record QueryPostRequest(String Query, DateTime Timestamp, TimeSpan Timeout);
