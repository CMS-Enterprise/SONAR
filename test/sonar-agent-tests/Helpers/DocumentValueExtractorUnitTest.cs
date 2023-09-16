using System;
using System.Text.Json;
using System.Xml;
using System.Xml.XPath;
using Cms.BatCave.Sonar.Agent.Exceptions;
using Cms.BatCave.Sonar.Agent.Helpers;
using Json.Path;
using Xunit;
using Xunit.Abstractions;

namespace Cms.BatCave.Sonar.Agent.Tests.Helpers;

public class DocumentValueExtractorUnitTest {

  private readonly ITestOutputHelper _output;

  public DocumentValueExtractorUnitTest(ITestOutputHelper output) {
    this._output = output;
  }

  [Theory]
  [InlineData("""{"invalid json"}""")]
  public void GetStringValueFromJson_InvalidJson_ThrowsException(String invalidJson) {
    var exception = Assert.Throws<DocumentValueExtractorException>(() =>
      DocumentValueExtractor.GetStringValueFromJson(invalidJson, jsonPath: "$"));
    Assert.IsAssignableFrom<JsonException>(exception.InnerException);
    this._output.WriteLine(exception.InnerException?.Message);
  }

  [Theory]
  [InlineData(".version")]
  public void GetStringValueFromJson_InvalidJsonPath_ThrowsException(String invalidJsonPath) {
    var exception = Assert.Throws<DocumentValueExtractorException>(() =>
      DocumentValueExtractor.GetStringValueFromJson("{}", invalidJsonPath));
    Assert.IsType<PathParseException>(exception.InnerException);
    this._output.WriteLine(exception.InnerException?.Message);
  }

  [Theory]
  [InlineData("""{"app":{"version":"1.0"}}""", "$.version")]
  [InlineData("""[{"app":"a","version":"1.0"},{"app":"b","version":"2.0"}]""", "$[*].version")]
  [InlineData("""{"versions":["1.0","2.0","3.0"]}""", "$.versions[*]")]
  public void GetStringValueFromJson_PathDoesntSelectSingleElement_ThrowsException(String json, String jsonPath) {
    var exception = Assert.Throws<DocumentValueExtractorException>(() =>
      DocumentValueExtractor.GetStringValueFromJson(json, jsonPath));
    Assert.Null(exception.InnerException);
    this._output.WriteLine(exception.Message);
  }

  [Theory]
  [InlineData("""{"version":null}""", "$.version")]
  [InlineData("""{"version":1.0}""", "$.version")]
  [InlineData("""{"version":[1.0]}""", "$.version[0]")]
  public void GetStringValueFromJson_SelectedElementIsNotString_ThrowsException(String json, String jsonPath) {
    var exception = Assert.Throws<DocumentValueExtractorException>(() =>
      DocumentValueExtractor.GetStringValueFromJson(json, jsonPath));
    Assert.Null(exception.InnerException);
    this._output.WriteLine(exception.Message);
  }

  [Theory]
  [InlineData("""{"version":""}""", "$.version")]
  [InlineData("""{"version":[""]}""", "$.version[0]")]
  public void GetStringValueFromJson_SelectedStringIsEmpty_ThrowsException(String json, String jsonPath) {
    var exception = Assert.Throws<DocumentValueExtractorException>(() =>
      DocumentValueExtractor.GetStringValueFromJson(json, jsonPath));
    Assert.Null(exception.InnerException);
    this._output.WriteLine(exception.Message);
  }

  [Theory]
  [InlineData("""{"version":"1.0"}""", "$.version", "1.0")]
  [InlineData("""{"version":["1.0"]}""", "$.version[0]", "1.0")]
  [InlineData("""[{"app":"a","version":"1.0"},{"app":"b","version":"2.0"}]""", "$[?@.app=='b'].version", "2.0")]
  public void GetStringValueFromJson_ValidCases(String json, String jsonPath, String expectedVersion) {
    Assert.Equal(expectedVersion, DocumentValueExtractor.GetStringValueFromJson(json, jsonPath));
  }

  [Theory]
  [InlineData("<root><child attribute=Invalid XML></root>")]
  public void GetStringValueFromXml_InvalidXml_ThrowsException(String invalidXml) {
    var exception = Assert.Throws<DocumentValueExtractorException>(() =>
      DocumentValueExtractor.GetStringValueFromXml(invalidXml, xPath: "/"));
    Assert.IsType<XmlException>(exception.InnerException);
    this._output.WriteLine(exception.InnerException?.Message);
  }

  [Theory]
  [InlineData("/prefixesAreInvalid:root/child/version")]
  // The "no prefixes" requirement comes from here (see Remarks section):
  // https://learn.microsoft.com/en-us/dotnet/api/system.xml.xmlnode.selectnodes?view=net-7.0
  // If we're going to have deal with XML namespaces, DocumentValueExtractor.GetStringValueFromXml will have
  // require a more sophisticated implementation (it errs toward simplicity as of now).
  [InlineData("/root/child@version")]
  public void GetStringValueFromXml_InvalidXPath_ThrowsException(String invalidXPath) {
    var exception = Assert.Throws<DocumentValueExtractorException>(() =>
      DocumentValueExtractor.GetStringValueFromXml(xml: "<root />", invalidXPath));
    Assert.IsType<XPathException>(exception.InnerException);
    this._output.WriteLine(exception.InnerException?.Message);
  }

  [Theory]
  [InlineData("<root><a><version>1.0</version></a><b><version>1.0</version></b></root>", "/root//version")]
  [InlineData("<root><child><version>1.0</version></child></root>", "/root/version")]
  [InlineData("<root version='1.0'><child version='1.0'></child></root>", "//@version")]
  [InlineData(
    "<root><child name='a' version='1.0'></child><child name='b' version='2.0'></child></root>",
    "/root/child/@version")]
  public void GetStringValueFromXml_PathDoesNotSelectSingleNode_ThrowsException(String xml, String xPath) {
    var exception = Assert.Throws<DocumentValueExtractorException>(() =>
      DocumentValueExtractor.GetStringValueFromXml(xml, xPath));
    Assert.Null(exception.InnerException);
    this._output.WriteLine(exception.Message);
  }

  [Theory]
  [InlineData("<root><version></version></root>", "/root/version")]
  [InlineData("<root><version>   </version></root>", "/root/version")]
  [InlineData("<root><child version=''></child></root>", "/root/child/@version")]
  [InlineData("<root><child version='    '></child></root>", "/root/child/@version")]
  public void GetStringValueFromXml_NodeTextContentIsEmpty_ThrowsException(String xml, String xPath) {
    var exception = Assert.Throws<DocumentValueExtractorException>(() =>
      DocumentValueExtractor.GetStringValueFromXml(xml, xPath));
    Assert.Null(exception.InnerException);
    this._output.WriteLine(exception.Message);
  }

  [Theory]
  [InlineData("<root><version>1.0</version></root>", "/root/version", "1.0")]
  [InlineData("<root><child><version>1.0</version></child></root>", "/root//version", "1.0")]
  [InlineData("<root><child version='1.0'/></root>", "/root/child/@version", "1.0")]
  [InlineData("<root><a><version>1.0</version></a><b><version>1.0</version></b></root>", "/root/a/version", "1.0")]
  [InlineData(
    "<root><child name='a' version='1.0'/><child name='b' version='2.0'/></root>",
    "/root/child[@name='b']/@version",
    "2.0")]
  public void GetStringValueFromXml_ValidCases(String xml, String xPath, String expectedVersion) {
    Assert.Equal(expectedVersion, DocumentValueExtractor.GetStringValueFromXml(xml, xPath));
  }

}
