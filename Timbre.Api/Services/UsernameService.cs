using System.Text.RegularExpressions;


namespace Timbre.Api.Services;


public class UsernameService
{
    // =====================================================
    // PATRÓN DE USUARIO
    //
    // Permitidos:
    //
    // - letras minúsculas a-z
    // - números 0-9
    // - punto .
    // - guion -
    // - guion bajo _
    //
    // Ejemplos:
    //
    // carlos.mendoza
    // rrhh01
    // admin
    // usuario_prueba
    // =====================================================

    private static readonly Regex PatronUsuario =
        new(
            "^[a-z0-9._-]+$",
            RegexOptions.Compiled |
            RegexOptions.CultureInvariant
        );


    // =====================================================
    // NORMALIZAR
    // =====================================================

    public string Normalizar(
        string usuario
    )
    {
        if (
            string.IsNullOrWhiteSpace(
                usuario
            )
        )
        {
            throw new ArgumentException(
                "El nombre de usuario es obligatorio."
            );
        }


        var normalizado =
            usuario
                .Trim()
                .ToLowerInvariant();


        // =================================================
        // LONGITUD MÍNIMA
        // =================================================

        if (
            normalizado.Length <
            3
        )
        {
            throw new ArgumentException(
                "El nombre de usuario debe contener al menos 3 caracteres."
            );
        }


        // =================================================
        // LONGITUD MÁXIMA
        // =================================================

        if (
            normalizado.Length >
            100
        )
        {
            throw new ArgumentException(
                "El nombre de usuario no puede superar los 100 caracteres."
            );
        }


        // =================================================
        // CARACTERES PERMITIDOS
        // =================================================

        if (
            !PatronUsuario.IsMatch(
                normalizado
            )
        )
        {
            throw new ArgumentException(
                "El nombre de usuario solo puede contener " +
                "letras minúsculas, números, punto, guion " +
                "y guion bajo."
            );
        }


        return normalizado;
    }


    // =====================================================
    // VALIDAR
    //
    // Útil cuando solamente necesitamos conocer si un
    // usuario cumple las reglas, sin lanzar excepción.
    // =====================================================

    public bool EsValido(
        string? usuario
    )
    {
        if (
            string.IsNullOrWhiteSpace(
                usuario
            )
        )
        {
            return false;
        }


        var normalizado =
            usuario
                .Trim()
                .ToLowerInvariant();


        if (
            normalizado.Length <
            3 ||
            normalizado.Length >
            100
        )
        {
            return false;
        }


        return PatronUsuario
            .IsMatch(
                normalizado
            );
    }
}