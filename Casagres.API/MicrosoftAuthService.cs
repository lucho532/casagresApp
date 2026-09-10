using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace Casagres.API;


/*
=========================================================
 MICROSOFT AUTH SERVICE
=========================================================

Esta clase se encarga de gestionar la autenticación con
Microsoft para permitir que la aplicación CASAGRES obtenga
tokens de acceso a servicios de Microsoft.

FUNCIONAMIENTO:

1. Configura una aplicación registrada previamente en
   Microsoft Azure utilizando su ClientId.

2. Define los permisos (Scopes) que necesita la aplicación:
   - User.Read:
     Permite obtener información básica del usuario autenticado.
   - Files.ReadWrite:
     Permite leer y modificar archivos a los que el usuario
     haya autorizado el acceso.

3. Crea una aplicación pública de Microsoft utilizando MSAL
   (Microsoft Authentication Library).

4. Guarda los tokens de autenticación en una caché local
   dentro del ordenador para evitar solicitar credenciales
   constantemente.

5. Cuando se solicita un token mediante ObtenerTokenAsync():

   - Primero intenta recuperar una sesión existente y obtener
     el token de forma silenciosa.

   - Si la sesión almacenada sigue siendo válida, obtiene un
     nuevo token sin que el usuario tenga que autenticarse
     nuevamente.

   - Si no existe una sesión válida, inicia el proceso de
     autenticación mediante Device Code Flow.

6. Device Code Flow muestra al usuario un código y una
   dirección web donde debe iniciar sesión con su cuenta
   de Microsoft.

7. Una vez autenticado el usuario, Microsoft devuelve un
   Access Token que puede utilizarse para acceder a servicios
   autorizados, como Microsoft Graph o OneDrive.

IMPORTANTE:

Esta clase NO gestiona el login de usuarios de la plataforma
CASAGRES.

Actualmente su objetivo es autenticar una cuenta de Microsoft
para permitir el acceso a servicios externos de Microsoft,
principalmente Microsoft Graph y OneDrive.

El Access Token obtenido aquí pertenece a Microsoft y NO debe
confundirse con el JWT utilizado para autenticar usuarios dentro
de la plataforma CASAGRES.

=========================================================
*/

public class MicrosoftAuthService : IMicrosoftAuthService
{
    private const string ClientId =
        "c21b50d1-67bf-4da2-a167-25e5d3625973";


    //Aquí defines los permisos que solicitas
    private static readonly string[] Scopes =
    {
        "User.Read",
        "Files.ReadWrite"
    };

    private readonly IPublicClientApplication _app;

    public MicrosoftAuthService()
    {
        var cacheDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "Casagres",
                "AuthCache");

        Directory.CreateDirectory(cacheDirectory);

        var storageProperties =
            new StorageCreationPropertiesBuilder(
                "msal.cache",
                cacheDirectory)
            .Build();

        var cacheHelper =
            MsalCacheHelper.CreateAsync(storageProperties)
                .GetAwaiter()
                .GetResult();

        //Aquí creas la aplicación de autenticación de Microsoft
        _app = PublicClientApplicationBuilder
            .Create(ClientId)
            .WithAuthority(
                AzureCloudInstance.AzurePublic,
                "consumers")
            .Build();

        cacheHelper.RegisterCache(_app.UserTokenCache);
    }

    public async Task<string> ObtenerTokenAsync()
    {
        var cuentas = await _app.GetAccountsAsync();

        var cuenta = cuentas.FirstOrDefault();

        if (cuenta != null)
        {
            try
            {
                var resultadoSilencioso =
                    await _app
                        .AcquireTokenSilent(Scopes, cuenta)
                        .ExecuteAsync();

                Console.WriteLine();
                Console.WriteLine("==========================================");
                Console.WriteLine("AUTENTICACIÓN SILENCIOSA CORRECTA");
                Console.WriteLine("==========================================");
                Console.WriteLine($"Usuario: {resultadoSilencioso.Account?.Username}");
                Console.WriteLine("No fue necesario iniciar sesión.");
                Console.WriteLine("==========================================");

                return resultadoSilencioso.AccessToken;
            }
            catch (MsalUiRequiredException)
            {
                Console.WriteLine(
                    "La sesión almacenada requiere volver a autenticarse.");
            }
        }

        Console.WriteLine();
        Console.WriteLine("==========================================");
        Console.WriteLine("INICIO DE SESIÓN DE MICROSOFT");
        Console.WriteLine("==========================================");

        var resultado =
            await _app
                .AcquireTokenWithDeviceCode(
                    Scopes,
                    deviceCodeResult =>
                    {
                        Console.WriteLine(deviceCodeResult.Message);
                        return Task.CompletedTask;
                    })
                .ExecuteAsync();

        Console.WriteLine();
        Console.WriteLine("==========================================");
        Console.WriteLine("AUTENTICACIÓN CORRECTA");
        Console.WriteLine("==========================================");
        Console.WriteLine($"Usuario: {resultado.Account?.Username}");
        Console.WriteLine("==========================================");

        return resultado.AccessToken;
    }
}