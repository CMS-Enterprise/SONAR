using System;
using Xunit;

namespace Cms.BatCave.Sonar.Tests;

public static class AssertHelper {
  /// <summary>
  ///   Asserts that a condition is true where that condition is not an aspect
  ///   of the scenario under test but a precondition of running the test.
  /// </summary>
  /// <remarks>
  ///   This methods exists largely to make it clear when a test setup step has failed as opposed to the
  ///   test itself. Internally it uses <see cref="Assert.True(Boolean, String)" />.
  /// </remarks>
  public static void Precondition(Boolean condition, String message) {
    Assert.True(condition, $"Test Setup Failed: {message}");
  }
}
