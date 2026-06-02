using System;
using System.Security.Cryptography;
using System.Text;

namespace ComputerCompanion.Services;

public class SecurityService : ISecurityService
{
    private readonly byte[] _secretKey;
    private string? _currentSessionKey;
    private DateTime _sessionKeyExpiration;

    public SecurityService()
    {
        _secretKey = LoadOrGenerateSecretKey();
        _currentSessionKey = null;
        _sessionKeyExpiration = DateTime.MinValue;
    }

    private byte[] LoadOrGenerateSecretKey()
    {
        try
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var keyPath = System.IO.Path.Combine(appDataPath, "ComputerCompanion", "security.key");
            
            if (System.IO.File.Exists(keyPath))
            {
                var bytes = System.IO.File.ReadAllBytes(keyPath);
                if (bytes.Length == 32)
                {
                    return bytes;
                }
            }
        }
        catch
        {
        }

        var newKey = GenerateRandomKey();
        try
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var keyDir = System.IO.Path.Combine(appDataPath, "ComputerCompanion");
            System.IO.Directory.CreateDirectory(keyDir);
            var keyPath = System.IO.Path.Combine(keyDir, "security.key");
            System.IO.File.WriteAllBytes(keyPath, newKey);
        }
        catch
        {
        }
        
        return newKey;
    }

    private byte[] GenerateRandomKey()
    {
        using var rng = RandomNumberGenerator.Create();
        var key = new byte[32];
        rng.GetBytes(key);
        return key;
    }

    public string SignMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            throw new ArgumentException("Message cannot be null or empty");

        using var hmac = new HMACSHA256(_secretKey);
        var messageBytes = Encoding.UTF8.GetBytes(message);
        var hash = hmac.ComputeHash(messageBytes);
        return Convert.ToBase64String(hash);
    }

    public bool VerifySignature(string message, string signature)
    {
        if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(signature))
            return false;

        try
        {
            var expectedSignature = SignMessage(message);
            return ConstantTimeComparison(expectedSignature, signature);
        }
        catch
        {
            return false;
        }
    }

    private bool ConstantTimeComparison(string a, string b)
    {
        if (a.Length != b.Length)
            return false;

        int result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }
        return result == 0;
    }

    public string GenerateSessionKey()
    {
        using var rng = RandomNumberGenerator.Create();
        var key = new byte[16];
        rng.GetBytes(key);
        
        _currentSessionKey = Convert.ToBase64String(key);
        _sessionKeyExpiration = DateTime.Now.AddMinutes(30);
        
        return _currentSessionKey;
    }

    public bool ValidateSessionKey(string sessionKey)
    {
        if (string.IsNullOrEmpty(sessionKey))
            return false;

        if (DateTime.Now > _sessionKeyExpiration)
            return false;

        return ConstantTimeComparison(_currentSessionKey ?? string.Empty, sessionKey);
    }
}