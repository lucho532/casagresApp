namespace Casagres.API.Services;

public interface IProductoService
{
    List<object> ObtenerProductos();

    void LimpiarCache();
}
