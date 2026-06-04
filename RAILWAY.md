# Configuración para Railway

## 📋 Variables de entorno necesarias

En tu dashboard de Railway, agrega estas variables:

### Base de datos
```
ConnectionStrings__DefaultConnection=Host=db.khgfvmtbeqzqbsdebafq.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=e2GLOUYELM5kveUf
```

### JWT
```
Jwt__Issuer=CitasApi
Jwt__Audience=CitasApiClients
Jwt__Key=aC3xY9zK2mP5qL7wM0oR4sT6uV8xY9zK2mP5qL7wN0oR=
```

### ASP.NET Core
```
ASPNETCORE_ENVIRONMENT=Production
```

## 🔧 Pasos en Railway:

1. Ve a tu proyecto en Railway
2. Selecciona la pestaña **Variables**
3. Haz clic en **+ Add Variable**
4. Copia y pega cada variable de arriba
5. Haz clic en **Deploy**

## ⚠️ Nota sobre credenciales

En producción, usa variables de entorno seguras. NUNCA commits credenciales a Git.

Para local development, usa:
```sh
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."
dotnet user-secrets set "Jwt:Key" "..."
```

## 🚀 Después de configurar:

Railway automáticamente detectará `.NET` y compilará el proyecto.
