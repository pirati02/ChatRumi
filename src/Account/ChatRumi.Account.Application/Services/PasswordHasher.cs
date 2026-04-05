using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace ChatRumi.Account.Application.Services;

public static class PasswordHasher
{
    private const int Pbkdf2Iterations = 600_000;
    private const int SaltSize = 16;
    private const int SubkeySize = 32;

    /// <summary>Legacy HMAC-SHA512 password hashes stored a 64-byte digest.</summary>
    public static bool IsLegacyHash(byte[] storedHash) => storedHash.Length == 64;

    public static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        passwordSalt = RandomNumberGenerator.GetBytes(SaltSize);
        passwordHash = Pbkdf2(password, passwordSalt);
    }

    public static bool VerifyPassword(string password, byte[] storedHash, byte[] storedSalt)
    {
        if (IsLegacyHash(storedHash))
        {
            return VerifyLegacyHmacSha512(password, storedHash, storedSalt);
        }

        var computed = Pbkdf2(password, storedSalt);
        return CryptographicOperations.FixedTimeEquals(computed, storedHash);
    }

    private static byte[] Pbkdf2(string password, byte[] salt)
    {
        return KeyDerivation.Pbkdf2(
            password,
            salt,
            KeyDerivationPrf.HMACSHA256,
            Pbkdf2Iterations,
            SubkeySize);
    }

    private static bool VerifyLegacyHmacSha512(string password, byte[] storedHash, byte[] storedSalt)
    {
        using var hmac = new HMACSHA512(storedSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
    }
}
