using System.Net;

namespace Casagres.API.Tests.TestHelpers;

/// <summary>
/// HttpMessageHandler falso que devuelve, en orden, las respuestas que se
/// le vayan encolando. Permite probar servicios que usan HttpClient sin
/// hacer llamadas HTTP reales.
/// </summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<(HttpStatusCode StatusCode, string Contenido)> _respuestas = new();

    public List<string> UrlsSolicitadas { get; } = new();

    public List<string> CuerposSolicitados { get; } = new();

    public List<HttpRequestMessage> SolicitudesRecibidas { get; } = new();

    public FakeHttpMessageHandler EncolarRespuesta(HttpStatusCode statusCode, string contenidoJson)
    {
        _respuestas.Enqueue((statusCode, contenidoJson));
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        UrlsSolicitadas.Add(request.RequestUri!.ToString());
        SolicitudesRecibidas.Add(request);

        CuerposSolicitados.Add(
            request.Content == null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken));

        if (_respuestas.Count == 0)
        {
            throw new InvalidOperationException(
                "No hay más respuestas encoladas para el fake HTTP handler.");
        }

        var (statusCode, contenido) = _respuestas.Dequeue();

        var respuesta = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(contenido)
        };

        return respuesta;
    }
}
