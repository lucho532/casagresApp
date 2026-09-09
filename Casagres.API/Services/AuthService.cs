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
                new HttpDocumentRetriever { RequireHttps = true });
    }

    // ============================================================
    // LOGIN CON USUARIO Y CONTRASEÑA
    // ============================================================

    public async Task<string?> LoginAsync(string usuario, string password)
    {
        var usuarioDb = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.UsuarioNombre == usuario && u.Activo);

        if (usuarioDb == null)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(password, usuarioDb.PasswordHash))
            return null;

        return GenerarJwt(usuarioDb);
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

        var nuevoUsuario = new Usuario
        {
            UsuarioNombre = usuario,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Nombre = nombre,
            Email = email,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _db.Usuarios.Add(nuevoUsuario);

        await _db.SaveChangesAsync();

        return true;
    }

    // ============================================================
    // LOGIN CON MICROSOFT
    // ============================================================

    public async Task<string?> LoginConMicrosoftAsync(string idToken)
    {
        var principal = await ValidarTokenMicrosoftAsync(idToken);

        if (principal == null)
            return null;

        var datos = ExtraerDatosUsuario(principal);

        if (datos == null)
            return null;

        LogAutenticacionMicrosoft(datos.Value);

        var usuario = await ObtenerOCrearUsuarioMicrosoftAsync(datos.Value);

        return usuario != null ? GenerarJwt(usuario) : null;
    }

    private async Task<ClaimsPrincipal?> ValidarTokenMicrosoftAsync(string idToken)
    {
        var configuration = await _microsoftConfigurationManager
            .GetConfigurationAsync(CancellationToken.None);

        var validationParameters = ConstruirParametrosValidacionMicrosoft(configuration);

        try
        {
            return new JwtSecurityTokenHandler()
                .ValidateToken(idToken, validationParameters, out _);
        }
        catch (SecurityTokenException ex)
        {
            Console.WriteLine($"Token Microsoft rechazado: {ex.Message}");
            return null;
        }
    }

    private static TokenValidationParameters ConstruirParametrosValidacionMicrosoft(
        OpenIdConnectConfiguration configuration) =>
        new()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = configuration.SigningKeys,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudience = MicrosoftClientId,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
            IssuerValidator = ValidarEmisorMicrosoft
        };

    private static string ValidarEmisorMicrosoft(
        string issuer,
        SecurityToken securityToken,
        TokenValidationParameters parameters)
    {
        if (securityToken is not JwtSecurityToken jwt)
        {
            throw new SecurityTokenException(
                "El token de Microsoft no es un JWT válido.");
        }

        var tid = jwt.Claims.FirstOrDefault(c => c.Type == "tid")?.Value;

        if (string.IsNullOrWhiteSpace(tid))
        {
            throw new SecurityTokenException(
                "El token de Microsoft no contiene tid.");
        }

        var issuerEsperado = $"https://login.microsoftonline.com/{tid}/v2.0";

        if (!string.Equals(issuer, issuerEsperado, StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityTokenInvalidIssuerException(
                "El emisor del token de Microsoft no es válido.");
        }

        return issuer;
    }

    private static DatosUsuarioMicrosoft? ExtraerDatosUsuario(ClaimsPrincipal principal)
    {
        var email = principal.FindFirst("preferred_username")?.Value;

        if (string.IsNullOrWhiteSpace(email))
            return null;

        return new DatosUsuarioMicrosoft(
            email,
            principal.FindFirst("name")?.Value,
            principal.FindFirst("oid")?.Value,
            principal.FindFirst("tid")?.Value);
    }

    private static void LogAutenticacionMicrosoft(DatosUsuarioMicrosoft datos)
    {
        Console.WriteLine();
        Console.WriteLine("==========================================");
        Console.WriteLine("AUTENTICACIÓN MICROSOFT VALIDADA");
        Console.WriteLine("==========================================");
        Console.WriteLine($"Email: {datos.Email}");
        Console.WriteLine($"Nombre: {datos.Nombre}");
        Console.WriteLine($"OID: {datos.ObjectId}");
        Console.WriteLine($"Tenant: {datos.TenantId}");
        Console.WriteLine("==========================================");
    }

    private async Task<Usuario?> ObtenerOCrearUsuarioMicrosoftAsync(DatosUsuarioMicrosoft datos)
    {
        var usuarioExistente = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Email == datos.Email);

        return usuarioExistente != null
            ? await ActualizarUsuarioExistenteAsync(usuarioExistente, datos.Nombre)
            : await CrearUsuarioMicrosoftAsync(datos);
    }

    private async Task<Usuario?> ActualizarUsuarioExistenteAsync(Usuario usuario, string? nombre)
    {
        if (!usuario.Activo)
            return null;

        if (!string.IsNullOrWhiteSpace(nombre))
        {
            usuario.Nombre = nombre;
        }

        await _db.SaveChangesAsync();

        Console.WriteLine($"Usuario Microsoft existente: {usuario.Email}");

        return usuario;
    }

    private async Task<Usuario?> CrearUsuarioMicrosoftAsync(DatosUsuarioMicrosoft datos)
    {
        var usuarioNombre = await GenerarNombreUsuarioUnicoAsync(datos.Email);
        var usuario = ConstruirUsuarioMicrosoft(datos, usuarioNombre);

        _db.Usuarios.Add(usuario);

        try
        {
            await _db.SaveChangesAsync();

            Console.WriteLine($"Nuevo usuario Microsoft creado: {datos.Email}");

            return usuario;
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException postgresException &&
                  postgresException.SqlState == "23505")
        {
            Console.WriteLine($"El usuario Microsoft ya existía: {datos.Email}");

            // El INSERT pudo haberse completado aunque la respuesta
            // haya provocado un reintento. Buscamos nuevamente por email.
            var usuarioExistente = await _db.Usuarios
                .FirstOrDefaultAsync(u => u.Email == datos.Email);

            if (usuarioExistente == null)
                throw;

            return await ActualizarNombreSiCorrespondeAsync(usuarioExistente, datos.Nombre);
        }
    }

    private async Task<Usuario?> ActualizarNombreSiCorrespondeAsync(Usuario usuario, string? nombre)
    {
        if (!usuario.Activo)
            return null;

        if (!string.IsNullOrWhiteSpace(nombre))
        {
            usuario.Nombre = nombre;
            await _db.SaveChangesAsync();
        }

        return usuario;
    }

    private async Task<string> GenerarNombreUsuarioUnicoAsync(string email)
    {
        if (!await _db.Usuarios.AnyAsync(u => u.UsuarioNombre == email))
            return email;

        var candidato = $"{email}_microsoft";
        var contador = 1;

        while (await _db.Usuarios.AnyAsync(u => u.UsuarioNombre == candidato))
        {
            candidato = $"{email}_microsoft{contador}";
            contador++;
        }

        return candidato;
    }

    private static Usuario ConstruirUsuarioMicrosoft(DatosUsuarioMicrosoft datos, string usuarioNombre) =>
        new()
        {
            UsuarioNombre = usuarioNombre,
            Nombre = datos.Nombre ?? datos.Email,
            Email = datos.Email,
            Rol = "usuario",
            Activo = true,
            FechaCreacion = DateTime.UtcNow,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString())
        };

    // ============================================================
    // JWT
    // ============================================================

    private string GenerarJwt(Usuario usuario)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.UsuarioNombre),
            new Claim("nombre", usuario.Nombre ?? ""),
            new Claim(ClaimTypes.Role, usuario.Rol)
        };

        var clave = _configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(clave))
        {
            throw new InvalidOperationException("No está configurada la clave JWT.");
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private readonly record struct DatosUsuarioMicrosoft(
        string Email, string? Nombre, string? ObjectId, string? TenantId);
}
