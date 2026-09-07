using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timbre.Api.Data;

namespace Timbre.Api.Controllers;

[Authorize(Roles = "Administrador")]
[ApiController]
[Route("api/[controller]")]
public class AuditoriaController : ControllerBase
{
    private readonly TimbreDbContext _context;

    public AuditoriaController(
        TimbreDbContext context)
    {
        _context = context;
    }

    // =====================================================
    // GET: api/auditoria
    //
    // Filtros opcionales:
    //
    // ?fechaDesde=2026-08-01
    // &fechaHasta=2026-08-31
    // &accion=ANULAR_MARCACION
    // &entidad=MarcacionAsistencia
    // &idUsuario=1
    // &idEntidad=15
    //
    // Solo Administrador.
    // =====================================================
    [HttpGet]
    public async Task<ActionResult> GetAuditoria(
        [FromQuery] DateOnly? fechaDesde,
        [FromQuery] DateOnly? fechaHasta,
        [FromQuery] string? accion,
        [FromQuery] string? entidad,
        [FromQuery] long? idUsuario,
        [FromQuery] long? idEntidad)
    {
        if (fechaDesde.HasValue &&
            fechaHasta.HasValue &&
            fechaDesde.Value > fechaHasta.Value)
        {
            return BadRequest(new
            {
                mensaje =
                    "La fecha desde no puede ser mayor que la fecha hasta."
            });
        }

        var query = _context.Auditoria
            .AsNoTracking()
            .AsQueryable();

        // =================================================
        // FECHA DESDE
        // =================================================
        if (fechaDesde.HasValue)
        {
            var desde =
                fechaDesde.Value.ToDateTime(
                    TimeOnly.MinValue);

            query = query.Where(a =>
                a.FechaAccion >= desde);
        }

        // =================================================
        // FECHA HASTA
        //
        // Usamos < día siguiente para incluir
        // toda la fecha hasta las 23:59:59...
        // =================================================
        if (fechaHasta.HasValue)
        {
            var hastaExclusiva =
                fechaHasta.Value
                    .AddDays(1)
                    .ToDateTime(
                        TimeOnly.MinValue);

            query = query.Where(a =>
                a.FechaAccion < hastaExclusiva);
        }

        // =================================================
        // ACCIÓN
        // =================================================
        if (!string.IsNullOrWhiteSpace(
            accion))
        {
            var accionFiltro =
                accion.Trim();

            query = query.Where(a =>
                a.Accion == accionFiltro);
        }

        // =================================================
        // ENTIDAD
        // =================================================
        if (!string.IsNullOrWhiteSpace(
            entidad))
        {
            var entidadFiltro =
                entidad.Trim();

            query = query.Where(a =>
                a.Entidad == entidadFiltro);
        }

        // =================================================
        // USUARIO
        // =================================================
        if (idUsuario.HasValue)
        {
            query = query.Where(a =>
                a.IdUsuario == idUsuario.Value);
        }

        // =================================================
        // ENTIDAD ESPECÍFICA
        // =================================================
        if (idEntidad.HasValue)
        {
            query = query.Where(a =>
                a.IdEntidad == idEntidad.Value);
        }

        var resultado = await query
            .OrderByDescending(a =>
                a.FechaAccion)
            .ThenByDescending(a =>
                a.IdAuditoria)
            .Select(a => new
            {
                a.IdAuditoria,

                a.IdUsuario,

                usuario =
                    a.IdUsuarioNavigation != null
                        ? a.IdUsuarioNavigation.NombreUsuario
                        : null,

                a.Accion,

                a.Entidad,

                a.IdEntidad,

                a.ValorAnterior,

                a.ValorNuevo,

                a.IpOrigen,

                a.FechaAccion
            })
            .Take(1000)
            .ToListAsync();

        return Ok(resultado);
    }

    // =====================================================
    // GET: api/auditoria/15
    //
    // Obtiene un registro específico.
    // =====================================================
    [HttpGet("{id:long}")]
    public async Task<ActionResult> GetAuditoriaPorId(
        long id)
    {
        var auditoria = await _context.Auditoria
            .AsNoTracking()
            .Where(a =>
                a.IdAuditoria == id)
            .Select(a => new
            {
                a.IdAuditoria,

                a.IdUsuario,

                usuario =
                    a.IdUsuarioNavigation != null
                        ? a.IdUsuarioNavigation.NombreUsuario
                        : null,

                a.Accion,

                a.Entidad,

                a.IdEntidad,

                a.ValorAnterior,

                a.ValorNuevo,

                a.IpOrigen,

                a.FechaAccion
            })
            .FirstOrDefaultAsync();

        if (auditoria is null)
        {
            return NotFound(new
            {
                mensaje =
                    "Registro de auditoría no encontrado."
            });
        }

        return Ok(auditoria);
    }

    // =====================================================
    // GET:
    // api/auditoria/entidad/MarcacionAsistencia/15
    //
    // Devuelve toda la trazabilidad de una entidad.
    // =====================================================
    [HttpGet("entidad/{entidad}/{idEntidad:long}")]
    public async Task<ActionResult> GetAuditoriaEntidad(
        string entidad,
        long idEntidad)
    {
        if (string.IsNullOrWhiteSpace(
            entidad))
        {
            return BadRequest(new
            {
                mensaje =
                    "Debe indicar la entidad."
            });
        }

        var entidadFiltro =
            entidad.Trim();

        var resultado = await _context.Auditoria
            .AsNoTracking()
            .Where(a =>
                a.Entidad == entidadFiltro &&
                a.IdEntidad == idEntidad)
            .OrderBy(a =>
                a.FechaAccion)
            .ThenBy(a =>
                a.IdAuditoria)
            .Select(a => new
            {
                a.IdAuditoria,

                a.IdUsuario,

                usuario =
                    a.IdUsuarioNavigation != null
                        ? a.IdUsuarioNavigation.NombreUsuario
                        : null,

                a.Accion,

                a.Entidad,

                a.IdEntidad,

                a.ValorAnterior,

                a.ValorNuevo,

                a.IpOrigen,

                a.FechaAccion
            })
            .ToListAsync();

        return Ok(resultado);
    }
}