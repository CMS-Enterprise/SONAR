// This source code is part of ASP.Net Core
// https://github.com/dotnet/aspnetcore/blob/v6.0.10/src/Http/Http.Extensions/src/ProblemDetails.cs
// Use with permission under the MIT license.
// The MIT License (MIT)
//
// Copyright (c) .NET Foundation and Contributors
//
// All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cms.BatCave.Sonar.Models;


/// <summary>
/// A machine-readable format for specifying errors in HTTP API responses based on <see href="https://tools.ietf.org/html/rfc7807"/>.
/// </summary>
public class ProblemDetails
{
    /// <summary>
    /// A URI reference [RFC3986] that identifies the problem type. This specification encourages that, when
    /// dereferenced, it provide human-readable documentation for the problem type
    /// (e.g., using HTML [W3C.REC-html5-20141028]). When this member is not present, its value is assumed to be
    /// "about:blank".
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// A short, human-readable summary of the problem type. It SHOULD NOT change from occurrence to occurrence
    /// of the problem, except for purposes of localization(e.g., using proactive content negotiation;
    /// see[RFC7231], Section 3.4).
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// The HTTP status code([RFC7231], Section 6) generated by the origin server for this occurrence of the problem.
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// A human-readable explanation specific to this occurrence of the problem.
    /// </summary>
    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    /// <summary>
    /// A URI reference that identifies the specific occurrence of the problem. It may or may not yield further information if dereferenced.
    /// </summary>
    [JsonPropertyName("instance")]
    public string? Instance { get; set; }

    /// <summary>
    /// Gets the <see cref="IDictionary{TKey, TValue}"/> for extension members.
    /// <para>
    /// Problem type definitions MAY extend the problem details object with additional members. Extension members appear in the same namespace as
    /// other members of a problem type.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The round-tripping behavior for <see cref="Extensions"/> is determined by the implementation of the Input \ Output formatters.
    /// In particular, complex types or collection types may not round-trip to the original type when using the built-in JSON or XML formatters.
    /// </remarks>
    [JsonExtensionData]
    public IDictionary<string, object?> Extensions { get; } = new Dictionary<string, object?>(StringComparer.Ordinal);
}
