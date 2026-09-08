using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Casagres.API.Data;
using Casagres.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Npgsql;

namespace Casagres.API.Services;

public class AuthService
{
    private readonly CasagresDbContext _db;
    private readonly IConfiguration _configuration;

    private const string MicrosoftClientId =
    "c21b50d1-67bf-4da2-a167-25e5d3625973";

    private const string MicrosoftAuthority =
        "https://login.microsoftonline.com/common/v2.0";


    private readonly ConfigurationManager<OpenIdConnectConfiguration>
        _microsoftConfigurationManager;
    public AuthService(
        CasagresDbContext db,
        IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;

        var metadataAddress =
        $"{MicrosoftAuthority}/.well-known/openid-configuration";

        _microsoftConfigurationManager =
            new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever
                {
                    RequireHttps = true
                });
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

    public async Task<string?> LoginConMicrosoftAsync(
        string idToken)
    {
        var configuration =
            await _microsoftConfigurationManager
                .GetConfigurationAsync(
                    CancellationToken.None);

        var tokenHandler = new JwtSecurityTokenHandler();

        var validationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,

                IssuerSigningKeys =
                    configuration.SigningKeys,

                ValidateIssuer = true,

                ValidateAudience = true,

                ValidAudience =
                    MicrosoftClientId,

                ValidateLifetime = true,

                ClockSkew = TimeSpan.FromMinutes(2),

                IssuerValidator =
                    (issuer, securityToken, parameters) =>
                    {
                        if (securityToken is not JwtSecurityToken jwt)
                        {
                            throw new SecurityTokenException(
                                "El token de Microsoft no es un JWT válido.");
                        }

                        var tid =
                            jwt.Claims
                                .FirstOrDefault(
                                    c => c.Type == "tid")
                                ?.Value;

                        if (string.IsNullOrWhiteSpace(tid))
                        {
                            throw new SecurityTokenException(
                                "El token de Microsoft no contiene tid.");
                        }

                        var issuerEsperado =
                            $"https://login.microsoftonline.com/{tid}/v2.0";

                        if (!string.Equals(
                                issuer,
                                issuerEsperado,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            throw new SecurityTokenInvalidIssuerException(
                                "El emisor del token de Microsoft no es válido.");
                        }

                        return issuer;
                    }
            };

        ClaimsPrincipal principal;

        try
        {
            principal =
                tokenHandler.ValidateToken(
                    idToken,
                    validationParameters,
                    out _);
        }
        catch (SecurityTokenException ex)
        {
            Console.WriteLine(
                $"Token Microsoft rechazado: {ex.Message}");

            return null;
        }

        var email =
            principal.FindFirst(
                "preferred_username")?.Value;

        var nombre =
            principal.FindFirst(
                "name")?.Value;

        var objectId =
            principal.FindFirst(
                "oid")?.Value;

        var tenantId =
            principal.FindFirst(
                "tid")?.Value;

        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        Console.WriteLine();
        Console.WriteLine(
            "==========================================");
        Console.WriteLine(
            "AUTENTICACIÓN MICROSOFT VALIDADA");
        Console.WriteLine(
            "==========================================");
        Console.WriteLine($"Email: {email}");
        Console.WriteLine($"Nombre: {nombre}");
        Console.WriteLine($"OID: {objectId}");
        Console.WriteLine($"Tenant: {tenantId}");
        Console.WriteLine(
            "==========================================");

        // Buscar primero por correo electrónico.
        var usuarioDb =
            await _db.Usuarios
                .FirstOrDefaultAsync(
                    u => u.Email == email);

        // Si el usuario ya existe.
        if (usuarioDb != null)
        {
            if (!usuarioDb.Activo)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(nombre))
            {
                usuarioDb.Nombre = nombre;
            }

            await _db.SaveChangesAsync();

            Console.WriteLine(
                $"Usuario Microsoft existente: {email}");

            return GenerarJwt(usuarioDb);
        }

        // El usuario no existe.
        // Generamos un nombre de usuario único.
        var usuarioNombre = email;

        var existeUsuario =
            await _db.Usuarios
                .AnyAsync(
                    u => u.UsuarioNombre == usuarioNombre);

        if (existeUsuario)
        {
            usuarioNombre =
                $"{email}_microsoft";

            var contador = 1;

            while (
                await _db.Usuarios
                    .AnyAsync(
                        u => u.UsuarioNombre == usuarioNombre))
            {
                usuarioNombre =
                    $"{email}_microsoft{contador}";

                contador++;
            }
        }

        usuarioDb = new Models.Usuario
        {
            UsuarioNombre = usuarioNombre,

            Nombre = nombre ?? email,

            Email = email,

            Rol = "usuario",

            Activo = true,

            FechaCreacion = DateTime.UtcNow,

            PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(
                    Guid.NewGuid().ToString())
        };

        _db.Usuarios.Add(usuarioDb);

        try
        {
            await _db.SaveChangesAsync();

            Console.WriteLine(
                $"Nuevo usuario Microsoft creado: {email}");

            return GenerarJwt(usuarioDb);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException postgresException &&
                  postgresException.SqlState == "23505")
        {
            Console.WriteLine(
                $"El usuario Microsoft ya existía: {email}");

            // El INSERT pudo haberse completado aunque la respuesta
            // haya provocado un reintento. Buscamos nuevamente por email.
            var usuarioExistente =
                await _db.Usuarios
                    .FirstOrDefaultAsync(
                        u => u.Email == email);

            if (usuarioExistente == null)
            {
                throw;
            }

            if (!usuarioExistente.Activo)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(nombre))
            {
                usuarioExistente.Nombre = nombre;

                await _db.SaveChangesAsync();
            }

            return GenerarJwt(usuarioExistente);
        }
    }


    private string GenerarJwt(Models.Usuario usuario)
    {
        var claims = new[]
        {
        new Claim(
            ClaimTypes.NameIdentifier,
            usuario.Id.ToString()),

        new Claim(
            ClaimTypes.Name,
            usuario.UsuarioNombre),

        new Claim(
            "nombre",
            usuario.Nombre ?? ""),

        new Claim(
            ClaimTypes.Role,
            usuario.Rol)
    };

        var clave =
            _configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(clave))
        {
            throw new InvalidOperationException(
                "No está configurada la clave JWT.");
        }

        var securityKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(clave));

        var credentials =
            new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

        var token =
            new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

}