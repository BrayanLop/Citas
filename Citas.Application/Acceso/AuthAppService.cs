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

    public async Task<TokenResponseDto> GenerarTokenAsync(TokenRequestDto dto, CancellationToken cancellationToken = default)
    {
        var empresa = await _empresas.ObtenerPorIdAsync(dto.Tenant, cancellationToken);
        if (empresa is null || !empresa.Activo)
            throw new DomainException("Empresa/tenant no existe o está inactiva.");

        var nombre = string.IsNullOrWhiteSpace(dto.Usuario) ? "demo" : dto.Usuario;
        var (token, expira) = _token.Generar(new TokenClaims(Tenant: empresa.Id, Nombre: nombre));

        return new TokenResponseDto { Token = token, Tenant = empresa.Id, ExpiraUtc = expira };
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
            usuario.TenantId = tenant; // El token de login no trae tenant; se sella explícitamente.
            await _usuarios.AgregarAsync(usuario, cancellationToken);
            await _uow.GuardarCambiosAsync(cancellationToken);

            await AsignarRolAsync(usuario.Id, tenant, "Cliente", cancellationToken);
        }

        var (token, expira) = _token.Generar(new TokenClaims(
            Tenant: tenant, PersonaId: personaId, UserId: usuario.Id, Nombre: nombre, Modo: "cliente"));

        return new SelectEmpresaResponseDto
        {
            Token = token,
            Tenant = tenant,
            EmpresaNombre = empresa.Nombre,
            UserId = usuario.Id,
            ExpiraUtc = expira,
        };
    }

    public async Task<SelectEmpresaResponseDto> TokenEmpleadoAsync(EmpleadoTokenRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Tenant) || string.IsNullOrWhiteSpace(dto.Nombre))
            throw new DomainException("Tenant y nombre son obligatorios.");

        if (!_token.EmpleadoExternoEsValido(dto.NaturalToken))
            throw new UnauthorizedAppException("Token de Natural inválido o ausente.");

        var tenant = dto.Tenant.Trim();
        var empresa = await _empresas.ObtenerPorIdAsync(tenant, cancellationToken);
        if (empresa is null || !empresa.Activo)
            throw new DomainException("Empresa no existe o está inactiva.");

        var nombre = dto.Nombre.Trim();
        var usuario = await _usuarios.BuscarEnTenantPorNombreAsync(tenant, nombre, cancellationToken);
        if (usuario is null)
        {
            usuario = Usuario.Crear(nombre);
            usuario.TenantId = tenant; // El llamador no está en un contexto de tenant; se sella explícitamente.
            await _usuarios.AgregarAsync(usuario, cancellationToken);
            await _uow.GuardarCambiosAsync(cancellationToken);

            await AsignarRolAsync(usuario.Id, tenant, "Empleado", cancellationToken);
        }

        var (token, expira) = _token.Generar(new TokenClaims(
            Tenant: tenant, UserId: usuario.Id, Nombre: nombre, Modo: "empresa"));

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
        var (token, expira) = _token.Generar(new TokenClaims(
            PersonaId: persona.Id, Nombre: nombre, Modo: "cliente"));

        return new ClienteAuthResponseDto
        {
            Token = token,
            PersonaId = persona.Id,
            Nombre = nombre,
            ExpiraUtc = expira,
        };
    }

    /// <summary>Asegura que exista el rol en el tenant y lo asigna al usuario.</summary>
    private async Task AsignarRolAsync(int usuarioId, string tenant, string nombreRol, CancellationToken cancellationToken)
    {
        var rol = await _roles.BuscarEnTenantPorNombreAsync(tenant, nombreRol, cancellationToken);
        if (rol is null)
        {
            rol = Rol.Crear(nombreRol);
            rol.TenantId = tenant;
            await _roles.AgregarAsync(rol, cancellationToken);
            await _uow.GuardarCambiosAsync(cancellationToken);
        }

        if (!await _roles.ExisteAsignacionAsync(usuarioId, rol.Id, cancellationToken))
        {
            var asignacion = RolUsuario.Crear(usuarioId, rol.Id);
            asignacion.TenantId = tenant;
            await _roles.AgregarAsignacionAsync(asignacion, cancellationToken);
            await _uow.GuardarCambiosAsync(cancellationToken);
        }
    }
}
