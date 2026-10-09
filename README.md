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
| `NaturalApi__BaseUrl` | sí | URL base de la API de Natural (p. ej. `https://naturalback.vip/v2/api`) para sincronizar |
| `NaturalApi__TimeoutSegundos` | no | Timeout de cada llamada a Natural (por defecto 15) |
| `NaturalApi__TiempoEstimadoPorDefecto` | no | Minutos que recibe un servicio nuevo al sincronizar (por defecto 30) |
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
  Si ese Usuario quedó **inactivo** (ya no está en Natural tras una sincronización), toda
  petición con su token responde 403 con mensaje.
- **Clientes externos**: cuenta propia en Citas. `POST /Auth/register` o `POST /Auth/login`
  devuelven un token sin empresa; con él `POST /Auth/select-empresa` (empresa existente y
  activa) devuelve el token de cliente con `tenant`, `userId`, `personaId` y `mode=cliente`.

## Integración con Natural (catálogo)

Natural es la fuente de los **servicios** y del **personal**; Citas no los crea ni los
borra, los sincroniza sin tocar el backend de Natural. Flujo de puesta en marcha:

1. Un administrador (`01`/`03`) entra con su token de Natural y **activa la agenda**
   (`POST /Empresas`).
2. **Sincroniza con Natural** (`POST /Sincronizacion/Natural`). Repetirlo cuando cambie
   el catálogo o el personal en Natural.
3. **Ajusta las duraciones** (y si se pueden agendar) de los servicios: `PUT /Servicios/{id}`.
4. Los **clientes agendan**: ven solo los servicios agendables y los empleados activos.

`POST /Sincronizacion/Natural` (sin cuerpo) llama a `GET Servicio/Obtener` y
`GET Persona/Obtener` de Natural reenviando el **mismo** `Authorization: Bearer` de la
petición (y el header `empresaId` con el tenant), y en una sola transacción:

- **Servicios** (vinculados por `ServicioNaturalId`, único por empresa): los nuevos se crean
  con `TiempoEstimado = NaturalApi__TiempoEstimadoPorDefecto`; en los existentes se copian
  nombre, valor y `disponible`, **sin tocar** `TiempoEstimado` ni `Activo` (ajustes de
  Citas). Agendable = `disponibleEnNatural && activo`. Los que ya no vienen de Natural (o
  vienen con `disponible=false`) quedan no agendables; nunca se borran (citas históricas).
- **Personal** (vinculado por `PersonaNaturalId`): crea el Usuario (rol Empleado) o
  actualiza nombre completo, email y celular (y lo reactiva); los que ya no están en Natural
  quedan inactivos (salvo quien sincroniza). No se borran.

Respuesta `200`:

```json
{
  "servicios": { "creados": 3, "actualizados": 1, "desactivados": 0 },
  "personal":  { "creados": 2, "actualizados": 0, "desactivados": 1 }
}
```

`actualizados` cuenta solo los registros que cambiaron. Errores: `403` si Natural rechaza
el token (401/403 allá) o la petición no trae token de Natural; `502` si Natural no
responde, tarda más del timeout o devuelve algo no interpretable. En esos casos no se
modifica nada en Citas.

Una cita puede llevar servicios (`idServicios` en `POST /Citas`, ids de Citas): deben ser
del tenant y agendables, el empleado debe estar activo, y la hora de fin se calcula como
inicio + suma de los `TiempoEstimado`. Cada línea (`DetalleCitas`) guarda el valor y la
duración del servicio en el momento de agendar. Pendiente (Fase 4): validar solapes con
otras citas del empleado y con su horario (`AgendaEmpleado`).

## Endpoints principales y permisos

| Endpoint | Quién |
|---|---|
| `GET /health` | anónimo (solo `status` y hora) |
| `GET /Empresas` | anónimo; solo empresas activas (Id y Nombre) |
| `POST /Empresas` ("activar agenda") | personal `01`/`03`; el Id es el `empresaId` del token |
| `POST /Auth/register`, `POST /Auth/login` | anónimo |
| `POST /Auth/select-empresa` | cliente con token de login |
| `POST /Sincronizacion/Natural` | personal `01`/`03` (con su token de Natural) |
| `GET /Servicios` | personal (todos, con `agendable`) o cliente (solo agendables) del tenant |
| `PUT /Servicios/{id}` (`tiempoEstimado`, `activo`) | personal `01`/`03`; no hay `POST`/`DELETE`: los servicios vienen de Natural |
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
cp .env.example .env   # rellena POSTGRES_PASSWORD, Jwt__Key, NaturalJwt__Key y NaturalApi__BaseUrl
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
