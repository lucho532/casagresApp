using Casagres.API.Controllers;
using Casagres.API.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Casagres.API.Tests.Controllers;

public class ProductosControllerTests
{
    private readonly Mock<IProductoService> _productoService = new();

    private ProductosController CrearController() => new(_productoService.Object);

    [Fact]
    public void ObtenerProductos_CuandoElServicioTieneExito_DevuelveOkConLaCantidad()
    {
        _productoService
            .Setup(s => s.ObtenerProductos())
            .Returns(new List<object> { new { referencia = "REF1" }, new { referencia = "REF2" } });

        var resultado = Assert.IsType<OkObjectResult>(CrearController().ObtenerProductos());

        Assert.NotNull(resultado.Value);
    }

    [Fact]
    public void ObtenerProductos_CuandoNoExisteElArchivo_Devuelve404()
    {
        _productoService
            .Setup(s => s.ObtenerProductos())
            .Throws(new FileNotFoundException("No se encontró el archivo."));

        Assert.IsType<NotFoundObjectResult>(CrearController().ObtenerProductos());
    }

    [Fact]
    public void ObtenerProductos_ConErrorInesperado_Devuelve500()
    {
        _productoService.Setup(s => s.ObtenerProductos()).Throws(new Exception("boom"));

        var resultado = Assert.IsType<ObjectResult>(CrearController().ObtenerProductos());

        Assert.Equal(500, resultado.StatusCode);
    }
}
