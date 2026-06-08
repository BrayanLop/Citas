using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Citas.Data;
using Citas.DTOs;
using Citas.Helpers;
using Citas.Models;
using Citas.Multitenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Citas.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly CitasContext _db;
    private readonly IConfiguration _config;

    public AuthController(CitasContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Emite un JWT con el claim 'tenant' para la empresa indicada.
    /// Usado por el puente de empleado (Natural -> Citas). NO valida credenciales.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("token")]
    public async Task<ActionResult<TokenResponseDto>> GenerarToken([FromBody] TokenRequestDto dto)
    {
        var empresa = await _db.Empresas.FirstOrDefaultAsync(e => e.Id == dto.Tenant);
        if (empresa == null || !empresa.Activo)
            return BadRequest("Empresa/tenant no existe o esta inactiva");

        var (token, expira) = GenerarJwt(new[]
        {
            new Claim(TenantClaimTypes.Tenant, empresa.Id),
            new Claim(ClaimTypes.Name, string.IsNullOrWhiteSpace(dto.Usuario) ? "demo" : dto.Usuario),
        });

        return Ok(new TokenResponseDto { Token = token, Tenant = empresa.Id, ExpiraUtc = expira });
    }

    /// <summary>Registro de un cliente externo. Crea una Persona global con contrasena (PBKDF2).</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<ClienteAuthResponseDto>> Register([FromBody] RegisterClienteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest("Email y contrasena son obligatorios");
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return BadRequest("El nombre es obligatorio");

        var email = dto.Email.Trim().ToLowerInvariant();
        var existe = await _db.Personas.AnyAsync(p => p.Email != null && p.Email.ToLower() == email);
        if (existe)
            return Conflict("Ya existe una cuenta con ese correo");

        var persona = new Persona
        {
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            Email = email,
            Celular = string.IsNullOrWhiteSpace(dto.Celular) ? null : dto.Celular.Trim(),
            ClaveHash = PasswordHasher.Hash(dto.Password),
        };
        _db.Personas.Add(persona);
        await _db.SaveChangesAsync();

        return Ok(BuildClienteAuth(persona));
    }

    /// <summary>Login global del cliente (email + contrasena contra Persona).</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ClienteAuthResponseDto>> Login([FromBody] LoginClienteDto dto)
    {
        var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();
        var persona = await _db.Personas.FirstOrDefaultAsync(p => p.Email != null && p.Email.ToLower() == email);
        if (persona == null || !PasswordHasher.Verify(dto.Password ?? string.Empty, persona.ClaveHash))
            return Unauthorized("Correo o contrasena incorrectos");

        return Ok(BuildClienteAuth(persona));
    }

    /// <summary>
    /// Tras elegir empresa, asegura un Usuario(cliente) en ese tenant ligado a la Persona global
    /// y emite un token con el claim 'tenant' para operar en esa empresa.
    /// </summary>
    [Authorize]
    [HttpPost("select-empresa")]
    public async Task<ActionResult<SelectEmpresaResponseDto>> SelectEmpresa([FromBody] SelectEmpresaDto dto)
    {
        if (!int.TryParse(User.FindFirstValue("personaId"), out var personaId))
            return Unauthorized("Token sin personaId");

        var tenant = (dto.Tenant ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(tenant))
            return BadRequest("Tenant requerido");

        var empresa = await _db.Empresas.FirstOrDefaultAsync(e => e.Id == tenant);
        if (empresa == null || !empresa.Activo)
            return BadRequest("Empresa no existe o esta inactiva");

        var persona = await _db.Personas.FirstOrDefaultAsync(p => p.Id == personaId);
        if (persona == null)
            return Unauthorized("Persona no encontrada");

        var nombre = $"{persona.Nombre} {persona.Apellido}".Trim();

        // Asegurar Usuario(cliente) en el tenant (ignorando el filtro global por tenant).
        var usuario = await _db.Usuarios.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.PersonaId == personaId && u.TenantId == tenant);

        if (usuario == null)
        {
            usuario = new Usuario
            {
                NombreUsuario = string.IsNullOrWhiteSpace(nombre) ? persona.Nombre : nombre,
                PersonaId = personaId,
                TenantId = tenant,
                Estado = true,
            };
            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync();

            await AsignarRolAsync(usuario.Id, tenant, "Cliente");
        }

        var (token, expira) = GenerarJwt(new[]
        {
            new Claim(TenantClaimTypes.Tenant, tenant),
            new Claim("personaId", personaId.ToString()),
            new Claim("userId", usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, nombre),
            new Claim("mode", "cliente"),
        });

        return Ok(new SelectEmpresaResponseDto
        {
            Token = token,
            Tenant = tenant,
            EmpresaNombre = empresa.Nombre,
            UserId = usuario.Id,
            ExpiraUtc = expira,
        });
    }

    // ===== Helpers =====

    private ClienteAuthResponseDto BuildClienteAuth(Persona persona)
    {
        var nombre = $"{persona.Nombre} {persona.Apellido}".Trim();
        var (token, expira) = GenerarJwt(new[]
        {
            new Claim("personaId", persona.Id.ToString()),
            new Claim(ClaimTypes.Name, nombre),
            new Claim("mode", "cliente"),
        });
        return new ClienteAuthResponseDto
        {
            Token = token,
            PersonaId = persona.Id,
            Nombre = nombre,
            ExpiraUtc = expira,
        };
    }

    /// <summary>Asegura que exista el rol indicado en el tenant y lo asigna al usuario.</summary>
    private async Task AsignarRolAsync(int usuarioId, string tenant, string nombreRol)
    {
        var rol = await _db.Roles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.TenantId == tenant && r.NombreRol == nombreRol);
        if (rol == null)
        {
            rol = new Rol { NombreRol = nombreRol, TenantId = tenant };
            _db.Roles.Add(rol);
            await _db.SaveChangesAsync();
        }

        var yaTiene = await _db.RolUsuarios.IgnoreQueryFilters()
            .AnyAsync(ru => ru.IdUsuario == usuarioId && ru.IdRol == rol.Id);
        if (!yaTiene)
        {
            _db.RolUsuarios.Add(new RolUsuario { IdUsuario = usuarioId, IdRol = rol.Id, TenantId = tenant });
            await _db.SaveChangesAsync();
        }
    }

    private (string token, DateTime expira) GenerarJwt(IEnumerable<Claim> claims, int horas = 8)
    {
        var jwt = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expira = DateTime.UtcNow.AddHours(horas);

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expira,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}
