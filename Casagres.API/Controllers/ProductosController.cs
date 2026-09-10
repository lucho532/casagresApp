using Casagres.API.Models;
using Casagres.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;




namespace Casagres.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.ConAccesoADatos)]
public class ProductosController : ControllerBase
{
    private readonly IProductoService _productoService;

    public ProductosController(
        IProductoService productoService)
    {
        _productoService = productoService;
    }

    // =========================================
    // OBTENER TODOS LOS PRODUCTOS
    // =========================================

    [HttpGet]
    public IActionResult ObtenerProductos()
    {
        try
        {
            var productos =
                _productoService.ObtenerProductos();

            return Ok(new
            {
                cantidad = productos.Count,
                productos
            });
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new
            {
                estado = "ERROR",
                mensaje = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                estado = "ERROR",
                mensaje =
                    "Error obteniendo el catálogo de productos.",
                detalle = ex.Message
            });
        }
    }
}