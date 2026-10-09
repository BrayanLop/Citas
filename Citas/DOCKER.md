# 🐳 Dockerización del Proyecto Citas

## Requisitos previos
- Docker Desktop instalado ([Descargar](https://www.docker.com/products/docker-desktop))
- Docker Compose incluido (viene con Docker Desktop)

## 🚀 Iniciar el proyecto con Docker

### En Linux/Mac:
```bash
bash start-docker.sh
```

### En Windows (PowerShell):
```powershell
docker-compose up -d
```

### En Windows (CMD):
```cmd
start-docker.bat
```

---

## 📋 Servicios configurados

| Servicio | Puerto | URL |
|----------|--------|-----|
| API .NET | 8080 | http://localhost:8080 |
| API HTTPS | 8443 | https://localhost:8443 |
| PostgreSQL | 5432 | localhost:5432 |
| API Docs | - | http://localhost:8080/scalar/v1 |

---

## 🔧 Comandos útiles

### Ver estado de contenedores
```bash
docker-compose ps
```

### Ver logs de la API
```bash
docker-compose logs -f api
```

### Ver logs de PostgreSQL
```bash
docker-compose logs -f postgres
```

### Acceder a PostgreSQL desde CLI
```bash
docker exec -it citas-postgres psql -U postgres -d citas
```

### Detener contenedores
```bash
docker-compose down
```

### Detener y eliminar volúmenes (⚠️ elimina datos)
```bash
docker-compose down -v
```

### Reconstruir sin caché
```bash
docker-compose build --no-cache
docker-compose up -d
```

---

## 🔐 Variables de entorno

Las credenciales NO están en `docker-compose.yml`: las toma de un archivo `.env` en la raíz
del repositorio (no versionado). Créalo desde la plantilla y rellena los valores:

```bash
cp .env.example .env
docker compose up --build -d
```

Obligatorias: `POSTGRES_PASSWORD`, `Jwt__Key`, `NaturalJwt__Key` y `NaturalApi__BaseUrl` (ver README).

---

## 📊 Estructura de Docker

```
┌─────────────────┐
│   Docker Host   │
├─────────────────┤
│  citas-network  │
│  ┌───────────┐  │
│  │  API.NET  │  │
│  │ :8080     │  │
│  └─────┬─────┘  │
│        │        │
│  ┌─────▼─────┐  │
│  │ PostgreSQL│  │
│  │ :5432     │  │
│  └───────────┘  │
└─────────────────┘
```

---

## ⚠️ Troubleshooting

### Error: "Cannot connect to PostgreSQL"
```bash
# Espera a que PostgreSQL esté listo
docker-compose logs postgres

# Reinicia los servicios
docker-compose restart
```

### Error: "Port already in use"
```bash
# Cambia los puertos en docker-compose.yml
# Por ejemplo, si 8080 está ocupado:
ports:
  - "8081:8080"  # localhost:8081 -> contenedor:8080
```

### Eliminar contenedores y empezar de cero
```bash
docker-compose down -v
docker-compose build --no-cache
docker-compose up -d
```

---

## 🔄 Flujo de desarrollo

1. **Desarrollo local** (sin Docker):
   ```bash
   dotnet run
   ```

2. **Testing con Docker**:
   ```bash
   docker-compose up -d
   # Prueba la API
   docker-compose down
   ```

3. **Producción** (actualizar `docker-compose.yml` con credenciales seguras)

---

## 📝 Notas

- El archivo `docker-compose.yml` ya incluye PostgreSQL local
- Si usas Supabase en producción, cambia la `ConnectionString` en las variables de entorno
- Los datos de PostgreSQL se persisten en el volumen `postgres_data`
- La aplicación espera a que PostgreSQL esté lista antes de iniciar

¡Listo! 🚀 Tu proyecto está dockerizado.
