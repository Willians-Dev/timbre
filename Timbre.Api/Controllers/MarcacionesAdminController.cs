using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timbre.Api.Data;
using Timbre.Api.DTOs.Marcaciones;
using Timbre.Api.Models;
using Timbre.Api.Services;

namespace Timbre.Api.Controllers;

[Authorize(Roles = "Administrador,RRHH")]
[ApiController]
[Route("api/marcaciones-admin")]
public class MarcacionesAdminController : ControllerBase
{
    private readonly TimbreDbContext _context;
    private readonly FechaHoraService _fechaHoraService;
    private readonly AuditoriaService _auditoriaService;

    public MarcacionesAdminController(
        TimbreDbContext context,
        FechaHoraService fechaHoraService,
        AuditoriaService auditoriaService)
    {
        _context = context;
        _fechaHoraService = fechaHoraService;
        _auditoriaService = auditoriaService;
    }

    // =====================================================
    // POST:
    // api/marcaciones-admin/correccion
    //
    // Crea una marcación manual.
    // Solo Administrador o RRHH.
    // =====================================================
    [HttpPost("correccion")]
    public async Task<ActionResult> CrearMarcacionManual(
        [FromBody] CorreccionManualMarcacionDto dto)
    {
        var tiposPermitidos = new[]
        {
            "Entrada",
            "InicioAlmuerzo",
            "FinAlmuerzo",
            "Salida"
        };

        if (!tiposPermitidos.Contains(dto.TipoMarcacion))
        {
            return BadRequest(new
            {
                mensaje = "El tipo de marcación no es válido."
            });
        }

        if (string.IsNullOrWhiteSpace(dto.Motivo))
        {
            return BadRequest(new
            {
                mensaje = "El motivo de la corrección es obligatorio."
            });
        }

        var empleado = await _context.Empleado
            .AsNoTracking()
            .FirstOrDefaultAsync(e =>
                e.IdEmpleado == dto.IdEmpleado);

        if (empleado is null)
        {
            return NotFound(new
            {
                mensaje = "Empleado no encontrado."
            });
        }

        var yaExiste = await _context.MarcacionAsistencia
            .AnyAsync(m =>
                m.IdEmpleado == dto.IdEmpleado &&
                m.FechaMarcacion == dto.Fecha &&
                m.TipoMarcacion == dto.TipoMarcacion &&
                m.EstadoMarcacion == "Activa");

        if (yaExiste)
        {
            return Conflict(new
            {
                mensaje =
                    $"Ya existe una marcación activa de tipo {dto.TipoMarcacion} para esa fecha."
            });
        }

        var fechaHora =
            dto.Fecha.ToDateTime(dto.Hora);

        var marcacion = new MarcacionAsistencia
        {
            IdEmpleado = dto.IdEmpleado,

            FechaMarcacion = dto.Fecha,

            FechaHora = fechaHora,

            TipoMarcacion = dto.TipoMarcacion,

            EstadoMarcacion = "Activa",

            Observacion =
                $"Corrección manual: {dto.Motivo.Trim()}",

            FechaCreacion =
                _fechaHoraService.AhoraEcuador()
        };

        _context.MarcacionAsistencia.Add(marcacion);

        await _context.SaveChangesAsync();

        await _auditoriaService.RegistrarAsync(
            User,
            accion: "CREAR_MARCACION_MANUAL",
            entidad: "MarcacionAsistencia",
            idEntidad: marcacion.IdMarcacion,
            valorAnterior: null,
            valorNuevo: new
            {
                marcacion.IdMarcacion,
                marcacion.IdEmpleado,
                marcacion.FechaMarcacion,
                marcacion.FechaHora,
                marcacion.TipoMarcacion,
                marcacion.EstadoMarcacion,
                marcacion.Observacion
            },
            ipOrigen:
                HttpContext.Connection
                    .RemoteIpAddress?
                    .ToString()
        );

        return Ok(new
        {
            idMarcacion = marcacion.IdMarcacion,

            mensaje =
                "Marcación manual registrada correctamente."
        });
    }

    // =====================================================
    // PATCH:
    // api/marcaciones-admin/5/anular
    //
    // Anula una marcación existente.
    // No elimina físicamente la fila.
    // =====================================================
    [HttpPatch("{id:long}/anular")]
    public async Task<ActionResult> AnularMarcacion(
        long id,
        [FromBody] AnularMarcacionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Motivo))
        {
            return BadRequest(new
            {
                mensaje = "El motivo de la anulación es obligatorio."
            });
        }

        var marcacion = await _context.MarcacionAsistencia
            .FirstOrDefaultAsync(m =>
                m.IdMarcacion == id);

        if (marcacion is null)
        {
            return NotFound(new
            {
                mensaje = "Marcación no encontrada."
            });
        }

        if (marcacion.EstadoMarcacion == "Anulada")
        {
            return Conflict(new
            {
                mensaje =
                    "La marcación ya se encuentra anulada."
            });
        }

        var valorAnterior = new
        {
            marcacion.IdMarcacion,
            marcacion.IdEmpleado,
            marcacion.FechaMarcacion,
            marcacion.FechaHora,
            marcacion.TipoMarcacion,
            marcacion.EstadoMarcacion,
            marcacion.Observacion
        };

        marcacion.EstadoMarcacion = "Anulada";

        marcacion.Observacion =
            dto.Motivo.Trim();

        marcacion.FechaModificacion =
            _fechaHoraService.AhoraEcuador();

        await _context.SaveChangesAsync();

        await _auditoriaService.RegistrarAsync(
            User,
            accion: "ANULAR_MARCACION",
            entidad: "MarcacionAsistencia",
            idEntidad: marcacion.IdMarcacion,
            valorAnterior: valorAnterior,
            valorNuevo: new
            {
                marcacion.IdMarcacion,
                marcacion.IdEmpleado,
                marcacion.FechaMarcacion,
                marcacion.FechaHora,
                marcacion.TipoMarcacion,
                marcacion.EstadoMarcacion,
                marcacion.Observacion
            },
            ipOrigen:
                HttpContext.Connection
                    .RemoteIpAddress?
                    .ToString()
        );

        return Ok(new
        {
            mensaje =
                "Marcación anulada correctamente."
        });
    }

    // =====================================================
    // GET:
    // api/marcaciones-admin/empleado/1
    // ?fecha=2026-08-24
    //
    // Incluye marcaciones activas y anuladas.
    // Útil para revisión administrativa.
    // =====================================================
    [HttpGet("empleado/{idEmpleado:long}")]
    public async Task<ActionResult> GetMarcacionesEmpleado(
        long idEmpleado,
        [FromQuery] DateOnly fecha)
    {
        var empleado = await _context.Empleado
            .AsNoTracking()
            .FirstOrDefaultAsync(e =>
                e.IdEmpleado == idEmpleado);

        if (empleado is null)
        {
            return NotFound(new
            {
                mensaje = "Empleado no encontrado."
            });
        }

        var marcaciones = await _context.MarcacionAsistencia
            .AsNoTracking()
            .Where(m =>
                m.IdEmpleado == idEmpleado &&
                m.FechaMarcacion == fecha)
            .OrderBy(m => m.FechaHora)
            .Select(m => new
            {
                m.IdMarcacion,
                m.IdEmpleado,
                m.FechaMarcacion,
                m.FechaHora,
                m.TipoMarcacion,
                m.EstadoMarcacion,
                m.Observacion,
                m.FechaCreacion,
                m.FechaModificacion
            })
            .ToListAsync();

        return Ok(marcaciones);
    }

    // =====================================================
// GET:
// api/marcaciones-admin/consulta
// ?fecha=2026-08-30
//
// Consulta administrativa de todas las marcaciones
// de una fecha.
// Incluye:
// - activas
// - anuladas
// - facial / manual
//
// Solo:
// Administrador / RRHH
// =====================================================
[HttpGet("consulta")]
public async Task<ActionResult> ConsultarMarcaciones(
    [FromQuery] DateOnly? fecha)
{
    var fechaConsulta =
        fecha ??
        DateOnly.FromDateTime(
            _fechaHoraService.AhoraEcuador()
        );


    var marcaciones =
        await _context.MarcacionAsistencia
            .AsNoTracking()
            .Include(m =>
                m.IdEmpleadoNavigation)
            .Include(m =>
                m.ValidacionFacial)
            .Where(m =>
                m.FechaMarcacion ==
                fechaConsulta)
            .OrderBy(m =>
                m.IdEmpleadoNavigation.Apellidos)
            .ThenBy(m =>
                m.IdEmpleadoNavigation.Nombres)
            .ThenBy(m =>
                m.FechaHora)
            .Select(m => new
            {
                idMarcacion =
                    m.IdMarcacion,

                idEmpleado =
                    m.IdEmpleado,

                identificacion =
                    m.IdEmpleadoNavigation
                        .Identificacion,

                empleado =
                    m.IdEmpleadoNavigation
                        .Nombres +
                    " " +
                    m.IdEmpleadoNavigation
                        .Apellidos,

                area =
                    m.IdEmpleadoNavigation
                        .Area,

                cargo =
                    m.IdEmpleadoNavigation
                        .Cargo,

                fechaMarcacion =
                    m.FechaMarcacion,

                fechaHora =
                    m.FechaHora,

                tipoMarcacion =
                    m.TipoMarcacion,

                estadoMarcacion =
                    m.EstadoMarcacion,

                observacion =
                    m.Observacion,

                esFacial =
                    m.ValidacionFacial != null,

                similitud =
                    m.ValidacionFacial != null
                        ? m.ValidacionFacial
                            .Similitud
                        : null,

                umbral =
                    m.ValidacionFacial != null
                        ? m.ValidacionFacial
                            .Umbral
                        : null,

                aprobadoFacial =
                    m.ValidacionFacial != null
                        ? m.ValidacionFacial
                            .Aprobado
                        : (bool?)null
            })
            .ToListAsync();


        return Ok(new
        {
            fecha =
                fechaConsulta,

            total =
                marcaciones.Count,

            marcaciones
        });
    }
}