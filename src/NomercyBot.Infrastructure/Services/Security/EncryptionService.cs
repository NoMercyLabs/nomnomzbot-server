using System.Text;
using Microsoft.AspNetCore.DataProtection;
using NoMercyBot.Application.Common.Interfaces;

namespace NoMercyBot.Infrastructure.Services.Security;

/// <summary>
/// IEncryptionService implementation using ASP.NET Core Data Protection API.
/// Used to encrypt/decrypt sensitive values like OAuth tokens and API keys.
/// </summary>
public sealed class EncryptionService : IEncryptionService
{
    private readonly IDataProtector _protector;

    public EncryptionService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("NoMercyBot.Encryption.v1");
    }

    public string Encrypt(string plainText)
    {
        ArgumentException.ThrowIfNullOrEmpty(plainText);
        return _protector.Protect(plainText);
    }

    public string Decrypt(string cipherText)
    {
        ArgumentException.ThrowIfNullOrEmpty(cipherText);
        return _protector.Unprotect(cipherText);
    }

    public string? TryDecrypt(string? cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return null;
        }

        try
        {
            return _protector.Unprotect(cipherText);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
