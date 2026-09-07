using Casagres.API.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Casagres.API.Services;

public class ExcelExportService
{
    private readonly CasagresDbContext _context;
    private readonly IConfiguration _configuration;

    public ExcelExportService(
        CasagresDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<List<string>> GenerarCsvVentas()
    {
        // 1. Obtener todas las ventas desde SQL Server
        var ventas = await _context.Ventas
            .AsNoTracking()
            .ToListAsync();

        if (ventas.Count == 0)
        {
            throw new InvalidOperationException(
                "No existen ventas en la base de datos."
            );
        }

        // 2. Obtener la carpeta donde se guardarán los CSV
        var carpetaDatos = _configuration["Rutas:CarpetaDatos"];

        if (string.IsNullOrWhiteSpace(carpetaDatos))
        {
            throw new InvalidOperationException(
                "No se configuró Rutas:CarpetaDatos en appsettings.json."
            );
        }

        Directory.CreateDirectory(carpetaDatos);

        // 3. Obtener los años existentes a partir de 'periodo'
        var años = ventas
            .Where(v => v.periodo.HasValue)
            .Select(v => v.periodo!.Value / 100)
            .Distinct()
            .OrderBy(año => año)
            .ToList();

        if (años.Count == 0)
        {
            throw new InvalidOperationException(
                "No se encontraron períodos válidos para determinar los años."
            );
        }

        var archivosGenerados = new List<string>();

        // 4. Encabezados del CSV
        string[] encabezados =
        {
            "referencia_producto",
            "descripcion_producto",
            "zona_vendedor",
            "nombre_zona_vendedor",
            "tercero",
            "nombre_tercero",
            "calificacion_tercero",
            "tipo_tercero",
            "lista_de_precio",
            "cod_ciudad",
            "sucursal_tercero",
            "periodo",
            "valor_neto",
            "cantidad",
            "cantidad_devolucion",
            "valor_venta",
            "cantidad_notas",
            "valor_notas",
            "tipo",
            "codigo_zona",
            "canal",
            "lista_de_precios",
            "departamento",
            "marca",
            "linea_inventarios",
            "descripcion_linea_inventarios",
            "grupo_inventarios",
            "descripcion_grupo_inventarios",
            "cod_producto",
            "descripcion",
            "clase_producto",
            "peso",
            "toneladas",
            "cantidad_neta",
            "valor_venta_neta",
            "valor_presupuesto",
            "dif",
            "precio",
            "valor_unitario",
            "diferencia",
            "precios_zonas",
            "precios_unicos",
            "periodo_precios",
            "meta_diaria_de_venta",
            "cumpli_proyect_$",
            "actual",
            "zona"
        };

        // 5. Generar un CSV por cada año
        foreach (var año in años)
        {
            var ventasDelAño = ventas
                .Where(v =>
                    v.periodo.HasValue &&
                    v.periodo.Value / 100 == año
                )
                .ToList();

            Console.WriteLine(
                $"Generando CSV del año {año}: {ventasDelAño.Count} registros..."
            );

            var nombreArchivo =
                $"Ventas_Casagres_{año}.csv";

            var rutaCsv = Path.Combine(
                carpetaDatos,
                nombreArchivo
            );

            // UTF-8 con BOM para que Excel reconozca correctamente
            // caracteres como ñ, á, é, etc.
            using var writer = new StreamWriter(
                rutaCsv,
                false,
                new UTF8Encoding(true)
            );

            // 6. Escribir encabezados
            writer.WriteLine(
                string.Join(
                    ";",
                    encabezados.Select(EscaparCsv)
                )
            );

            // 7. Escribir datos
            foreach (var v in ventasDelAño)
            {
                object?[] fila =
                {
                    v.referencia_producto,
                    v.descripcion_producto,
                    v.zona_vendedor,
                    v.nombre_zona_vendedor,
                    v.tercero,
                    v.nombre_tercero,
                    v.calificacion_tercero,
                    v.tipo_tercero,
                    v.lista_de_precio,
                    v.cod_ciudad,
                    v.sucursal_tercero,
                    v.periodo,
                    v.valor_neto,
                    v.cantidad,
                    v.cantidad_devolucion,
                    v.valor_venta,
                    v.cantidad_notas,
                    v.valor_notas,
                    v.tipo,
                    v.codigo_zona,
                    v.canal,
                    v.lista_de_precios,
                    v.departamento,
                    v.marca,
                    v.linea_inventarios,
                    v.descripcion_linea_inventarios,
                    v.grupo_inventarios,
                    v.descripcion_grupo_inventarios,
                    v.cod_producto,
                    v.descripcion,
                    v.clase_producto,
                    v.peso,
                    v.toneladas,
                    v.cantidad_neta,
                    v.valor_venta_neta,
                    v.valor_presupuesto,
                    v.dif,
                    v.precio,
                    v.valor_unitario,
                    v.diferencia,
                    v.precios_zonas,
                    v.precios_unicos,
                    v.periodo_precios,
                    v.meta_diaria_de_venta,
                    v.cumpli_proyect,
                    v.actual,
                    v.zona
                };

                writer.WriteLine(
                    string.Join(
                        ";",
                        fila.Select(EscaparCsv)
                    )
                );
            }

            archivosGenerados.Add(rutaCsv);

            Console.WriteLine(
                $"✓ CSV generado: {rutaCsv}"
            );
        }

        return archivosGenerados;
    }

    // Escapa correctamente los valores para CSV
    private static string EscaparCsv(object? valor)
    {
        if (valor == null)
            return "";

        var texto = valor.ToString() ?? "";

        // Si contiene ;, comillas o saltos de línea,
        // lo encerramos entre comillas.
        if (
            texto.Contains(";") ||
            texto.Contains("\"") ||
            texto.Contains("\n") ||
            texto.Contains("\r")
        )
        {
            texto = texto.Replace("\"", "\"\"");
            return $"\"{texto}\"";
        }

        return texto;
    }
}