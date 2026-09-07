using System.Security.Claims;
using System.Text.Json;
using Timbre.Api.Data;
using Timbre.Api.Models;

namespace Timbre.Api.Services;

public class AuditoriaService
{
    private readonly TimbreDbContext _context;
    private readonly FechaHoraService _fechaHoraService;

    public AuditoriaService(
        TimbreDbContext context,
        FechaHoraService fechaHoraService)
    {
        _context = context;
        _fechaHoraService = fechaHoraService;
    }

    public async Task RegistrarAsync(
        ClaimsPrincipal usuarioActual,
        string accion,
        string entidad,
        long? idEntidad,
        object? valorAnterior = null,
        object? valorNuevo = null,
        string? ipOrigen = null)
    {
        long? idUsuario = null;

        var claimIdUsuario =
            usuarioActual.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (long.TryParse(
            claimIdUsuario,
            out var idUsuarioConvertido))
        {
            idUsuario = idUsuarioConvertido;
        }

        var auditoria = new Auditoria
        {
            IdUsuario = idUsuario,

            Accion = accion,

            Entidad = entidad,

            IdEntidad = idEntidad,

            ValorAnterior =
                valorAnterior is null
                    ? null
                    : JsonSerializer.Serialize(
                        valorAnterior),

            ValorNuevo =
                valorNuevo is null
                    ? null
                    : JsonSerializer.Serialize(
                        valorNuevo),

            IpOrigen = ipOrigen,

            FechaAccion =
                _fechaHoraService.AhoraEcuador()
        };

        _context.Auditoria.Add(auditoria);

        await _context.SaveChangesAsync();
    }
}