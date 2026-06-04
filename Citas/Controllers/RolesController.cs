using Citas.DTOs;
using Citas.Models;
using Citas.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Citas.Controllers;

[ApiController]
[Route("[controller]")]
public class RolesController : ControllerBase
{
    private readonly IRepository<Rol> _roles;

    public RolesController(IRepository<Rol> roles)
    {
        _roles = roles;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RolResponseDto>>> GetRoles()
    {
        var roles = await _roles.GetAllAsync();

        var response = roles.Select(r => new RolResponseDto
        {
            Id = r.Id,
            NombreRol = r.NombreRol
        });

        return Ok(response);
    }
}
