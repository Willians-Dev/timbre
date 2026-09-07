using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Timbre.Api.DTOs.Pruebas;
using Timbre.Api.Services;

namespace Timbre.Api.Controllers;

[Authorize(Roles = "Administrador")]
[ApiController]
[Route("api/[controller]")]
public class PruebasController : ControllerBase
{
    private readonly FechaHoraService _fechaHoraService;
    private readonly IWebHostEnvironment _environment;

    public PruebasController(
        FechaHoraService fechaHoraService,
        IWebHostEnvironment environment)
    {
        _fechaHoraService = fechaHoraService;
        _environment = environment;
    }

    [HttpGet("hora")]
    public ActionResult ObtenerHora()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        return Ok(new
        {
            ambiente = _environment.EnvironmentName,
            horaActual = _fechaHoraService.AhoraEcuador(),
            horaSimulada =
                _fechaHoraService.ObtenerFechaHoraSimulada()
        });
    }

    [HttpPost("hora")]
    public ActionResult EstablecerHora(
        [FromBody] FechaHoraSimuladaDto dto)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        _fechaHoraService
            .EstablecerFechaHoraSimulada(dto.FechaHora);

        return Ok(new
        {
            mensaje = "Hora simulada establecida correctamente.",
            fechaHora = _fechaHoraService.AhoraEcuador()
        });
    }

    [HttpDelete("hora")]
    public ActionResult LimpiarHora()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        _fechaHoraService.LimpiarFechaHoraSimulada();

        return Ok(new
        {
            mensaje = "Hora simulada eliminada.",
            fechaHoraReal = _fechaHoraService.AhoraEcuador()
        });
    }
}