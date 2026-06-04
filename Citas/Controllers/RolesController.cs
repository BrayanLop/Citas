using Citas.Data;
using Citas.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Citas.Controllers;

[ApiController]
[Route("[controller]")]
public class RolesController : ControllerBase
{
    private readonly CitasContext _context;

    public RolesController(CitasContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RolResponseDto>>> GetRoles()
    {
        var roles = await _context.Roles
            .Select(r => new RolResponseDto
            {
                Id = r.Id,
                NombreRol = r.NombreRol
            })
            .ToListAsync();

        return Ok(roles);
    }
}
