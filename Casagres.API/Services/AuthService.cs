using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Casagres.API.Data;
using Casagres.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Npgsql;

namespace Casagres.API.Services;

public class AuthService : IAuthService
{
    private readonly CasagresDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEmailVerificationService _emailVerificationService;

    private const string MicrosoftClientId =
        "c21b50d1-67bf-4da2-a167-25e5d3625973";

    private const string MicrosoftAuthority =
        "https://login.microsoftonline.com/common/v2.0";

    private const string GoogleUserInfoUrl =
        "https://www.googleapis.com/oauth2/v3/userinfo";

    private readonly ConfigurationManager<OpenIdConnectConfiguration>
        _microsoftConfigurationManager;

    public AuthService(
        CasagresDbContext db,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IEmailVerificationService emailVerificationService)
    {
        _db = db;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _emailVerificationService = emailVerificationService;

        _microsoftConfigurationManager = CrearAdministradorConfiguracionOidc(
            $"{MicrosoftAuthority}/.well-known/openid-configuration");
    }

    private static ConfigurationManager<OpenIdConnectConfiguration>
        CrearAdministradorConfiguracionOidc(string metadataAddress) =>
        new(
            metadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever { RequireHttps = true });

    // ============================================================
    // PERFIL
    // ============================================================

    public async Task<Usuario?> ObtenerPorIdAsync(long id) =>
        await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id);

    // ============================================================
    // LOGIN CON CORREO Y CONTRASEÑA
    // ============================================================

    public async Task<string?> LoginAsync(string email, string password)
    {
        email = email.Trim();

        var usuarioDb = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Email == email && u.Activo);

        if (usuarioDb == null)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(password, usuarioDb.PasswordHash))
            return null;

        if (!usuarioDb.EmailVerificado)
        {
            throw new EmailNoVerificadoException(usuarioDb.Email);
        }

        return GenerarJwt(usuarioDb);
    }

    public async Task<bool> RegistrarAsync(
        string password,
        string nombre,
        string email)
    {
        nombre = nombre.Trim();
        email = email.Trim();

        var emailExiste = await _db.Usuarios
            .AnyAsync(u => u.Email == email);

        if (emailExiste)
        {
            return false;
        }

        var usuarioNombre = await GenerarNombreUsuarioUnicoAsync(email, sufijoUsuario: "registro");

        var nuevoUsuario = new Usuario
        {
            UsuarioNombre = usuarioNombre,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Nombre = nombre,
            Email = email,
            Activo = true,
            FechaCreacion = DateTime.UtcNow,
            EmailVerificado = false,
            // Queda pendiente de aprobación por un admin: no debe ver
            // datos de la empresa hasta que le cambien el rol.
            Rol = Roles.Pendiente
        };

        _db.Usuarios.Add(nuevoUsuario);

        await _db.SaveChangesAsync();

        // El envío del correo (SMTP) puede tardar bastante -incluso hasta
        // el límite completo de espera si el proveedor está siendo lento
        // con una IP nueva, como ya pasó antes- y el usuario no necesita
        // esperar eso para saber que su cuenta se creó: se dispara en
        // segundo plano en vez de bloquear la respuesta del registro.
        _ = EnviarVerificacionSinFallarElRegistroAsync(nuevoUsuario);

        return true;
    }

    private async Task EnviarVerificacionSinFallarElRegistroAsync(Usuario usuario)
    {
        try
        {
            await _emailVerificationService.EnviarCorreoDeVerificacionAsync(usuario);
        }
        catch (Exception ex)
        {
            // El usuario ya quedó creado; un fallo de correo no debe
            // convertir un registro exitoso en un error 500. Queda la
            // opción de reenviar la verificación más adelante.
            Console.WriteLine($"No fue posible enviar el correo de verificación: {ex.Message}");
        }
    }

    // ============================================================
    // LOGIN CON MICROSOFT
    // ============================================================

    public async Task<string?> LoginConMicrosoftAsync(string idToken) =>
        await LoginConProveedorExternoAsync(
            idToken,
            _microsoftConfigurationManager,
            ConstruirParametrosValidacionMicrosoft,
            ExtraerDatosUsuarioMicrosoft,
            proveedor: "MICROSOFT",
            sufijoUsuario: "microsoft");

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

    private static DatosUsuarioExterno? ExtraerDatosUsuarioMicrosoft(ClaimsPrincipal principal)
    {
        var email = principal.FindFirst("preferred_username")?.Value;

        if (string.IsNullOrWhiteSpace(email))
            return null;

        return new DatosUsuarioExterno(email, principal.FindFirst("name")?.Value);
    }

    // ============================================================
    // LOGIN CON GOOGLE
    // ============================================================
    //
    // A diferencia de Microsoft (que entrega un id_token / JWT firmado que
    // se valida localmente contra las claves públicas de Azure), el botón
    // personalizado de Google (useGoogleLogin) entrega un access_token de
    // OAuth2 puro. Ese token no es un JWT: se valida pidiéndole a Google
    // los datos del usuario con él. Si el token es inválido o expiró,
    // Google responde con error y la petición falla.

    public async Task<string?> LoginConGoogleAsync(string accessToken)
    {
        var datos = await ObtenerDatosUsuarioGoogleAsync(accessToken);

        if (datos == null)
            return null;

        LogAutenticacionExterna("GOOGLE", datos.Value);

        var usuario = await ObtenerOCrearUsuarioExternoAsync(datos.Value, sufijoUsuario: "google");

        return usuario != null ? GenerarJwt(usuario) : null;
    }

    private async Task<DatosUsuarioExterno?> ObtenerDatosUsuarioGoogleAsync(string accessToken)
    {
        var cliente = _httpClientFactory.CreateClient();

        using var solicitud = new HttpRequestMessage(HttpMethod.Get, GoogleUserInfoUrl);
        solicitud.Headers.Authorization = new("Bearer", accessToken);

        using var respuesta = await cliente.SendAsync(solicitud);

        if (!respuesta.IsSuccessStatusCode)
        {
            Console.WriteLine(
                $"Token GOOGLE rechazado: Google respondió {(int)respuesta.StatusCode}.");
            return null;
        }

        using var contenido = await respuesta.Content.ReadAsStreamAsync();
        using var perfil = await JsonDocument.ParseAsync(contenido);

        if (!perfil.RootElement.TryGetProperty("email", out var emailElemento))
            return null;

        var email = emailElemento.GetString();

        if (string.IsNullOrWhiteSpace(email))
            return null;

        var nombre = perfil.RootElement.TryGetProperty("name", out var nombreElemento)
            ? nombreElemento.GetString()
            : null;

        return new DatosUsuarioExterno(email, nombre);
    }

    // ============================================================
    // FLUJO COMÚN PARA PROVEEDORES EXTERNOS (MICROSOFT / GOOGLE)
    // ============================================================

    private async Task<string?> LoginConProveedorExternoAsync(
        string idToken,
        ConfigurationManager<OpenIdConnectConfiguration> configurationManager,
        Func<OpenIdConnectConfiguration, TokenValidationParameters> construirParametros,
        Func<ClaimsPrincipal, DatosUsuarioExterno?> extraerDatos,
        string proveedor,
        string sufijoUsuario)
    {
        var principal = await ValidarTokenExternoAsync(
            idToken, configurationManager, construirParametros, proveedor);

        if (principal == null)
            return null;

        var datos = extraerDatos(principal);

        if (datos == null)
            return null;

        LogAutenticacionExterna(proveedor, datos.Value);

        var usuario = await ObtenerOCrearUsuarioExternoAsync(datos.Value, sufijoUsuario);

        return usuario != null ? GenerarJwt(usuario) : null;
    }

    private async Task<ClaimsPrincipal?> ValidarTokenExternoAsync(
        string idToken,
        ConfigurationManager<OpenIdConnectConfiguration> configurationManager,
        Func<OpenIdConnectConfiguration, TokenValidationParameters> construirParametros,
        string proveedor)
    {
        var configuration = await configurationManager
            .GetConfigurationAsync(CancellationToken.None);

        var validationParameters = construirParametros(configuration);

        try
        {
            return new JwtSecurityTokenHandler()
                .ValidateToken(idToken, validationParameters, out _);
        }
        catch (SecurityTokenException ex)
        {
            Console.WriteLine($"Token {proveedor} rechazado: {ex.Message}");
            return null;
        }
    }

    private static void LogAutenticacionExterna(string proveedor, DatosUsuarioExterno datos)
    {
        Console.WriteLine();
        Console.WriteLine("==========================================");
        Console.WriteLine($"AUTENTICACIÓN {proveedor} VALIDADA");
        Console.WriteLine("==========================================");
        Console.WriteLine($"Email: {datos.Email}");
        Console.WriteLine($"Nombre: {datos.Nombre}");
        Console.WriteLine("==========================================");
    }

    private async Task<Usuario?> ObtenerOCrearUsuarioExternoAsync(
        DatosUsuarioExterno datos, string sufijoUsuario)
    {
        var usuarioExistente = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Email == datos.Email);

        return usuarioExistente != null
            ? await ActualizarUsuarioExistenteAsync(usuarioExistente, datos.Nombre)
            : await CrearUsuarioExternoAsync(datos, sufijoUsuario);
    }

    private async Task<Usuario?> ActualizarUsuarioExistenteAsync(Usuario usuario, string? nombre)
    {
        if (!usuario.Activo)
            return null;

        if (!string.IsNullOrWhiteSpace(nombre))
        {
            usuario.Nombre = nombre;
        }

        // Si inició sesión con un proveedor externo, ese proveedor ya
        // verificó que el correo le pertenece.
        usuario.EmailVerificado = true;

        await _db.SaveChangesAsync();

        Console.WriteLine($"Usuario externo existente: {usuario.Email}");

        return usuario;
    }

    private async Task<Usuario?> CrearUsuarioExternoAsync(DatosUsuarioExterno datos, string sufijoUsuario)
    {
        var usuarioNombre = await GenerarNombreUsuarioUnicoAsync(datos.Email, sufijoUsuario);
        var usuario = ConstruirUsuarioExterno(datos, usuarioNombre);

        _db.Usuarios.Add(usuario);

        try
        {
            await _db.SaveChangesAsync();

            Console.WriteLine($"Nuevo usuario externo creado: {datos.Email}");

            return usuario;
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException postgresException &&
                  postgresException.SqlState == "23505")
        {
            Console.WriteLine($"El usuario externo ya existía: {datos.Email}");

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

    private async Task<string> GenerarNombreUsuarioUnicoAsync(string email, string sufijoUsuario)
    {
        if (!await _db.Usuarios.AnyAsync(u => u.UsuarioNombre == email))
            return email;

        var candidato = $"{email}_{sufijoUsuario}";
        var contador = 1;

        while (await _db.Usuarios.AnyAsync(u => u.UsuarioNombre == candidato))
        {
            candidato = $"{email}_{sufijoUsuario}{contador}";
            contador++;
        }

        return candidato;
    }

    private static Usuario ConstruirUsuarioExterno(DatosUsuarioExterno datos, string usuarioNombre) =>
        new()
        {
            UsuarioNombre = usuarioNombre,
            Nombre = datos.Nombre ?? datos.Email,
            Email = datos.Email,
            // Cualquiera con una cuenta de Microsoft/Google puede
            // autenticarse aquí por primera vez: igual que en el registro
            // manual, queda pendiente de aprobación por un admin.
            Rol = Roles.Pendiente,
            Activo = true,
            FechaCreacion = DateTime.UtcNow,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
            // El proveedor externo (Microsoft/Google) ya verificó este correo.
            EmailVerificado = true
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

    private readonly record struct DatosUsuarioExterno(string Email, string? Nombre);
}
