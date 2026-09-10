namespace Casagres.API.Services;

public interface IExcelExportService
{
    Task<List<string>> GenerarCsvVentas();
}
