using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timbre.Api.Data;
using Timbre.Api.DTOs.Empleados;
using Timbre.Api.Models;

namespace Timbre.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmpleadosController : ControllerBase
{
    private readonly TimbreDbContext _context;

    public EmpleadosController(TimbreDbContext context)
    {
        _context = context;
    }

    // =====================================================
    // GET: api/empleados
    // =====================================================
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmpleadoDto>>> GetEmpleados()
    {
        var empleados = await _context.Empleado
            .AsNoTracking()
            .OrderBy(e => e.Apellidos)
            .ThenBy(e => e.Nombres)
            .Select(e => new EmpleadoDto
            {
                IdEmpleado = e.IdEmpleado,
                Identificacion = e.Identificacion,
                Nombres = e.Nombres,
                Apellidos = e.Apellidos,
                Correo = e.Correo,
                Telefono = e.Telefono,
                Area = e.Area,
                Cargo = e.Cargo,
                FechaIngreso = e.FechaIngreso,
                FechaSalida = e.FechaSalida,
                IdJornada = e.IdJornada,
                Jornada = e.IdJornadaNavigation.Nombre,
                Activo = e.Activo
            })
            .ToListAsync();

        return Ok(empleados);
    }


    // =====================================================
    // GET: api/empleados/1
    // =====================================================
    [HttpGet("{id:long}")]
    public async Task<ActionResult<EmpleadoDto>> GetEmpleado(long id)
    {
        var empleado = await _context.Empleado
            .AsNoTracking()
            .Where(e => e.IdEmpleado == id)
            .Select(e => new EmpleadoDto
            {
                IdEmpleado = e.IdEmpleado,
                Identificacion = e.Identificacion,
                Nombres = e.Nombres,
                Apellidos = e.Apellidos,
                Correo = e.Correo,
                Telefono = e.Telefono,
                Area = e.Area,
                Cargo = e.Cargo,
                FechaIngreso = e.FechaIngreso,
                FechaSalida = e.FechaSalida,
                IdJornada = e.IdJornada,
                Jornada = e.IdJornadaNavigation.Nombre,
                Activo = e.Activo
            })
            .FirstOrDefaultAsync();

        if (empleado is null)
        {
            return NotFound(new
            {
                mensaje = "Empleado no encontrado."
            });
        }

        return Ok(empleado);
    }


    // =====================================================
    // POST: api/empleados
    // =====================================================
    [HttpPost]
    public async Task<ActionResult> CrearEmpleado(
        [FromBody] CrearEmpleadoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Identificacion))
        {
            return BadRequest(new
            {
                mensaje = "La identificación es obligatoria."
            });
        }

        if (string.IsNullOrWhiteSpace(dto.Nombres))
        {
            return BadRequest(new
            {
                mensaje = "Los nombres son obligatorios."
            });
        }

        if (string.IsNullOrWhiteSpace(dto.Apellidos))
        {
            return BadRequest(new
            {
                mensaje = "Los apellidos son obligatorios."
            });
        }

        var identificacion = dto.Identificacion.Trim();

        var identificacionExiste = await _context.Empleado
            .AnyAsync(e => e.Identificacion == identificacion);

        if (identificacionExiste)
        {
            return Conflict(new
            {
                mensaje = "Ya existe un empleado con esa identificación."
            });
        }

        var jornadaExiste = await _context.JornadaLaboral
            .AnyAsync(j =>
                j.IdJornada == dto.IdJornada &&
                j.Activo);

        if (!jornadaExiste)
        {
            return BadRequest(new
            {
                mensaje = "La jornada indicada no existe o está inactiva."
            });
        }

        var empleado = new Empleado
        {
            Identificacion = identificacion,
            Nombres = dto.Nombres.Trim(),
            Apellidos = dto.Apellidos.Trim(),
            Correo = dto.Correo?.Trim(),
            Telefono = dto.Telefono?.Trim(),
            Area = dto.Area?.Trim(),
            Cargo = dto.Cargo?.Trim(),
            FechaIngreso = dto.FechaIngreso,
            IdJornada = dto.IdJornada,
            Activo = true
        };

        _context.Empleado.Add(empleado);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetEmpleado),
            new { id = empleado.IdEmpleado },
            new
            {
                idEmpleado = empleado.IdEmpleado,
                mensaje = "Empleado creado correctamente."
            });
    }


    // =====================================================
    // PUT: api/empleados/1
    // =====================================================
    [HttpPut("{id:long}")]
    public async Task<ActionResult> ActualizarEmpleado(
        long id,
        [FromBody] ActualizarEmpleadoDto dto)
    {
        var empleado = await _context.Empleado
            .FirstOrDefaultAsync(e => e.IdEmpleado == id);

        if (empleado is null)
        {
            return NotFound(new
            {
                mensaje = "Empleado no encontrado."
            });
        }

        if (string.IsNullOrWhiteSpace(dto.Identificacion))
        {
            return BadRequest(new
            {
                mensaje = "La identificación es obligatoria."
            });
        }

        if (string.IsNullOrWhiteSpace(dto.Nombres))
        {
            return BadRequest(new
            {
                mensaje = "Los nombres son obligatorios."
            });
        }

        if (string.IsNullOrWhiteSpace(dto.Apellidos))
        {
            return BadRequest(new
            {
                mensaje = "Los apellidos son obligatorios."
            });
        }

        var identificacion = dto.Identificacion.Trim();

        var identificacionExiste = await _context.Empleado
            .AnyAsync(e =>
                e.Identificacion == identificacion &&
                e.IdEmpleado != id);

        if (identificacionExiste)
        {
            return Conflict(new
            {
                mensaje = "Ya existe otro empleado con esa identificación."
            });
        }

        var jornadaExiste = await _context.JornadaLaboral
            .AnyAsync(j =>
                j.IdJornada == dto.IdJornada &&
                j.Activo);

        if (!jornadaExiste)
        {
            return BadRequest(new
            {
                mensaje = "La jornada indicada no existe o está inactiva."
            });
        }

        empleado.Identificacion = identificacion;
        empleado.Nombres = dto.Nombres.Trim();
        empleado.Apellidos = dto.Apellidos.Trim();
        empleado.Correo = dto.Correo?.Trim();
        empleado.Telefono = dto.Telefono?.Trim();
        empleado.Area = dto.Area?.Trim();
        empleado.Cargo = dto.Cargo?.Trim();
        empleado.FechaIngreso = dto.FechaIngreso;
        empleado.FechaSalida = dto.FechaSalida;
        empleado.IdJornada = dto.IdJornada;
        empleado.Activo = dto.Activo;
        empleado.FechaModificacion = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Empleado actualizado correctamente."
        });
    }


    // =====================================================
    // PATCH: api/empleados/1/estado
    // =====================================================
    [HttpPatch("{id:long}/estado")]
    public async Task<ActionResult> CambiarEstado(
        long id,
        [FromQuery] bool activo)
    {
        var empleado = await _context.Empleado
            .FirstOrDefaultAsync(e => e.IdEmpleado == id);

        if (empleado is null)
        {
            return NotFound(new
            {
                mensaje = "Empleado no encontrado."
            });
        }

        empleado.Activo = activo;
        empleado.FechaModificacion = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = activo
                ? "Empleado activado correctamente."
                : "Empleado desactivado correctamente."
        });
    }
}