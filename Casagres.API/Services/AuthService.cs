using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Casagres.API.Data;
using Casagres.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Casagres.API.Services;

public class AuthService
{
    private readonly CasagresDbContext _db;
    private readonly IConfiguration _configuration;

    public AuthService(
        CasagresDbContext db,
        IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<string?> LoginAsync(
        string usuario,
        string password)
    {
        var usuarioDb = await _db.Usuarios
            .FirstOrDefaultAsync(u =>
                u.UsuarioNombre == usuario &&
                u.Activo);

        if (usuarioDb == null)
        {
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(
                password,
                usuarioDb.PasswordHash))
        {
            return null;
        }

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                usuarioDb.Id.ToString()),

            new Claim(
                ClaimTypes.Name,
                usuarioDb.UsuarioNombre),

            new Claim(
                "nombre",
                usuarioDb.Nombre ?? ""),

            new Claim(
                ClaimTypes.Role,
                usuarioDb.Rol)
        };

        var clave = _configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(clave))
        {
            throw new InvalidOperationException(
                "No está configurada la clave JWT.");
        }

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(clave));

        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    public async Task<bool> RegistrarAsync(
        string usuario,
        string password,
        string? nombre,
        string? email)
    {
        var usuarioExiste = await _db.Usuarios
            .AnyAsync(u => u.UsuarioNombre == usuario);

        if (usuarioExiste)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailExiste = await _db.Usuarios
                .AnyAsync(u => u.Email == email);

            if (emailExiste)
            {
                return false;
            }
        }

        var passwordHash =
            BCrypt.Net.BCrypt.HashPassword(password);

        var nuevoUsuario = new Usuario
        {
            UsuarioNombre = usuario,
            PasswordHash = passwordHash,
            Nombre = nombre,
            Email = email,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _db.Usuarios.Add(nuevoUsuario);

        await _db.SaveChangesAsync();

        return true;
    }
}