using System;
using System.Security.Cryptography;

namespace Cms.BatCave.Sonar.Data;

public class KeyHash {
  private const Int32 ApiKeyByteLength = 32;
  private static String GetRandomSalt()
  {
    return BCrypt.Net.BCrypt.GenerateSalt(12);
  }

  public static String HashPassword(String password)
  {
    return BCrypt.Net.BCrypt.HashPassword(password, GetRandomSalt());
  }

  public static String GenerateKey()
  {
    String password = GenerateApiKeyValue();
    return BCrypt.Net.BCrypt.HashPassword(password, GetRandomSalt());
  }


  public static Boolean ValidatePassword(String password, String correctHash)
  {
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
