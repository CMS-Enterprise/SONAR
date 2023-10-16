using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.XPath;
using Cms.BatCave.Sonar.Agent.Exceptions;
using Cms.BatCave.Sonar.Enumeration;
using Json.Path;

namespace Cms.BatCave.Sonar.Agent.Helpers;

public static class DocumentValueExtractor {

  // Convenience method for selecting the appropriate value extractor method based on an HttpBodyType enum value.
  public static String GetStringValue(HttpBodyType documentType, String document, String path) {
    Func<String, String, String> extractor = documentType switch {
      HttpBodyType.Json => GetValueAsStringFromJson,
      HttpBodyType.Xml => GetStringValueFromXml,
      _ => throw new NotSupportedException()
    };

    return extractor.Invoke(document, path);
  }

  /// <summary>
  /// Gets the string value of the JSON document node at the given path. The path must match exactly one
  /// node in the document, and that node must be a non-empty/non-white-space string.
  /// </summary>
  /// <param name="json">The JSON document to extract the value from.</param>
  /// <param name="jsonPath">The <see cref="JsonPath"/> expression to match the document node to extract the value from.</param>
  /// <returns>The string value from the document node at the given path.</returns>
  /// <exception cref="DocumentValueExtractorException">
  /// If document isn't valid JSON, the JSONPath expression isn't valid, the path doesn't match exactly one node,
  /// the selected node isn't a string, or the string is empty/white-space.
  /// </exception>
  public static String GetStringValueFromJson(String json, String jsonPath) {
    var node = GetNodeFromJson(json, jsonPath);

    if (node?.GetValue<JsonElement>() is not { ValueKind: JsonValueKind.String }) {
      throw new DocumentValueExtractorException(
        $"JSON value is {(node?.GetValue<JsonElement>().ValueKind ?? JsonValueKind.Null).ToString()}, " +
        $"expected {JsonValueKind.String} at JsonPath: {jsonPath}");
    }

    var value = node.GetValue<String>();

    if (String.IsNullOrWhiteSpace(value)) {
      throw new DocumentValueExtractorException(
        $"JSON {JsonValueKind.String} is empty at JsonPath: {jsonPath}");
    }

    return value;
  }

  /// <summary>
  /// Gets the value of the JSON document node as a string at the given path. The path must match exactly one
  /// node in the document, and that node must be a non-empty/non-white-space string.
  /// </summary>
  /// <param name="json">The JSON document to extract the value from.</param>
  /// <param name="jsonPath">The <see cref="JsonPath"/> expression to match the document node to extract the value from.</param>
  /// <returns>The string value from the document node at the given path.</returns>
  /// <exception cref="DocumentValueExtractorException">
  /// If document isn't valid JSON, the JSONPath expression isn't valid, the path doesn't match exactly one node,
  /// the selected node isn't a string, or the string is empty/white-space.
  /// </exception>
  public static String GetValueAsStringFromJson(String json, String jsonPath) {
    var node = GetNodeFromJson(json, jsonPath);
    //There is no value type checking performed here.
    //We get the string representation of all value types;
    var value = (node != null) ? node.ToString() : String.Empty;
    if (String.IsNullOrWhiteSpace(value)) {
      throw new DocumentValueExtractorException(
        $"JSON value is empty at JsonPath: {jsonPath}");
    }

    return value;
  }

  public static JsonNode? GetNodeFromJson(String json, String jsonPath) {
    PathResult? pathResult;
    try {
      pathResult = JsonPath.Parse(jsonPath).Evaluate(JsonNode.Parse(json));
    } catch (JsonException e) {
      throw new DocumentValueExtractorException(message: "Invalid JSON document.", e);
    } catch (PathParseException e) {
      throw new DocumentValueExtractorException(message: "Invalid JsonPath expression.", e);
    }

    if (pathResult.Matches is not { Count: 1 }) {
      throw new DocumentValueExtractorException(
        $"JSON document has {pathResult.Matches?.Count ?? 0} nodes matching JsonPath, expected 1: {jsonPath}");
    }

    return pathResult.Matches[0].Value;
  }

  /// <summary>
  /// Gets the text content of the XML document node at the given path. The path must match exactly one
  /// node in the document, and the text content of that node must be non-empty/non-white-space.
  /// Returns ALL inner text content of the selected node, so if the node is like:
  /// <code>&lt;version&gt;1.0&lt;commit sha='abcdef99' /&gt;&lt;/version&gt;</code>
  /// the return value will be '1.0abcdef99'.
  /// </summary>
  /// <param name="xml">The XML document to extract the value from.</param>
  /// <param name="xPath">The XPath expression to match the document node to extract the value from.</param>
  /// <returns>The text content from the document node at the given path.</returns>
  /// <exception cref="DocumentValueExtractorException">
  /// If document isn't valid XML, the XPath expression isn't valid, the path doesn't match exactly one node,
  /// or the text content of that node is empty/white-space.
  /// </exception>
  public static String GetStringValueFromXml(String xml, String xPath) {
    var xmlNodes = GetNodeFromXml(xml, xPath);

    //This is the string value of any data type.
    var value = (xmlNodes != null) ? xmlNodes[0]?.InnerText : String.Empty;

    if (String.IsNullOrWhiteSpace(value)) {
      throw new DocumentValueExtractorException(
        $"XML document node has empty text content at XPath: {xPath}");
    }
    return value;
  }

  public static XmlNodeList? GetNodeFromXml(String xml, String xPath) {
    var xmlDocument = new XmlDocument();
    XmlNodeList? xmlNodes;

    try {
      xmlDocument.LoadXml(xml);
      xmlNodes = xmlDocument.SelectNodes(xPath);
    } catch (XmlException e) {
      throw new DocumentValueExtractorException(message: "Invalid XML document.", e);
    } catch (XPathException e) {
      throw new DocumentValueExtractorException(message: "Invalid XPath expression.", e);
    }

    if (xmlNodes is not { Count: 1 }) {
      throw new DocumentValueExtractorException(
        $"XML document has {xmlNodes?.Count ?? 0} nodes matching XPath, expected 1: {xPath}");
    }

    return xmlNodes;
  }

}
