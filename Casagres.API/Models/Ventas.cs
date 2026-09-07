namespace Casagres.API.Models;

public class Venta
{
    public long Id { get; set; }

    public string? referencia_producto { get; set; }
    public string? descripcion_producto { get; set; }

    public string? zona_vendedor { get; set; }
    public string? nombre_zona_vendedor { get; set; }

    public string? tercero { get; set; }
    public string? nombre_tercero { get; set; }

    public string? calificacion_tercero { get; set; }
    public string? tipo_tercero { get; set; }

    public string? lista_de_precio { get; set; }

    public string? cod_ciudad { get; set; }
    public string? sucursal_tercero { get; set; }

    public int? periodo { get; set; }

    public decimal? valor_neto { get; set; }
    public decimal? cantidad { get; set; }
    public decimal? cantidad_devolucion { get; set; }
    public decimal? valor_venta { get; set; }

    public decimal? cantidad_notas { get; set; }
    public decimal? valor_notas { get; set; }

    public string? tipo { get; set; }

    public string? codigo_zona { get; set; }
    public string? canal { get; set; }

    public string? lista_de_precios { get; set; }

    public string? departamento { get; set; }
    public string? marca { get; set; }

    public string? linea_inventarios { get; set; }
    public string? descripcion_linea_inventarios { get; set; }

    public string? grupo_inventarios { get; set; }
    public string? descripcion_grupo_inventarios { get; set; }

    public string? cod_producto { get; set; }
    public string? descripcion { get; set; }

    public string? clase_producto { get; set; }

    public decimal? peso { get; set; }
    public decimal? toneladas { get; set; }
    public decimal? cantidad_neta { get; set; }

    public decimal? valor_venta_neta { get; set; }
    public decimal? valor_presupuesto { get; set; }

    public decimal? dif { get; set; }
    public decimal? precio { get; set; }
    public decimal? valor_unitario { get; set; }
    public decimal? diferencia { get; set; }

    public string? precios_zonas { get; set; }
    public string? precios_unicos { get; set; }

    public int? periodo_precios { get; set; }

    public decimal? meta_diaria_de_venta { get; set; }

    public decimal? cumpli_proyect { get; set; }

    public decimal? actual { get; set; }

    public string? zona { get; set; }
    public string? planta { get; set; }

    public DateTime FechaCarga { get; set; }
}