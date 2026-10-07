using Citas.Application.Acceso.Abstractions;
using Citas.Application.Common;
using Citas.Domain.Acceso;
using Citas.Domain.Common;
using Citas.Domain.Empresas;
using Citas.Models;

namespace Citas.Application.Acceso;

public class AuthAppService : IAuthAppService
{
    private readonly IEmpresaRepository _empresas;
    private readonly IPersonaRepository _personas;
    private readonly IUsuarioRepository _usuarios;
    private readonly IRolRepository _roles;
    private readonly ITokenService _token;
    private readonly IPasswordHasher _hasher;
    private readonly IUnitOfWork _uow;

    public AuthAppService(
        IEmpresaRepository empresas,
        IPersonaRepository personas,
        IUsuarioRepository usuarios,
        IRolRepository roles,
        ITokenService token,
        IPasswordHasher hasher,
        IUnitOfWork uow)
    {
        _empresas = empresas;
        _personas = personas;
        _usuarios = usuarios;
        _roles = roles;
        _token = token;
        _hasher = hasher;
        _uow = uow;
    }

    public async Task<ClienteAuthResponseDto> RegisterAsync(RegisterClienteDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            throw new DomainException("Email y contraseña son obligatorios.");
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            throw new DomainException("El nombre es obligatorio.");

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _personas.ExistePorEmailAsync(email, cancellationToken))
            throw new ConflictException("Ya existe una cuenta con ese correo.");

        var claveHash = _hasher.Hash(dto.Password);
        var persona = Persona.Registrar(dto.Nombre, dto.Apellido, dto.Email, dto.Celular, claveHash);
        await _personas.AgregarAsync(persona, cancellationToken);
        await _uow.GuardarCambiosAsync(cancellationToken);

        return BuildClienteAuth(persona);
    }

    public async Task<ClienteAuthResponseDto> LoginAsync(LoginClienteDto dto, CancellationToken cancellationToken = default)
    {
        var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();
        var persona = await _personas.ObtenerPorEmailAsync(email, cancellationToken);
        if (persona is null || !_hasher.Verify(dto.Password ?? string.Empty, persona.ClaveHash))
            throw new UnauthorizedAppException("Correo o contraseña incorrectos.");

        return BuildClienteAuth(persona);
    }

    public async Task<SelectEmpresaResponseDto> SelectEmpresaAsync(int personaId, SelectEmpresaDto dto, CancellationToken cancellationToken = default)
    {
        var tenant = (dto.Tenant ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(tenant))
            throw new DomainException("Tenant requerido.");

        var empresa = await _empresas.ObtenerPorIdAsync(tenant, cancellationToken);
        if (empresa is null || !empresa.Activo)
            throw new DomainException("Empresa no existe o está inactiva.");

        var persona = await _personas.ObtenerPorIdAsync(personaId, cancellationToken);
        if (persona is null)
            throw new UnauthorizedAppException("Persona no encontrada.");

        var nombre = persona.NombreCompleto;

        var usuario = await _usuarios.BuscarEnTenantPorPersonaAsync(personaId, tenant, cancellationToken);
        if (usuario is null)
        {
            usuario = Usuario.CrearParaPersona(string.IsNullOrWhiteSpace(nombre) ? persona.Nombre : nombre, personaId);
            // El token de login todavía no trae empresa; sellamos el tenant explícitamente.
            await _usuarios.AgregarAsync(usuario, tenant, cancellationToken);
            await _uow.GuardarCambiosAsync(cancellationToken);

            await AsignadorRoles.AsignarAsync(_roles, _uow, usuario.Id, tenant, AsignadorRoles.Cliente, cancellationToken);
        }

        var (token, expira) = _token.Generar(new TokenClaims(
            Tenant: tenant, PersonaId: personaId, UserId: usuario.Id, Nombre: nombre));

        return new SelectEmpresaResponseDto
        {
            Token = token,
            Tenant = tenant,
            EmpresaNombre = empresa.Nombre,
            UserId = usuario.Id,
            ExpiraUtc = expira,
        };
    }

    private ClienteAuthResponseDto BuildClienteAuth(Persona persona)
    {
        var nombre = persona.NombreCompleto;
        var (token, expira) = _token.Generar(new TokenClaims(PersonaId: persona.Id, Nombre: nombre));

        return new ClienteAuthResponseDto
        {
            Token = token,
            PersonaId = persona.Id,
            Nombre = nombre,
            ExpiraUtc = expira,
        };
    }
}
