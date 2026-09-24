using System.Security.Cryptography;

namespace multitenant_vargas.Api.Services;

public static class PasswordService
{
    private const int Iteraciones = 100_000;
    private const int Tamanio = 32;

    public static string HashDeterministico(string password, string semilla)
    {
        var salt = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(semilla))[..16];
        return CrearHash(password, salt);
    }

    public static string Hash(string password) => CrearHash(password, RandomNumberGenerator.GetBytes(16));

    public static bool Verificar(string password, string hashGuardado)
    {
        try
        {
            var partes = hashGuardado.Split('.', 2);
            var salt = Convert.FromBase64String(partes[0]);
            var esperado = Convert.FromBase64String(partes[1]);
            var obtenido = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iteraciones, HashAlgorithmName.SHA256, Tamanio);
            return CryptographicOperations.FixedTimeEquals(esperado, obtenido);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string CrearHash(string password, byte[] salt)
    {
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iteraciones, HashAlgorithmName.SHA256, Tamanio);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }
}
