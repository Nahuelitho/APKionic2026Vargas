using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using multitenant_vargas.Api.Domain.Entities;

namespace multitenant_vargas.Api.Services;

public sealed class TokenService(IConfiguration configuration)
{
    public (string Token, DateTime ExpiraUtc) CrearAccessToken(Usuario usuario)
    {
        var expira = DateTime.UtcNow.AddMinutes(15);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(ClaimTypes.Name, usuario.Nombre)
        };

        if (usuario.Rol is { Activo: true }) claims.Add(new Claim(ClaimTypes.Role, usuario.Rol.Codigo));

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expira,
            signingCredentials: new SigningCredentials(ObtenerClave(configuration), SecurityAlgorithms.HmacSha256)
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }

    public static string CrearRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static SymmetricSecurityKey ObtenerClave(IConfiguration configuration)
    {
        var clave = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(clave) || clave.Length < 32)
            throw new InvalidOperationException("Configure Jwt:Key fuera del repositorio con al menos 32 caracteres.");
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave));
    }
}
