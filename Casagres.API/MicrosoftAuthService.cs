using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace Casagres.API;

public class MicrosoftAuthService
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