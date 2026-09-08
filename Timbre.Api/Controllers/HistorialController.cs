using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Timbre.Api.DTOs.Historial;
using Timbre.Api.Services;

namespace Timbre.Api.Controllers;


[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador,RRHH")]
public class HistorialController : ControllerBase
{
    private readonly HistorialAsistenciaService
        _historialService;


    public HistorialController(
        HistorialAsistenciaService historialService)
    {
        _historialService =
            historialService;
    }


    // =====================================================
    // GET:
    // api/historial
    // =====================================================

    [HttpGet]
    public async Task<ActionResult> Consultar(
        [FromQuery] DateOnly? fechaDesde,
        [FromQuery] DateOnly? fechaHasta,
        [FromQuery] long? idEmpleado,
        [FromQuery] string? area,
        [FromQuery] string? estadoJornada,
        [FromQuery] string? estadoEntrada,
        [FromQuery] string? origen,
        CancellationToken cancellationToken)
    {
        var hoy =
            DateOnly.FromDateTime(
                DateTime.Now
            );


        var desde =
            fechaDesde ??
            new DateOnly(
                hoy.Year,
                hoy.Month,
                1
            );


        var hasta =
            fechaHasta ??
            hoy;


        try
        {
            var resultado =
                await _historialService
                    .ConsultarAsync(
                        new HistorialAsistenciaFiltroDto
                        {
                            FechaDesde =
                                desde,

                            FechaHasta =
                                hasta,

                            IdEmpleado =
                                idEmpleado,

                            Area =
                                area,

                            EstadoJornada =
                                estadoJornada,

                            EstadoEntrada =
                                estadoEntrada,

                            Origen =
                                origen
                        },

                        cancellationToken
                    );


            return Ok(
                resultado
            );
        }
        catch (
            ArgumentException ex
        )
        {
            return BadRequest(
                new
                {
                    mensaje =
                        ex.Message
                }
            );
        }
    }


    // =====================================================
    // GET:
    // api/historial/filtros
    // =====================================================

    [HttpGet("filtros")]
    public async Task<ActionResult>
        ObtenerFiltros(
            CancellationToken cancellationToken)
    {
        var resultado =
            await _historialService
                .ObtenerFiltrosAsync(
                    cancellationToken
                );


        return Ok(
            resultado
        );
    }
}