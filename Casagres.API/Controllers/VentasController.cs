using Casagres.API.Services;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using Casagres.API.Models;

namespace Casagres.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VentasController : ControllerBase
{
    private readonly ExcelExportService _excelExportService;
    private readonly IConfiguration _configuration;

    public VentasController(
        ExcelExportService excelExportService,
        IConfiguration configuration)
    {
        _excelExportService = excelExportService;
        _configuration = configuration;
    }

    [HttpGet("generar-csv")]
    public async Task<IActionResult> GenerarCsv()
    {
        try
        {
            var rutasCsv = await _excelExportService.GenerarCsvVentas();

            return Ok(new
            {
                mensaje = "CSV generados correctamente.",
                archivos = rutasCsv.Select(Path.GetFileName),
                rutas = rutasCsv
            });
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    mensaje = "Error al generar los CSV.",
                    error = ex.Message
                }
            );
        }
    }

    [HttpGet("pronostico")]
    public IActionResult ObtenerPronostico()
    {
        try
        {
            var carpetaDatos =
                _configuration["Rutas:CarpetaDatos"];

            if (string.IsNullOrWhiteSpace(carpetaDatos))
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        mensaje = "No está configurada la ruta de datos."
                    }
                );
            }

            var rutaPronostico = Path.Combine(
                carpetaDatos,
                "salidas_prediccion",
                "pronostico.csv"
            );

            if (!System.IO.File.Exists(rutaPronostico))
            {
                return NotFound(new
                {
                    mensaje = "No se encontró el archivo de pronóstico.",
                    ruta = rutaPronostico
                });
            }

            var lineas =
                System.IO.File.ReadAllLines(
                    rutaPronostico
                );

            if (lineas.Length <= 1)
            {
                return Ok(new
                {
                    mes = (string?)null,
                    cantidadProductos = 0,
                    productos = Array.Empty<object>()
                });
            }

            // Primera línea: encabezados
            var encabezados = lineas[0].Split(',');

            // Segunda línea: datos del pronóstico
            var valores = lineas[1].Split(',');

            var mes = valores.Length > 0
                ? valores[0]
                : null;

            var productos =
                new List<object>();

            for (int i = 1; i < encabezados.Length; i++)
            {
                if (i >= valores.Length)
                    continue;

                var referencia =
                    encabezados[i].Trim();

                if (string.IsNullOrWhiteSpace(referencia))
                    continue;

                if (!double.TryParse(
                        valores[i],
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out double pronostico))
                {
                    continue;
                }

                // No enviamos productos con pronóstico 0
                if (pronostico == 0)
                    continue;

                productos.Add(new
                {
                    referencia,
                    pronostico
                });
            }

            return Ok(new
            {
                mes,
                cantidadProductos = productos.Count,
                productos
            });
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    mensaje = "Error al obtener el pronóstico.",
                    error = ex.Message
                }
            );
        }
    }

    [HttpGet("pronostico-intervalos")]
    public IActionResult ObtenerPronosticoIntervalos()
    {
        try
        {
            var carpetaDatos =
                _configuration["Rutas:CarpetaDatos"];

            if (string.IsNullOrWhiteSpace(carpetaDatos))
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        mensaje = "No está configurada la ruta de datos."
                    }
                );
            }

            var rutaIntervalos = Path.Combine(
                carpetaDatos,
                "salidas_prediccion",
                "pronostico_intervalos.csv"
            );

            if (!System.IO.File.Exists(rutaIntervalos))
            {
                return NotFound(new
                {
                    mensaje = "No se encontró el archivo de intervalos.",
                    ruta = rutaIntervalos
                });
            }

            var lineas =
                System.IO.File.ReadAllLines(rutaIntervalos);

            if (lineas.Length <= 1)
            {
                return Ok(new
                {
                    mes = (string?)null,
                    cantidadProductos = 0,
                    productos = Array.Empty<object>()
                });
            }

            var productos =
                new List<object>();

            for (int i = 1; i < lineas.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lineas[i]))
                    continue;

                var valores =
                    lineas[i].Split(',');

                if (valores.Length < 8)
                    continue;

                var mes = valores[0].Trim();
                var codigoProducto = valores[1].Trim();
                var metodo = valores[2].Trim();

                if (!double.TryParse(
                        valores[3],
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out double prediccion))
                {
                    continue;
                }

                if (!double.TryParse(
                        valores[4],
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out double inferior))
                {
                    continue;
                }

                if (!double.TryParse(
                        valores[5],
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out double superior))
                {
                    continue;
                }

                if (!double.TryParse(
                        valores[6],
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out double halfWidth))
                {
                    continue;
                }

                if (!double.TryParse(
                        valores[7],
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out double alphaAci))
                {
                    continue;
                }

                productos.Add(new
                {
                    referencia = codigoProducto,
                    metodo,
                    prediccion,
                    inferior,
                    superior,
                    halfWidth,
                    alphaAci
                });
            }

            var mesResultado =
                productos.Count > 0
                    ? lineas[1].Split(',')[0].Trim()
                    : null;

            return Ok(new
            {
                mes = mesResultado,
                cantidadProductos = productos.Count,
                productos
            });
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    mensaje = "Error al obtener los intervalos del pronóstico.",
                    error = ex.Message
                }
            );
        }
    }

    [HttpGet("metodos")]
    public IActionResult ObtenerMetodos()
    {
        try
        {
            var carpetaDatos =
                _configuration["Rutas:CarpetaDatos"];

            if (string.IsNullOrWhiteSpace(carpetaDatos))
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        mensaje = "No está configurada la ruta de datos."
                    }
                );
            }

            var rutaMetodos = Path.Combine(
                carpetaDatos,
                "salidas_prediccion",
                "metodo_por_serie.csv"
            );

            if (!System.IO.File.Exists(rutaMetodos))
            {
                return NotFound(new
                {
                    mensaje = "No se encontró el archivo de métodos.",
                    ruta = rutaMetodos
                });
            }

            var lineas =
                System.IO.File.ReadAllLines(rutaMetodos);

            if (lineas.Length <= 1)
            {
                return Ok(new
                {
                    cantidadProductos = 0,
                    productos = Array.Empty<object>()
                });
            }

            var productos =
                new List<object>();

            for (int i = 1; i < lineas.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lineas[i]))
                    continue;

                var valores =
                    lineas[i].Split(',');

                if (valores.Length < 5)
                    continue;

                var codigoProducto = valores[0].Trim();
                var metodo = valores[1].Trim();

                if (!bool.TryParse(
                        valores[2].Trim(),
                        out bool tieneHiperparametros))
                {
                    continue;
                }

                if (!int.TryParse(
                        valores[3].Trim(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int nScoresAci))
                {
                    continue;
                }

                if (!double.TryParse(
                        valores[4].Trim(),
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out double alphaAci))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(codigoProducto))
                    continue;

                productos.Add(new
                {
                    referencia = codigoProducto,
                    metodo,
                    tieneHiperparametros,
                    nScoresAci,
                    alphaAci
                });
            }

            return Ok(new
            {
                cantidadProductos = productos.Count,
                productos
            });
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    mensaje = "Error al obtener los métodos por serie.",
                    error = ex.Message
                }
            );
        }
    }


    [HttpGet("dashboard")]
    public IActionResult ObtenerDashboard()
    {
        try
        {
            var carpetaDatos =
                _configuration["Rutas:CarpetaDatos"];

            if (string.IsNullOrWhiteSpace(carpetaDatos))
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        mensaje = "No está configurada la ruta de datos."
                    }
                );
            }

            var carpetaSalidas = Path.Combine(
                carpetaDatos,
                "salidas_prediccion"
            );

            var rutaPronostico = Path.Combine(
                carpetaSalidas,
                "pronostico.csv"
            );

            var rutaIntervalos = Path.Combine(
                carpetaSalidas,
                "pronostico_intervalos.csv"
            );

            var rutaMetodos = Path.Combine(
                carpetaSalidas,
                "metodo_por_serie.csv"
            );

            if (!System.IO.File.Exists(rutaPronostico))
            {
                return NotFound(new
                {
                    mensaje = "No se encontró pronostico.csv."
                });
            }

            if (!System.IO.File.Exists(rutaIntervalos))
            {
                return NotFound(new
                {
                    mensaje = "No se encontró pronostico_intervalos.csv."
                });
            }

            if (!System.IO.File.Exists(rutaMetodos))
            {
                return NotFound(new
                {
                    mensaje = "No se encontró metodo_por_serie.csv."
                });
            }

            // ============================================
            // PRONÓSTICOS
            // ============================================

            var lineasPronostico =
                System.IO.File.ReadAllLines(rutaPronostico);

            var pronosticos =
                new Dictionary<string, double>(
                    StringComparer.OrdinalIgnoreCase);

            string? mes = null;

            if (lineasPronostico.Length > 1)
            {
                var encabezados =
                    lineasPronostico[0].Split(',');

                var valores =
                    lineasPronostico[1].Split(',');

                if (valores.Length > 0)
                {
                    mes = valores[0].Trim();
                }

                for (int i = 1; i < encabezados.Length; i++)
                {
                    if (i >= valores.Length)
                        continue;

                    var referencia =
                        encabezados[i].Trim();

                    if (string.IsNullOrWhiteSpace(referencia))
                        continue;

                    if (double.TryParse(
                            valores[i],
                            NumberStyles.Any,
                            CultureInfo.InvariantCulture,
                            out double pronostico))
                    {
                        pronosticos[referencia] = pronostico;
                    }
                }
            }

            // ============================================
            // INTERVALOS
            // ============================================

            var lineasIntervalos =
                System.IO.File.ReadAllLines(rutaIntervalos);

            var intervalos =
                new Dictionary<string, (double inferior,
                                        double superior,
                                        double halfWidth,
                                        double alphaAci)>(
                    StringComparer.OrdinalIgnoreCase);

            for (int i = 1; i < lineasIntervalos.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lineasIntervalos[i]))
                    continue;

                var valores =
                    lineasIntervalos[i].Split(',');

                if (valores.Length < 8)
                    continue;

                var referencia =
                    valores[1].Trim();

                if (string.IsNullOrWhiteSpace(referencia))
                    continue;

                if (!double.TryParse(
                        valores[4],
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out double inferior))
                    continue;

                if (!double.TryParse(
                        valores[5],
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out double superior))
                    continue;

                if (!double.TryParse(
                        valores[6],
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out double halfWidth))
                    continue;

                if (!double.TryParse(
                        valores[7],
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out double alphaAci))
                    continue;

                intervalos[referencia] =
                    (
                        inferior,
                        superior,
                        halfWidth,
                        alphaAci
                    );
            }

            // ============================================
            // MÉTODOS
            // ============================================

            var lineasMetodos =
                System.IO.File.ReadAllLines(rutaMetodos);

            var metodos =
                new Dictionary<string, (string metodo,
                                        bool tieneHiperparametros,
                                        int nScoresAci,
                                        double alphaAci)>(
                    StringComparer.OrdinalIgnoreCase);

            for (int i = 1; i < lineasMetodos.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lineasMetodos[i]))
                    continue;

                var valores =
                    lineasMetodos[i].Split(',');

                if (valores.Length < 5)
                    continue;

                var referencia =
                    valores[0].Trim();

                var metodo =
                    valores[1].Trim();

                if (!bool.TryParse(
                        valores[2].Trim(),
                        out bool tieneHiperparametros))
                    continue;

                if (!int.TryParse(
                        valores[3].Trim(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int nScoresAci))
                    continue;

                if (!double.TryParse(
                        valores[4].Trim(),
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out double alphaAci))
                    continue;

                if (string.IsNullOrWhiteSpace(referencia))
                    continue;

                metodos[referencia] =
                    (
                        metodo,
                        tieneHiperparametros,
                        nScoresAci,
                        alphaAci
                    );
            }

            // ============================================
            // CRUZAR INFORMACIÓN
            // ============================================

            var referencias =
                new HashSet<string>(
                    pronosticos.Keys,
                    StringComparer.OrdinalIgnoreCase);

            referencias.UnionWith(intervalos.Keys);
            referencias.UnionWith(metodos.Keys);

            var productos =
                new List<DashboardProducto>();

            foreach (var referencia in referencias.OrderBy(x => x))
            {
                pronosticos.TryGetValue(
                    referencia,
                    out double pronostico);

                metodos.TryGetValue(
                    referencia,
                    out var metodoInfo);

                intervalos.TryGetValue(
                    referencia,
                    out var intervaloInfo);

                var producto = new DashboardProducto
                {
                    Referencia = referencia,

                    Pronostico = pronostico,

                    Metodo =
                        metodoInfo.metodo,

                    TieneHiperparametros =
                        metodoInfo.tieneHiperparametros,

                    NScoresAci =
                        metodoInfo.nScoresAci,

                    AlphaAci =
                        intervaloInfo.alphaAci,

                    Inferior =
                        intervaloInfo.inferior,

                    Superior =
                        intervaloInfo.superior,

                    HalfWidth =
                        intervaloInfo.halfWidth
                };

                productos.Add(producto);
            }

            // ============================================
            // RESPUESTA
            // ============================================

            var respuesta = new DashboardRespuesta
            {
                Mes = mes,

                CantidadProductos =
                    productos.Count,

                Productos =
                    productos
            };

            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    mensaje =
                        "Error al construir los datos del dashboard.",

                    error = ex.Message
                }
            );
        }
    }


    [HttpGet("historico")]
    public IActionResult ObtenerHistorico(
        [FromQuery] string? referencia = null)
    {
        try
        {
            var carpetaDatos =
                _configuration["Rutas:CarpetaDatos"];

            if (string.IsNullOrWhiteSpace(carpetaDatos))
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        mensaje = "No está configurada la ruta de datos."
                    }
                );
            }

            var rutaSeries = Path.Combine(
                carpetaDatos,
                "datos_preprocesados",
                "series_mensuales.csv"
            );

            if (!System.IO.File.Exists(rutaSeries))
            {
                return NotFound(new
                {
                    mensaje =
                        "No se encontró series_mensuales.csv."
                });
            }

            var lineas =
                System.IO.File.ReadAllLines(rutaSeries);

            if (lineas.Length < 2)
            {
                return NotFound(new
                {
                    mensaje =
                        "El archivo histórico no contiene datos."
                });
            }

            var encabezados =
                lineas[0].Split(',');

            int indiceProducto = -1;

            if (!string.IsNullOrWhiteSpace(referencia))
            {
                for (int i = 1; i < encabezados.Length; i++)
                {
                    if (
                        encabezados[i].Trim()
                            .Equals(
                                referencia.Trim(),
                                StringComparison.OrdinalIgnoreCase
                            )
                    )
                    {
                        indiceProducto = i;
                        break;
                    }
                }

                if (indiceProducto == -1)
                {
                    return NotFound(new
                    {
                        mensaje =
                            $"No se encontró la referencia '{referencia}'."
                    });
                }
            }

            var historico =
                new List<HistoricoMensual>();

            foreach (var linea in lineas.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(linea))
                    continue;

                var valores = linea.Split(',');

                if (valores.Length == 0)
                    continue;

                var mes = valores[0].Trim();

                if (string.IsNullOrWhiteSpace(mes))
                    continue;

                double cantidad = 0;

                if (!string.IsNullOrWhiteSpace(referencia))
                {
                    if (indiceProducto >= valores.Length)
                        continue;

                    double.TryParse(
                        valores[indiceProducto],
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out cantidad
                    );
                }
                else
                {
                    for (int i = 1; i < valores.Length; i++)
                    {
                        if (
                            double.TryParse(
                                valores[i],
                                NumberStyles.Any,
                                CultureInfo.InvariantCulture,
                                out double valor
                            )
                        )
                        {
                            cantidad += valor;
                        }
                    }
                }

                historico.Add(
                    new HistoricoMensual
                    {
                        Mes = mes,
                        Cantidad = cantidad
                    }
                );
            }

            return Ok(new
            {
                referencia,
                cantidadMeses = historico.Count,
                historico
            });
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    mensaje =
                        "Error al obtener el histórico de ventas.",

                    error = ex.Message
                }
            );
        }
    }
}