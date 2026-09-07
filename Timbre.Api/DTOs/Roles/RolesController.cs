using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timbre.Api.Data;
using Timbre.Api.DTOs.Roles;

namespace Timbre.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly TimbreDbContext _context;

    public RolesController(TimbreDbContext context)
    {
        _context = context;
    }

    // GET: api/roles
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RolDto>>> GetRoles()
    {
        var roles = await _context.Rol
            .AsNoTracking()
            .OrderBy(r => r.Nombre)
            .Select(r => new RolDto
            {
                IdRol = r.IdRol,
                Nombre = r.Nombre,
                Descripcion = r.Descripcion,
                Activo = r.Activo
            })
            .ToListAsync();

        return Ok(roles);
    }

    // GET: api/roles/1
    [HttpGet("{id:long}")]
    public async Task<ActionResult<RolDto>> GetRol(long id)
    {
        var rol = await _context.Rol
            .AsNoTracking()
            .Where(r => r.IdRol == id)
            .Select(r => new RolDto
            {
                IdRol = r.IdRol,
                Nombre = r.Nombre,
                Descripcion = r.Descripcion,
                Activo = r.Activo
            })
            .FirstOrDefaultAsync();

        if (rol is null)
        {
            return NotFound(new
            {
                mensaje = "Rol no encontrado."
            });
        }

        return Ok(rol);
    }
}