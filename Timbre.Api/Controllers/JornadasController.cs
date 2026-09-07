using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timbre.Api.Data;
using Timbre.Api.DTOs.Jornadas;
using Timbre.Api.Models;

namespace Timbre.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JornadasController : ControllerBase
{
    private readonly TimbreDbContext _context;

    public JornadasController(TimbreDbContext context)
    {
        _context = context;
    }

    // GET: api/jornadas
    [HttpGet]
    public async Task<ActionResult<IEnumerable<JornadaLaboralDto>>> GetJornadas()
    {
        var jornadas = await _context.JornadaLaboral
            .AsNoTracking()
            .OrderBy(j => j.Nombre)
            .Select(j => new JornadaLaboralDto
            {
                IdJornada = j.IdJornada,
                Nombre = j.Nombre,
                HoraEntrada = j.HoraEntrada,
                HoraInicioAlmuerzo = j.HoraInicioAlmuerzo,
                HoraFinAlmuerzo = j.HoraFinAlmuerzo,
                HoraSalida = j.HoraSalida,
                ToleranciaEntradaMinutos = j.ToleranciaEntradaMinutos,
                Lunes = j.Lunes,
                Martes = j.Martes,
                Miercoles = j.Miercoles,
                Jueves = j.Jueves,
                Viernes = j.Viernes,
                Sabado = j.Sabado,
                Domingo = j.Domingo,
                Activo = j.Activo
            })
            .ToListAsync();

        return Ok(jornadas);
    }

    // GET: api/jornadas/1
    [HttpGet("{id:long}")]
    public async Task<ActionResult<JornadaLaboralDto>> GetJornada(long id)
    {
        var jornada = await _context.JornadaLaboral
            .AsNoTracking()
            .Where(j => j.IdJornada == id)
            .Select(j => new JornadaLaboralDto
            {
                IdJornada = j.IdJornada,
                Nombre = j.Nombre,
                HoraEntrada = j.HoraEntrada,
                HoraInicioAlmuerzo = j.HoraInicioAlmuerzo,
                HoraFinAlmuerzo = j.HoraFinAlmuerzo,
                HoraSalida = j.HoraSalida,
                ToleranciaEntradaMinutos = j.ToleranciaEntradaMinutos,
                Lunes = j.Lunes,
                Martes = j.Martes,
                Miercoles = j.Miercoles,
                Jueves = j.Jueves,
                Viernes = j.Viernes,
                Sabado = j.Sabado,
                Domingo = j.Domingo,
                Activo = j.Activo
            })
            .FirstOrDefaultAsync();

        if (jornada is null)
        {
            return NotFound(new
            {
                mensaje = "Jornada no encontrada."
            });
        }

        return Ok(jornada);
    }

    // POST: api/jornadas
    [HttpPost]
    public async Task<ActionResult> CrearJornada(
        [FromBody] CrearJornadaLaboralDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            return BadRequest(new
            {
                mensaje = "El nombre de la jornada es obligatorio."
            });
        }

        if (dto.ToleranciaEntradaMinutos < 0)
        {
            return BadRequest(new
            {
                mensaje = "La tolerancia no puede ser negativa."
            });
        }

        if (dto.HoraEntrada >= dto.HoraSalida)
        {
            return BadRequest(new
            {
                mensaje = "La hora de entrada debe ser menor que la hora de salida."
            });
        }

        if (dto.HoraInicioAlmuerzo >= dto.HoraFinAlmuerzo)
        {
            return BadRequest(new
            {
                mensaje = "La hora de inicio de almuerzo debe ser menor que la hora de fin de almuerzo."
            });
        }

        if (dto.HoraInicioAlmuerzo <= dto.HoraEntrada ||
            dto.HoraFinAlmuerzo >= dto.HoraSalida)
        {
            return BadRequest(new
            {
                mensaje = "El horario de almuerzo debe estar dentro de la jornada laboral."
            });
        }

        var nombre = dto.Nombre.Trim();

        var existe = await _context.JornadaLaboral
            .AnyAsync(j => j.Nombre.ToLower() == nombre.ToLower());

        if (existe)
        {
            return Conflict(new
            {
                mensaje = "Ya existe una jornada con ese nombre."
            });
        }

        var jornada = new JornadaLaboral
        {
            Nombre = nombre,
            HoraEntrada = dto.HoraEntrada,
            HoraInicioAlmuerzo = dto.HoraInicioAlmuerzo,
            HoraFinAlmuerzo = dto.HoraFinAlmuerzo,
            HoraSalida = dto.HoraSalida,
            ToleranciaEntradaMinutos = dto.ToleranciaEntradaMinutos,
            Lunes = dto.Lunes,
            Martes = dto.Martes,
            Miercoles = dto.Miercoles,
            Jueves = dto.Jueves,
            Viernes = dto.Viernes,
            Sabado = dto.Sabado,
            Domingo = dto.Domingo,
            Activo = true
        };

        _context.JornadaLaboral.Add(jornada);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetJornada),
            new { id = jornada.IdJornada },
            new
            {
                idJornada = jornada.IdJornada,
                mensaje = "Jornada creada correctamente."
            });
    }

    // PUT: api/jornadas/1
    [HttpPut("{id:long}")]
    public async Task<ActionResult> ActualizarJornada(
        long id,
        [FromBody] ActualizarJornadaLaboralDto dto)
    {
        var jornada = await _context.JornadaLaboral
            .FirstOrDefaultAsync(j => j.IdJornada == id);

        if (jornada is null)
        {
            return NotFound(new
            {
                mensaje = "Jornada no encontrada."
            });
        }

        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            return BadRequest(new
            {
                mensaje = "El nombre de la jornada es obligatorio."
            });
        }

        if (dto.ToleranciaEntradaMinutos < 0)
        {
            return BadRequest(new
            {
                mensaje = "La tolerancia no puede ser negativa."
            });
        }

        if (dto.HoraEntrada >= dto.HoraSalida)
        {
            return BadRequest(new
            {
                mensaje = "La hora de entrada debe ser menor que la hora de salida."
            });
        }

        if (dto.HoraInicioAlmuerzo >= dto.HoraFinAlmuerzo)
        {
            return BadRequest(new
            {
                mensaje = "La hora de inicio de almuerzo debe ser menor que la hora de fin de almuerzo."
            });
        }

        if (dto.HoraInicioAlmuerzo <= dto.HoraEntrada ||
            dto.HoraFinAlmuerzo >= dto.HoraSalida)
        {
            return BadRequest(new
            {
                mensaje = "El horario de almuerzo debe estar dentro de la jornada laboral."
            });
        }

        var nombre = dto.Nombre.Trim();

        var nombreExiste = await _context.JornadaLaboral
            .AnyAsync(j =>
                j.Nombre.ToLower() == nombre.ToLower() &&
                j.IdJornada != id);

        if (nombreExiste)
        {
            return Conflict(new
            {
                mensaje = "Ya existe otra jornada con ese nombre."
            });
        }

        jornada.Nombre = nombre;
        jornada.HoraEntrada = dto.HoraEntrada;
        jornada.HoraInicioAlmuerzo = dto.HoraInicioAlmuerzo;
        jornada.HoraFinAlmuerzo = dto.HoraFinAlmuerzo;
        jornada.HoraSalida = dto.HoraSalida;
        jornada.ToleranciaEntradaMinutos = dto.ToleranciaEntradaMinutos;
        jornada.Lunes = dto.Lunes;
        jornada.Martes = dto.Martes;
        jornada.Miercoles = dto.Miercoles;
        jornada.Jueves = dto.Jueves;
        jornada.Viernes = dto.Viernes;
        jornada.Sabado = dto.Sabado;
        jornada.Domingo = dto.Domingo;
        jornada.Activo = dto.Activo;
        jornada.FechaModificacion = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Jornada actualizada correctamente."
        });
    }

    // PATCH: api/jornadas/1/estado?activo=false
    [HttpPatch("{id:long}/estado")]
    public async Task<ActionResult> CambiarEstado(
        long id,
        [FromQuery] bool activo)
    {
        var jornada = await _context.JornadaLaboral
            .FirstOrDefaultAsync(j => j.IdJornada == id);

        if (jornada is null)
        {
            return NotFound(new
            {
                mensaje = "Jornada no encontrada."
            });
        }

        if (!activo)
        {
            var tieneEmpleadosActivos = await _context.Empleado
                .AnyAsync(e =>
                    e.IdJornada == id &&
                    e.Activo);

            if (tieneEmpleadosActivos)
            {
                return Conflict(new
                {
                    mensaje = "No se puede desactivar la jornada porque tiene empleados activos asignados."
                });
            }
        }

        jornada.Activo = activo;
        jornada.FechaModificacion = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = activo
                ? "Jornada activada correctamente."
                : "Jornada desactivada correctamente."
        });
    }
}