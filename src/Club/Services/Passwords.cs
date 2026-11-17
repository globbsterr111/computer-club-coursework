using System.Security.Cryptography;

namespace Club.Services;

public static class Passwords
{
    private const int Iterations = 210000;

    public static string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = DeriveHash(password, salt);
        return $"210000:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string encoded)
    {
        var parts = encoded.Split(':');
        if (parts.Length != 3 || parts[0] != "210000")
            return false;

        try
        {
            byte[] expected = Convert.FromBase64String(parts[2]);
            byte[] actual = DeriveHash(password, Convert.FromBase64String(parts[1]));
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] DeriveHash(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
}
