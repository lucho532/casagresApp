namespace Casagres.API.Models;

public static class Roles
{
    public const string Admin = "admin";
    public const string Usuario = "usuario";

    // Cuenta recién registrada, a la espera de que un administrador la
    // apruebe. No debe tener acceso a ningún dato de la empresa.
    public const string Pendiente = "pendiente";

    // Para usar en [Authorize(Roles = Roles.ConAccesoADatos)]: roles que
    // sí pueden ver datos de la empresa (deja fuera a "pendiente").
    public const string ConAccesoADatos = Admin + "," + Usuario;
}
