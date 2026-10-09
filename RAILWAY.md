# Configuración para Railway

## Variables de entorno necesarias

En el dashboard de Railway (pestaña **Variables**) define estas variables. Los valores
reales NUNCA se escriben en el repositorio; aquí solo van los nombres.

### Base de datos
```
ConnectionStrings__DefaultConnection=Host=<host>;Port=5432;Database=<bd>;Username=<usuario>;Password=<password>
```

### JWT de clientes (emitido por Citas)
```
Jwt__Issuer=CitasApi
Jwt__Audience=CitasApiClients
Jwt__Key=<clave aleatoria de al menos 32 caracteres>
```

### Natural (JWT del personal y API para sincronizar)
```
NaturalJwt__Key=<la misma clave con la que Natural firma sus tokens>
NaturalApi__BaseUrl=https://naturalback.vip/v2/api
```

### ASP.NET Core / CORS
```
ASPNETCORE_ENVIRONMENT=Production
Cors__AllowedOrigins__0=https://<frontend>
```

La API no arranca si falta alguna variable obligatoria (indica cuál en el error).
Ver también `.env.example` y el `README.md`.

## Desarrollo local

```sh
dotnet user-secrets --project Citas set "ConnectionStrings:DefaultConnection" "..."
dotnet user-secrets --project Citas set "Jwt:Key" "..."
dotnet user-secrets --project Citas set "NaturalJwt:Key" "..."
```
