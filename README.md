# Citas · API (.NET)

Backend del sistema de citas. Está hecho en **.NET 10 / C#** con **arquitectura
hexagonal** (capas separadas) y **PostgreSQL** por medio de **Entity Framework Core**.

El frontend (React) vive en un repo aparte y consume esta API.

## Cómo está organizado

La solución (`Citas.slnx`) tiene cuatro proyectos, cada uno con su responsabilidad:

```
Citas.Domain          Entidades y reglas de negocio. C# puro, sin EF ni web.
Citas.Application     Casos de uso (servicios) y puertos (interfaces de repos, etc.).
Citas.Infrastructure  EF Core: DbContext, modelos de persistencia, mappers y repos.
Citas (Web/Api)       Controladores, JWT y arranque (Program.cs).
```

La idea de fondo: el dominio no sabe que existe una base de datos. La capa de
aplicación depende de **interfaces** (puertos) y la infraestructura las implementa
(adaptadores). En `Program.cs` se enlaza cada puerto con su adaptador.

Un detalle del que estamos orgullosos: las entidades de dominio **no son** las
entidades de EF. En `Infrastructure/Persistencia` hay un modelo por tabla
(`CitaDbModel`, `UsuarioDbModel`, ...) y un **mapper** por agregado que traduce
dominio ↔ base de datos. Así el dominio queda limpio y la persistencia aislada.

## Configuración (variables de entorno)

Ningún secreto vive en el repositorio. La API **no arranca** si falta una variable
obligatoria y el error dice cuál. Plantilla con todos los nombres: [`.env.example`](.env.example).

| Variable | Obligatoria | Para qué |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | sí | BD propia de Citas (PostgreSQL/Supabase) |
| `Jwt__Issuer`, `Jwt__Audience` | sí | Tokens de **clientes** que emite Citas (el issuer no puede ser `Natural.Api`) |
| `Jwt__Key` | sí | Clave de firma de los tokens de clientes (≥ 32 caracteres) |
| `NaturalJwt__Key` | sí | **La misma** clave con la que Natural firma sus JWT (≥ 32 caracteres) |
| `NaturalJwt__Issuer`, `NaturalJwt__Audience` | no | Por defecto `Natural.Api` / `Natural.Client` |
| `Cors__AllowedOrigins__0`, `__1`... | no | Orígenes permitidos; vacío = cualquier origen sin credenciales |
| `RunMigrationsOnStartup` | no | `true` aplica las migraciones al arrancar |
| `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` | solo Docker | Contenedor local de PostgreSQL |

En local (sin Docker) usa `dotnet user-secrets --project Citas set "Jwt:Key" "..."`
(mismas claves con `:`) o un `Citas/appsettings.json` propio a partir de
`appsettings.Example.json` (no se versiona).

## Autenticación

Hay dos tipos de usuario y cada uno trae su propio token (`Authorization: Bearer ...`).
Citas decide cómo validarlo mirando el `iss` del token:

- **Personal de la empresa** (administradores, trabajadores): usa el **mismo JWT de
  Natural**, sin segundo login. Se valida con `NaturalJwt__Key`, issuer `Natural.Api`,
  audience `Natural.Client` y expiración. Citas lee `empresaId` (= tenant), `sub`
  (= id de la persona en Natural; también se acepta `nameidentifier`), `name` y el
  rol (`role`/`ClaimTypes.Role`): `01` administrador, `02` trabajador, `03` super empleado.
  La primera vez que una persona entra a una empresa con la agenda activada, Citas le
  crea su Usuario automáticamente, vinculado por (tenant, id de Natural); nunca por nombre.
- **Clientes externos**: cuenta propia en Citas. `POST /Auth/register` o `POST /Auth/login`
  devuelven un token sin empresa; con él `POST /Auth/select-empresa` (empresa existente y
  activa) devuelve el token de cliente con `tenant`, `userId`, `personaId` y `mode=cliente`.

## Endpoints principales y permisos

| Endpoint | Quién |
|---|---|
| `GET /health` | anónimo (solo `status` y hora) |
| `GET /Empresas` | anónimo; solo empresas activas (Id y Nombre) |
| `POST /Empresas` ("activar agenda") | personal `01`/`03`; el Id es el `empresaId` del token |
| `POST /Auth/register`, `POST /Auth/login` | anónimo |
| `POST /Auth/select-empresa` | cliente con token de login |
| `GET /Servicios` | personal o cliente del tenant (lectura) |
| `POST/PUT/DELETE /Servicios` | personal `01`/`03` |
| `GET /Usuarios` | personal del tenant; el cliente solo ve empleados activos |
| `POST/DELETE /Usuarios`, `GET /Roles`, `PATCH /AgendaEmpleado/{id}` | personal `01`/`03` |
| `GET /Citas` | personal ve todo el tenant; el cliente solo las suyas |
| `POST /Citas` | cliente (para sí mismo) o personal `01`/`03` |
| `PATCH /Citas/{id}` (estado) | `01`/`03` cualquiera; `02` solo las que atiende; cliente solo cancelar las suyas |
| `PUT /Citas/{id}`, `DELETE /Citas/{id}` | personal `01`/`03` |

Sin token: 401. Con token pero sin permiso (o empresa sin agenda activada): 403.

## Arranque rápido con Docker (todo el sistema)

Necesitas el repo del frontend clonado al lado (`../citas-frontend`) y Docker:

```bash
cp .env.example .env   # rellena POSTGRES_PASSWORD, Jwt__Key y NaturalJwt__Key
docker compose up --build
```

- Frontend: <http://localhost:5173>
- API: <http://localhost:8080> (health en `/health`; docs en `/scalar/v1` solo en Development)

El Postgres arranca vacío y la API le aplica las migraciones al iniciar. Para apagar
todo: `docker compose down` (agrega `-v` para borrar también los datos).

## Base de datos desde cero

El esquema completo está en una única migración, `InitialCreate`
(`Citas.Infrastructure/Migrations`). No se crean empresas de ejemplo: cada empresa nace
cuando un administrador de Natural la activa (`POST /Empresas`).

1. Crea (o resetea) una base vacía para Citas. Si la base ya tenía tablas de una versión
   anterior, bórralas antes (incluida `__EFMigrationsHistory`).
2. Define la cadena de conexión en la terminal:
   `export ConnectionStrings__DefaultConnection="Host=...;Database=...;Username=...;Password=..."`
3. Aplica la migración:

```bash
dotnet ef database update --project Citas.Infrastructure --startup-project Citas.Infrastructure
```

Alternativa: arrancar la API con `RunMigrationsOnStartup=true`.

Para agregar una migración cuando cambie el modelo (no necesita conectarse a la BD,
pero sí que la variable de la cadena exista):

```bash
dotnet ef migrations add NombreDeLaMigracion --project Citas.Infrastructure --startup-project Citas.Infrastructure
```

> Si no tienes la herramienta: `dotnet tool install --global dotnet-ef`.

## Correr la API

Requisitos: .NET SDK 10 y una base PostgreSQL. Usa el perfil **http**:

```bash
cd Citas
dotnet run --launch-profile http
```

Queda escuchando en `http://localhost:5202` (`/health`; en Development, `/scalar/v1`).

## Despliegue

Hay notas de despliegue en Railway en [`RAILWAY.md`](RAILWAY.md).
