using System;
using System.Security.Cryptography;
using Cms.BatCave.Sonar.Configuration;
using Microsoft.Extensions.Options;

namespace Cms.BatCave.Sonar.Data;

public class KeyHashHelper {
  private const Int32 ApiKeyByteLength = 32;

  private readonly IOptions<SecurityConfiguration> _configuration;

  public KeyHashHelper(IOptions<SecurityConfiguration> configuration) {
    this._configuration = configuration;
  }

  private String GetRandomSalt() {
    return BCrypt.Net.BCrypt.GenerateSalt(this._configuration.Value.ApiKeyWorkFactor);
  }

  public (String key, String hashKey) GenerateKey() {
    var key = GenerateApiKeyValue();
    var hashKey = BCrypt.Net.BCrypt.HashPassword(key, this.GetRandomSalt());
    return (key, hashKey);
  }

  public static Boolean ValidatePassword(String password, String correctHash) {
    return BCrypt.Net.BCrypt.Verify(password, correctHash);
  }

  private static String GenerateApiKeyValue() {
    var apiKey = new Byte[ApiKeyByteLength];
    var encodedApiKey = "";

    using var rng = RandomNumberGenerator.Create();
    // Generate API key
    rng.GetBytes(apiKey);

    // Encode API key
    encodedApiKey = Convert.ToBase64String(apiKey);
    return encodedApiKey;
  }
}
