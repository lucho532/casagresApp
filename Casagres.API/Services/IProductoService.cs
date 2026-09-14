using Casagres.API.Models;

namespace Casagres.API.Services;

public interface IProductoService
{
    List<ProductoCatalogo> ObtenerProductos();

    void LimpiarCache();
}
