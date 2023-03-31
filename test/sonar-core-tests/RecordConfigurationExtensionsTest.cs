using System;
using System.Globalization;
using Cms.BatCave.Sonar.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Xunit;

namespace Cms.BatCave.Sonar.Tests;

public class RecordConfigurationExtensionsTest {
  private readonly IConfigurationRoot _config;

  public RecordConfigurationExtensionsTest() {
    this._config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
  }

  [Fact]
  public void TestBindCtor_AllTypes() {
    var result =
      this._config.GetSection("RecordConfigurationTests")
        .GetSection("AllTypesTest1")
        .BindCtor<AllTypesRecord>();

    Assert.Equal("test-string", result.StringType);
    Assert.Equal(42, result.IntegerType);
    Assert.Equal(2701, result.NullableIntegerType);
    Assert.Equal(3.14m, result.DecimalType);
    Assert.Equal(Guid.Parse("f0d9c508-7d5e-49c2-8d8b-1e66ca8ef707"), result.GuidType);
    Assert.Equal(DateTime.Parse("2023-03-20T12:34:56Z").ToUniversalTime(), result.DateTimeType.ToUniversalTime());
    Assert.Equal(TimeSpan.Parse("3.11:16:32.458"), result.TimeSpanType);
    Assert.Equal(new Uri("http://localhost:1234/"), result.UriType);
    Assert.Equal(new[] { "Foo", "Bar", "Baz" }, result.ArrayOfStringType);
    Assert.Equal(new NestedRecord("Anything"), result.NestedRecordType);
    Assert.NotNull(result.ComplexObjectType);
    Assert.Equal("Anything Else", result.ComplexObjectType.Value);
  }

  [Fact]
  public void TestBindCtor_AllTypes_DefaultsAndNulls() {
    var result =
      this._config.GetSection("RecordConfigurationTests")
        .GetSection("AllTypesTest2")
        .BindCtor<AllTypesRecord>();

    // Note: there is an issue with IConfiguration where it is not possible to
    // differentiate between null and ""
    Assert.Equal(String.Empty, result.StringType);
    Assert.Equal(0, result.IntegerType);
    Assert.Null(result.NullableIntegerType);
    Assert.Equal(0m, result.DecimalType);
    Assert.Equal(Guid.Empty, result.GuidType);
    Assert.Equal(DateTime.MinValue, result.DateTimeType);
    Assert.Equal(TimeSpan.Zero, result.TimeSpanType);
    Assert.Null(result.UriType);
    Assert.Null(result.ArrayOfStringType);
    Assert.Null(result.NestedRecordType);
    Assert.Null(result.ComplexObjectType);
  }

  public record AllTypesRecord(
    String? StringType,
    Int32 IntegerType,
    Int32? NullableIntegerType,
    Decimal DecimalType,
    Guid GuidType,
    DateTime DateTimeType,
    TimeSpan TimeSpanType,
    Uri? UriType,
    String[]? ArrayOfStringType,
    NestedRecord? NestedRecordType,
    ComplexObject? ComplexObjectType) {
  }

  public record NestedRecord(String Value);

  public class ComplexObject {
    public String Value { get; set; }
  }
}
