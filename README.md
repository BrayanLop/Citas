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

## Arranque rápido con Docker (todo el sistema)

La forma más fácil de levantar **base de datos + API + frontend** de una sola vez.
Solo necesitas tener el repo del frontend clonado al lado de este
(`../citas-frontend`) y Docker corriendo:

```bash
docker compose up --build
```

Y listo:

- Frontend: <http://localhost:5173>
- API: <http://localhost:8080> (docs en `/scalar/v1`, health en `/health`)

El Postgres arranca vacío y la API le aplica las migraciones sola al iniciar, así
que no hay que crear tablas a mano. Para apagar todo: `docker compose down`
(agrega `-v` si quieres borrar también los datos).

## Requisitos (si lo corres sin Docker)

- .NET SDK 10
- Una base de datos PostgreSQL (sirve local, Supabase, Railway, etc.)

## Configuración

El archivo `Citas/appsettings.json` **no está en el repo** porque lleva la cadena
de conexión y la clave del JWT. Crea el tuyo a partir de la plantilla:

```bash
cd Citas
cp appsettings.Example.json appsettings.json
```

y completa `ConnectionStrings:DefaultConnection` y `Jwt:Key` con tus valores.

## Base de datos (migraciones)

El historial del esquema se maneja con la CLI de EF Core. Para crear/actualizar la
base de datos a la última versión:

```bash
dotnet ef database update --project Citas.Infrastructure --startup-project Citas
```

Para agregar una migración nueva cuando cambie el modelo:

```bash
dotnet ef migrations add NombreDeLaMigracion --project Citas.Infrastructure --startup-project Citas
```

> Si no tienes la herramienta: `dotnet tool install --global dotnet-ef`.

## Correr la API

Usa el perfil **http** (evita el redirect a HTTPS, que es lo que espera el front):

```bash
cd Citas
dotnet run --launch-profile http
```

Queda escuchando en `http://localhost:5202`.

- Healthcheck: `GET http://localhost:5202/health`
- En desarrollo hay documentación interactiva en `http://localhost:5202/scalar/v1`

## Despliegue

Hay notas de despliegue en Railway en [`RAILWAY.md`](RAILWAY.md).
