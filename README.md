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
| `Agenda__ZonaHoraria` | no | Zona horaria del negocio, id IANA (por defecto `America/Bogota`; si el sistema no la tiene, UTC-5 fijo). Un id desconocido hace fallar el arranque |
| `Agenda__IntervaloMinutos` | no | Cada cuántos minutos se ofrecen horas de inicio en la disponibilidad (por defecto 15) |
| `Agenda__AnticipacionMinimaMinutos` | no | Margen mínimo entre ahora y el inicio de una cita nueva o reprogramada (por defecto 0) |
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

Cada cita lleva **al menos un servicio** (`idServicios` en `POST /Citas`, ids de Citas):
deben ser del tenant y agendables, el empleado debe estar activo, y el fin se calcula como
inicio + suma de los `TiempoEstimado`. Cada línea (`DetalleCitas`) guarda el valor y la
duración del servicio en el momento de agendar (o de reprogramar con servicios nuevos).

## Agenda: horarios, bloqueos y disponibilidad

### Fechas y zona horaria

- En la BD, `Citas.Inicio/Fin` y `BloqueosAgenda.Inicio/Fin` son `timestamptz` (instantes
  UTC). El horario semanal (`AgendaEmpleados`) guarda día + horas locales (`time`).
- La API habla en **hora local del negocio** (`Agenda__ZonaHoraria`, por defecto
  `America/Bogota`):
  - Citas, entrada: `fechaCita` (`"YYYY-MM-DD"` o `"YYYY-MM-DDT00:00:00"`, solo cuenta la
    fecha) + `horaEstimadaCita` (`"HH:mm:ss"`), sin offset.
  - Citas, salida: `fechaCita` (`"YYYY-MM-DDT00:00:00"`), `horaEstimadaCita` y
    `horaEstimadaFin` (`"HH:mm:ss"`) locales, y además `inicio`/`fin` con el offset del
    negocio (`"2026-10-20T09:00:00-05:00"`).
  - Bloqueos: `inicio`/`fin` ISO 8601; sin offset = hora local del negocio, con offset o
    `Z` se respeta el instante. Se devuelven con el offset del negocio.
  - Días de la semana: `0` = domingo … `6` = sábado (como `Date.getDay()` en JS).
- "Hoy", "pasado" y los días de la disponibilidad se calculan en la zona del negocio.

### Reglas al crear (`POST /Citas`) y reprogramar (`PUT /Citas/{id}`)

1. Al menos un servicio agendable (400).
2. No en el pasado ni antes de `Agenda__AnticipacionMinimaMinutos` (400).
3. Completamente dentro de **una** franja del horario semanal del empleado (409).
4. Sin cruce con bloqueos del empleado (409) ni con sus citas **activas**
   (`Pendiente`/`Confirmada`; `Atendida` y `Cancelada` no ocupan agenda) (409).

Los 409 traen el motivo en `detail` (ProblemDetails). Contra carreras: crear/reprogramar
citas, crear bloqueos y reemplazar el horario toman un *advisory lock* de PostgreSQL por
empleado dentro de una transacción, y la tabla `Citas` tiene la restricción de exclusión
`EX_Citas_EmpleadoSinCruces` (`EXCLUDE USING gist (TenantId =, IdEmpleado =,
tstzrange(Inicio, Fin) &&) WHERE Estado IN ('Pendiente','Confirmada')`, extensión
`btree_gist`, creada en `InitialCreate`). Si aun así salta (SQLSTATE `23P01`) se responde 409.
Citas contiguas (10:00-10:30 y 10:30-11:00) sí se permiten.

Cambiar el horario semanal no toca las citas ya agendadas aunque queden fuera de él: el
horario solo condiciona citas nuevas o reprogramadas. Crear un bloqueo que se cruce con
citas activas se rechaza con 409 indicando cuántas afecta (no se cancelan solas).

### Endpoints

| Endpoint | Entrada | Salida |
|---|---|---|
| `GET /AgendaEmpleado/{usuarioId}` | — | `{ idEmpleado, nombreEmpleado, zonaHoraria, franjas: [{ dia, horaInicio, horaFin }] }` (404 si el empleado no existe o está inactivo) |
| `PUT /AgendaEmpleado/{usuarioId}` | `{ franjas: [{ dia: 1, horaInicio: "08:00:00", horaFin: "12:00:00" }, ...] }` (reemplaza todo; `[]` = no trabaja) | el horario guardado. 400 si hay solapes o inicio ≥ fin |
| `GET /AgendaEmpleado/{usuarioId}/bloqueos?desde=YYYY-MM-DD&hasta=YYYY-MM-DD` | por defecto desde hoy, 31 días; máx. 366 | `[{ idBloqueo, idEmpleado, inicio, fin, motivo }]` |
| `POST /AgendaEmpleado/{usuarioId}/bloqueos` | `{ inicio, fin, motivo? }` (motivo ≤ 200) | 201 con el bloqueo; 409 si afecta citas activas |
| `DELETE /AgendaEmpleado/{usuarioId}/bloqueos/{id}` | — | 204 / 404 |
| `GET /Disponibilidad?empleadoId=&fecha=YYYY-MM-DD&idServicios=1,2` | `idServicios` con comas o repetido | `{ idEmpleado, fecha, duracionTotal, intervaloMinutos, zonaHoraria, horas: ["09:00:00", ...] }` |
| `GET /Disponibilidad/dias?empleadoId=&desde=&hasta=&idServicios=1,2` | máx. 62 días | `{ idEmpleado, desde, hasta, duracionTotal, dias: ["2026-10-20", ...] }` |
| `PUT /Citas/{id}` (reprogramar) | `{ fechaCita?, horaEstimadaCita?, idEmpleado?, idServicios?, observaciones? }`; lo que no venga se conserva; `idServicios` reemplaza los servicios (nueva foto de valor/duración) | 200 con la cita. El estado ya no se cambia aquí (usa `PATCH`) |

Las horas libres se recorren desde el inicio de cada franja cada `Agenda__IntervaloMinutos`
y excluyen horas pasadas (y la anticipación mínima), bloqueos y citas activas. Un día
pasado devuelve `horas: []`.

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
| `POST/DELETE /Usuarios`, `GET /Roles` | personal `01`/`03` |
| `GET /AgendaEmpleado/{usuarioId}` | cualquier miembro del tenant (cliente incluido); solo empleados activos |
| `PUT /AgendaEmpleado/{usuarioId}`, `POST`/`DELETE .../bloqueos` | `01`/`03` sobre cualquier empleado; `02` solo sobre su propia agenda |
| `GET /AgendaEmpleado/{usuarioId}/bloqueos` | personal del tenant (el cliente no ve motivos; la disponibilidad ya los descuenta) |
| `GET /Disponibilidad`, `GET /Disponibilidad/dias` | cualquier miembro del tenant |
| `GET /Citas` | personal ve todo el tenant; el cliente solo las suyas |
| `POST /Citas` | cliente (para sí mismo) o personal `01`/`03` |
| `PATCH /Citas/{id}` (estado) | `01`/`03` cualquiera; `02` solo las que atiende; cliente solo cancelar las suyas |
| `PUT /Citas/{id}` (reprogramar) | `01`/`03` cualquier cita activa; cliente solo las suyas en `Pendiente`; `02` no |
| `DELETE /Citas/{id}` | personal `01`/`03` |

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
