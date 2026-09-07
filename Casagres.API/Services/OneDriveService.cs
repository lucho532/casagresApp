using System.Net.Http;
using System.Text;
using System.Text.Json;
using Casagres.API.Models;

namespace Casagres.API.Services;

public class OneDriveService
{
    private readonly HttpClient _httpClient;

    public OneDriveService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<OneDriveFile>> ObtenerArchivosAsync()
    {
        var url =
            "https://onedrive.live.com/personal/f3ff5bb59011e061/" +
            "_api/web/GetListUsingPath(DecodedUrl=@a1)/RenderListDataAsStream" +
            "?@a1=%27%2Fpersonal%2Ff3ff5bb59011e061%2FDocuments%27" +
            "&RootFolder=%2Fpersonal%2Ff3ff5bb59011e061%2FDocuments%2FCOMERCIAL%20COMPARTIDO%2FUNAL%2FINFORMES%20DE%20VENTAS" +
            "&View=3861ce67-220c-4d20-8717-004d6a1cb205" +
            "&TryNewExperienceSingle=TRUE";

        var body = new
        {
            parameters = new
            {
                __metadata = new
                {
                    type = "SP.RenderListDataParameters"
                },
                RenderOptions = 1496871,
                AllowMultipleValueFilterForTaxonomyFields = true,
                AddRequiredFields = true,
                RequireFolderColoringFields = true
            }
        };

        var jsonBody = JsonSerializer.Serialize(body);

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            url
        );

        request.Content = new StringContent(
            jsonBody,
            Encoding.UTF8,
            "application/json"
        );

        request.Headers.Add(
            "Accept",
            "application/json;odata=verbose"
        );

        var response = await _httpClient.SendAsync(request);

        var contenido = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"OneDrive respondió {(int)response.StatusCode}: {contenido}"
            );
        }

        var archivos = new List<OneDriveFile>();

        using var documento = JsonDocument.Parse(contenido);

        var rows = documento
            .RootElement
            .GetProperty("ListData")
            .GetProperty("Row");

        foreach (var row in rows.EnumerateArray())
        {
            // Solo archivos
            if (!row.TryGetProperty("FSObjType", out var tipoObjeto))
                continue;

            // 0 = archivo
            if (tipoObjeto.GetString() != "0")
                continue;

            var nombre = row
                .GetProperty("FileLeafRef")
                .GetString();

            var extension = row
                .GetProperty("File_x0020_Type")
                .GetString();

            // Solo Excel
            if (extension != "xlsx" &&
                extension != "xls")
            {
                continue;
            }

            var ruta = row
                .GetProperty("FileRef")
                .GetString();

            var id = "";

            if (row.TryGetProperty(
                "name.FileSystemItemId",
                out var itemId))
            {
                id = itemId.GetString() ?? "";
            }

            long tamaño = 0;

            if (row.TryGetProperty(
                "File_x0020_Size",
                out var size))
            {
                long.TryParse(
                    size.GetString(),
                    out tamaño
                );
            }

            archivos.Add(new OneDriveFile
            {
                Nombre = nombre ?? "",
                Ruta = ruta ?? "",
                Id = id,
                Tipo = extension ?? "",
                Tamaño = tamaño
            });
        }

        return archivos;
    }

    public async Task<string> ProbarArchivoAsync()
    {
        var url = "https://d.docs.live.net/f3ff5bb59011e061/COMERCIAL%20COMPARTIDO/UNAL/INFORMES%20DE%20VENTAS/INFORME%20DE%20VENTAS%20CASAGRES%20CIERRE%202019.xlsx";

        var response = await _httpClient.GetAsync(url);

        var contentType = response.Content.Headers.ContentType?.ToString();
        var contentLength = response.Content.Headers.ContentLength;

        return $"""
            StatusCode: {(int)response.StatusCode}
            Estado: {response.StatusCode}
            Content-Type: {contentType}
            Content-Length: {contentLength}
            """;
    }
}