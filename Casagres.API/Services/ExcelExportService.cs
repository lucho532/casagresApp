using Casagres.API.Data;
using Casagres.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Casagres.API.Services;

public class ExcelExportService
{
    private readonly CasagresDbContext _context;
    private readonly IConfiguration _configuration;

    // Cada columna del CSV se define junto a su selector, para que el
    // encabezado y el dato correspondiente nunca puedan desalinearse.
    private static readonly (string Encabezado, Func<Venta, object?> Valor)[] Columnas =
    {
        ("referencia_producto", v => v.referencia_producto),
        ("descripcion_producto", v => v.descripcion_producto),
        ("zona_vendedor", v => v.zona_vendedor),
        ("nombre_zona_vendedor", v => v.nombre_zona_vendedor),
        ("tercero", v => v.tercero),
        ("nombre_tercero", v => v.nombre_tercero),
        ("calificacion_tercero", v => v.calificacion_tercero),
        ("tipo_tercero", v => v.tipo_tercero),
        ("lista_de_precio", v => v.lista_de_precio),
        ("cod_ciudad", v => v.cod_ciudad),
        ("sucursal_tercero", v => v.sucursal_tercero),
        ("periodo", v => v.periodo),
        ("valor_neto", v => v.valor_neto),
        ("cantidad", v => v.cantidad),
        ("cantidad_devolucion", v => v.cantidad_devolucion),
        ("valor_venta", v => v.valor_venta),
        ("cantidad_notas", v => v.cantidad_notas),
        ("valor_notas", v => v.valor_notas),
        ("tipo", v => v.tipo),
        ("codigo_zona", v => v.codigo_zona),
        ("canal", v => v.canal),
        ("lista_de_precios", v => v.lista_de_precios),
        ("departamento", v => v.departamento),
        ("marca", v => v.marca),
        ("linea_inventarios", v => v.linea_inventarios),
        ("descripcion_linea_inventarios", v => v.descripcion_linea_inventarios),
        ("grupo_inventarios", v => v.grupo_inventarios),
        ("descripcion_grupo_inventarios", v => v.descripcion_grupo_inventarios),
        ("cod_producto", v => v.cod_producto),
        ("descripcion", v => v.descripcion),
        ("clase_producto", v => v.clase_producto),
        ("peso", v => v.peso),
        ("toneladas", v => v.toneladas),
        ("cantidad_neta", v => v.cantidad_neta),
        ("valor_venta_neta", v => v.valor_venta_neta),
        ("valor_presupuesto", v => v.valor_presupuesto),
        ("dif", v => v.dif),
        ("precio", v => v.precio),
        ("valor_unitario", v => v.valor_unitario),
        ("diferencia", v => v.diferencia),
        ("precios_zonas", v => v.precios_zonas),
        ("precios_unicos", v => v.precios_unicos),
        ("periodo_precios", v => v.periodo_precios),
        ("meta_diaria_de_venta", v => v.meta_diaria_de_venta),
        ("cumpli_proyect_$", v => v.cumpli_proyect),
        ("actual", v => v.actual),
        ("zona", v => v.zona)
    };

    public ExcelExportService(
        CasagresDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<List<string>> GenerarCsvVentas()
    {
        var ventas = await ObtenerVentasAsync();
        var carpetaDatos = ObtenerCarpetaDestino();
        var años = ObtenerAniosDisponibles(ventas);

        var archivosGenerados = new List<string>();

        foreach (var año in años)
        {
            var ventasDelAño = ventas
                .Where(v => v.periodo.HasValue && v.periodo.Value / 100 == año)
                .ToList();

            archivosGenerados.Add(
                EscribirCsvDelAño(carpetaDatos, año, ventasDelAño));
        }

        return archivosGenerados;
    }

    private async Task<List<Venta>> ObtenerVentasAsync()
    {
        var ventas = await _context.Ventas.AsNoTracking().ToListAsync();

        if (ventas.Count == 0)
        {
            throw new InvalidOperationException("No existen ventas en la base de datos.");
        }

        return ventas;
    }

    private string ObtenerCarpetaDestino()
    {
        var carpetaDatos = _configuration["Rutas:CarpetaDatos"];

        if (string.IsNullOrWhiteSpace(carpetaDatos))
        {
            throw new InvalidOperationException(
                "No se configuró Rutas:CarpetaDatos en appsettings.json.");
        }

        Directory.CreateDirectory(carpetaDatos);

        return carpetaDatos;
    }

    private static List<int> ObtenerAniosDisponibles(List<Venta> ventas)
    {
        var años = ventas
            .Where(v => v.periodo.HasValue)
            .Select(v => v.periodo!.Value / 100)
            .Distinct()
            .OrderBy(año => año)
            .ToList();

        if (años.Count == 0)
        {
            throw new InvalidOperationException(
                "No se encontraron períodos válidos para determinar los años.");
        }

        return años;
    }

    private static string EscribirCsvDelAño(string carpetaDatos, int año, List<Venta> ventasDelAño)
    {
        Console.WriteLine($"Generando CSV del año {año}: {ventasDelAño.Count} registros...");

        var rutaCsv = Path.Combine(carpetaDatos, $"Ventas_Casagres_{año}.csv");

        // UTF-8 con BOM para que Excel reconozca correctamente
        // caracteres como ñ, á, é, etc.
        using var writer = new StreamWriter(rutaCsv, false, new UTF8Encoding(true));

        writer.WriteLine(string.Join(";", Columnas.Select(c => EscaparCsv(c.Encabezado))));

        foreach (var venta in ventasDelAño)
        {
            writer.WriteLine(string.Join(";", Columnas.Select(c => EscaparCsv(c.Valor(venta)))));
        }

        Console.WriteLine($"✓ CSV generado: {rutaCsv}");

        return rutaCsv;
    }

    // Escapa correctamente los valores para CSV
    private static string EscaparCsv(object? valor)
    {
        if (valor == null)
            return "";

        var texto = valor.ToString() ?? "";

        // Si contiene ;, comillas o saltos de línea,
        // lo encerramos entre comillas.
        if (texto.Contains(";") || texto.Contains("\"") || texto.Contains("\n") || texto.Contains("\r"))
        {
            texto = texto.Replace("\"", "\"\"");
            return $"\"{texto}\"";
        }

        return texto;
    }
}
