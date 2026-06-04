using Citas.DTOs;
using Citas.Models;
using Citas.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Citas.Controllers;

[ApiController]
[Route("[controller]")]
// Administracion de tenants. En produccion deberia restringirse a un rol de super-admin.
[AllowAnonymous]
public class EmpresasController : ControllerBase
{
    private readonly IRepository<Empresa> _empresas;

    public EmpresasController(IRepository<Empresa> empresas)
    {
        _empresas = empresas;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmpresaResponseDto>>> GetEmpresas()
    {
        var empresas = await _empresas.GetAllAsync();

        var response = empresas.Select(e => new EmpresaResponseDto
        {
            Id = e.Id,
            Nombre = e.Nombre,
            Activo = e.Activo
        });

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<EmpresaResponseDto>> CreateEmpresa([FromBody] CreateEmpresaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Id))
            return BadRequest("El código de la empresa es obligatorio");

        if (await _empresas.ExistsAsync(e => e.Id == dto.Id))
            return Conflict($"Ya existe una empresa con el código '{dto.Id}'");

        var empresa = new Empresa
        {
            Id = dto.Id,
            Nombre = dto.Nombre,
            Activo = true
        };

        await _empresas.AddAsync(empresa);
        await _empresas.SaveChangesAsync();

        var response = new EmpresaResponseDto
        {
            Id = empresa.Id,
            Nombre = empresa.Nombre,
            Activo = empresa.Activo
        };

        return CreatedAtAction(nameof(GetEmpresas), new { id = empresa.Id }, response);
    }
}
