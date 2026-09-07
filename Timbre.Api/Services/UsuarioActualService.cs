using System.Security.Claims;

namespace Timbre.Api.Services;

public class UsuarioActualService
{
    public long? ObtenerIdUsuario(
        ClaimsPrincipal user)
    {
        var valor =
            user.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (long.TryParse(valor, out var id))
        {
            return id;
        }

        return null;
    }

    public long? ObtenerIdEmpleado(
        ClaimsPrincipal user)
    {
        var valor =
            user.FindFirst(
                "idEmpleado")?.Value;

        if (long.TryParse(valor, out var id))
        {
            return id;
        }

        return null;
    }

    public string? ObtenerRol(
        ClaimsPrincipal user)
    {
        return user.FindFirst(
            ClaimTypes.Role)?.Value;
    }

    public bool EsAdministrador(
        ClaimsPrincipal user)
    {
        return user.IsInRole("Administrador");
    }

    public bool EsRRHH(
        ClaimsPrincipal user)
    {
        return user.IsInRole("RRHH");
    }

    public bool PuedeConsultarEmpleado(
        ClaimsPrincipal user,
        long idEmpleado)
    {
        if (user.IsInRole("Administrador") ||
            user.IsInRole("RRHH"))
        {
            return true;
        }

        var idEmpleadoActual =
            ObtenerIdEmpleado(user);

        return idEmpleadoActual.HasValue &&
               idEmpleadoActual.Value == idEmpleado;
    }
}