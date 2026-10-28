using System.Globalization;

namespace Casagres.API.Services.Pronostico;

internal static class CsvCampoParser
{
    public static double? ADouble(string valor) =>
        double.TryParse(
            valor,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var resultado)
            ? resultado
            : null;

    public static int? AEntero(string valor) =>
        int.TryParse(
            valor,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var resultado)
            ? resultado
            : null;

    public static bool? ABooleano(string valor) =>
        bool.TryParse(valor.Trim(), out var resultado)
            ? resultado
            : null;
}
