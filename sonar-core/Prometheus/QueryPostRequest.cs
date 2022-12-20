using System;

namespace Cms.BatCave.Sonar.Query;

public record QueryPostRequest(String Query, DateTime Timestamp, TimeSpan? Timeout);
