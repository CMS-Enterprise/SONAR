using Xunit;
using Xunit.Abstractions;

using System;
using System.Collections.Generic;
using System.Text.Json;
using Cms.BatCave.Sonar.Json;


namespace Cms.BatCave.Sonar.SonarCoreTest;

public class ArrayTupleConverterTest {
  private readonly ITestOutputHelper _output;
  private readonly DateTime _currentTime = DateTime.UtcNow;

  private readonly JsonSerializerOptions _options = new JsonSerializerOptions() {
    Converters = { new ArrayTupleConverterFactory() }
  };

  public ArrayTupleConverterTest(ITestOutputHelper output) {
    this._output = output;
  }

  // Serializes a valid Tuple containing String, Int32, and DateTime
  [Fact]
  public void VerifySingleSerialize() {
    // Arrange
    var testTuple = new Tuple<String, Int32, DateTime>(
      "Foo",
      42,
      _currentTime
    );
    var expectedResults = "[\"Foo\",42,\"" + _currentTime.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ") + "\"]";

    // Act
    var result = JsonSerializer.Serialize(testTuple, _options);
    _output.WriteLine("Serialized Result: " + result);

    // Assert
    Assert.Equal(expectedResults, result);
  }

  // Serialize Tuple containing 7 parameters
  [Fact]
  public void SerializeSevenParameters() {
    // Arrange
    var sevenParamTuple = new Tuple<String, String, String, String, String, String, String>(
      "1st Param",
      "2nd Param",
      "3rd Param",
      "4th Param",
      "5th Param",
      "6th Param",
      "7th Param"
    );

    // Act
    // Assert
    Assert.Null(Record.Exception(() => JsonSerializer.Serialize(sevenParamTuple, _options)));
  }

  // Serialize Tuple containing 8 parameters
  [Fact]
  public void SerializeEightParameters() {
    // Arrange
    var eightParamTuple = new Tuple<String, String, String, String, String, String, String, Tuple<String>>(
      "1st Param",
      "2nd Param",
      "3rd Param",
      "4th Param",
      "5th Param",
      "6th Param",
      "7th Param",
      new Tuple<String>("8th Param")
    );

    // Act
    // Assert
    Assert.Throws<ArgumentException>(() => JsonSerializer.Serialize(eightParamTuple, _options));
  }

  // Serialize Tuple containing nulls or empty
  // Nullable String
  // Non Nullable DateTime, Int
  [Theory]
  [InlineData(null, 77)]
  [InlineData("", 118)]
  public void SerializeNullValues(String str, Int32 value) {
    // Arrange
    var serializedString = new Tuple<String, Int32, DateTime>(str, value, _currentTime);

    // Act
    // Assert
    Assert.Null(Record.Exception( ()=> JsonSerializer.Serialize(serializedString, _options)));
  }

  // Serializes a valid Tuple containing String, Int32, and DateTime
  [Fact]
  public void VerifySingleDeserialize() {
    // Arrange
    var expectedResults = new Tuple<String, Int32, DateTime>(
      "Foo",
      42,
      _currentTime
    );
    var inputString = "[\"Foo\",42,\"" + _currentTime.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ") + "\"]";

    // Act
    var result = JsonSerializer.Deserialize<Tuple<String, Int32, DateTime>>(inputString, _options);
    _output.WriteLine("Deserialized Result: " + result);

    // Assert
    Assert.Equal(expectedResults, result);
  }

  // Deserialize Tuple containing nth number of parameters
  [Fact]
  public void DeserializeNthParameters() {
    // Arrange
    var sevenValues = "[\"First\", \"Second\", \"Third\", \"Fourth\", \"Fifth\", \"Sixth\", \"Seventh\" ]";
    var eightValues = "[\"First\", \"Second\", \"Third\", \"Fourth\", \"Fifth\", \"Sixth\", \"Seventh\", \"Eighth\" ]";

    // Act
    // Assert
    Assert.Null(Record.Exception(() =>
      JsonSerializer.Deserialize<Tuple<String, String, String, String, String, String, String>>(sevenValues, _options)));
    Assert.Throws<JsonException>(() =>
      JsonSerializer.Deserialize<Tuple<String, String, String, String, String, String, String>>(eightValues, _options));
  }

  // Deserialize Tuple containing empty string
  [Fact]
  public void DeserializeEmptyString() {
    // Arrange
    var expectedResults = new Tuple<String, Int32, DateTime>(
      "",
      999,
      _currentTime
    );
    var inputString = "[\"\",999,\"" + _currentTime.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ") + "\"]";

    // Act
    var result = JsonSerializer.Deserialize<Tuple<String, Int32, DateTime>>(inputString, _options);
    _output.WriteLine("Deserialized Result: " + result);

    // Assert
    Assert.Equal(expectedResults, result);
  }
}

