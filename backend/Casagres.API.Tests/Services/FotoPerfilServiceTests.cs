using Casagres.API.Data;
using Casagres.API.Models;
using Casagres.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Casagres.API.Tests.Services;

public class FotoPerfilServiceTests : IDisposable
{
    private readonly string _carpetaTemporal;

    public FotoPerfilServiceTests()
    {
        _carpetaTemporal = Path.Combine(Path.GetTempPath(), "casagres-fotos-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_carpetaTemporal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_carpetaTemporal))
        {
            Directory.Delete(_carpetaTemporal, recursive: true);
        }
    }

    private static CasagresDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<CasagresDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CasagresDbContext(opciones);
    }

    private FotoPerfilService CrearServicio(CasagresDbContext db)
    {
        var configuracion = new Mock<IConfiguration>();
        configuracion.Setup(c => c["Rutas:CarpetaDatos"]).Returns(_carpetaTemporal);

        return new FotoPerfilService(db, configuracion.Object);
    }

    private static async Task<Usuario> CrearUsuarioAsync(CasagresDbContext db)
    {
        var usuario = new Usuario
        {
            UsuarioNombre = "jperez",
            PasswordHash = "hash",
            Email = "jperez@ejemplo.com",
        };

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        return usuario;
    }

    [Fact]
    public async Task GuardarFotoAsync_ConImagenValida_LaGuardaYActualizaLaUrlDelUsuario()
    {
        await using var db = CrearContexto();
        var usuario = await CrearUsuarioAsync(db);
        var servicio = CrearServicio(db);

        using var contenido = new MemoryStream([1, 2, 3, 4]);

        var url = await servicio.GuardarFotoAsync(usuario.Id, contenido, contenido.Length, "image/jpeg");

        Assert.StartsWith($"/api/auth/foto-perfil/{usuario.Id}?v=", url);
        Assert.Equal(url, usuario.FotoUrl);

        var archivoGuardado = Path.Combine(_carpetaTemporal, "fotos_perfil", $"{usuario.Id}.jpg");
        Assert.True(File.Exists(archivoGuardado));
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, await File.ReadAllBytesAsync(archivoGuardado));
    }

    [Theory]
    [InlineData("image/gif")]
    [InlineData("application/pdf")]
    [InlineData(null)]
    public async Task GuardarFotoAsync_ConFormatoNoSoportado_LanzaArgumentException(string? contentType)
    {
        await using var db = CrearContexto();
        var usuario = await CrearUsuarioAsync(db);
        var servicio = CrearServicio(db);

        using var contenido = new MemoryStream([1, 2, 3]);

        await Assert.ThrowsAsync<ArgumentException>(
            () => servicio.GuardarFotoAsync(usuario.Id, contenido, contenido.Length, contentType));
    }

    [Fact]
    public async Task GuardarFotoAsync_ConImagenDemasiadoGrande_LanzaArgumentException()
    {
        await using var db = CrearContexto();
        var usuario = await CrearUsuarioAsync(db);
        var servicio = CrearServicio(db);

        using var contenido = new MemoryStream(new byte[10]);
        const long tresMbMasUnByte = 3 * 1024 * 1024 + 1;

        await Assert.ThrowsAsync<ArgumentException>(
            () => servicio.GuardarFotoAsync(usuario.Id, contenido, tresMbMasUnByte, "image/jpeg"));
    }

    [Fact]
    public async Task GuardarFotoAsync_ConUsuarioInexistente_LanzaInvalidOperationException()
    {
        await using var db = CrearContexto();
        var servicio = CrearServicio(db);

        using var contenido = new MemoryStream([1, 2, 3]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.GuardarFotoAsync(9999, contenido, contenido.Length, "image/png"));
    }

    [Fact]
    public async Task GuardarFotoAsync_AlSubirUnaNuevaConOtroFormato_BorraElArchivoAnterior()
    {
        await using var db = CrearContexto();
        var usuario = await CrearUsuarioAsync(db);
        var servicio = CrearServicio(db);

        using (var primera = new MemoryStream([1, 2, 3]))
        {
            await servicio.GuardarFotoAsync(usuario.Id, primera, primera.Length, "image/png");
        }

        var rutaPng = Path.Combine(_carpetaTemporal, "fotos_perfil", $"{usuario.Id}.png");
        Assert.True(File.Exists(rutaPng));

        using (var segunda = new MemoryStream([4, 5, 6]))
        {
            await servicio.GuardarFotoAsync(usuario.Id, segunda, segunda.Length, "image/jpeg");
        }

        var rutaJpg = Path.Combine(_carpetaTemporal, "fotos_perfil", $"{usuario.Id}.jpg");
        Assert.True(File.Exists(rutaJpg));
        Assert.False(File.Exists(rutaPng));
    }

    [Fact]
    public async Task ObtenerFoto_ConFotoExistente_DevuelveElContenidoYElTipo()
    {
        await using var db = CrearContexto();
        var usuario = await CrearUsuarioAsync(db);
        var servicio = CrearServicio(db);

        using (var contenido = new MemoryStream([9, 9, 9]))
        {
            await servicio.GuardarFotoAsync(usuario.Id, contenido, contenido.Length, "image/webp");
        }

        var resultado = servicio.ObtenerFoto(usuario.Id);

        Assert.NotNull(resultado);
        Assert.Equal("image/webp", resultado.Value.ContentType);

        using var contenidoLeido = resultado.Value.Contenido;
        using var memoria = new MemoryStream();
        await contenidoLeido.CopyToAsync(memoria);

        Assert.Equal(new byte[] { 9, 9, 9 }, memoria.ToArray());
    }

    [Fact]
    public void ObtenerFoto_SinNingunaFotoSubida_DevuelveNull()
    {
        using var db = CrearContexto();
        var servicio = CrearServicio(db);

        Assert.Null(servicio.ObtenerFoto(1));
    }
}
