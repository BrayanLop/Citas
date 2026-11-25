# Despliegue de Citas en AWS Lightsail (Docker)

Mismo esquema que Natural y Brar: imagen en Docker Hub, contenedor en la red `natural-net`,
nginx (contenedor) por delante con su propio subdominio. **No toca Natural ni Brar.**

Contenedor: `citas-api` · puerto local `127.0.0.1:8083` · imagen `bray09/citas-api:<versión>`.

## 0. Antes de empezar (una sola vez)
- [ ] **BD de Citas lista** (PostgreSQL / Supabase). Si es Supabase: *Connect → Session pooler*
      del proyecto de Citas. La contraseña anterior estuvo en el historial de git: usa una BD
      nueva o cambia la contraseña.
- [ ] **DNS:** registro `A` de `citas.bia.solutions` hacia la IP de la instancia (GoDaddy).
- [ ] Haber hecho `docker login` en el PC desde donde subes la imagen.

## 1. Construir y subir la imagen (desde tu PC, en la raíz del repo `Citas`)
```bash
docker build -f Citas/Dockerfile -t bray09/citas-api:0.1.0 .
docker push bray09/citas-api:0.1.0
```

## 2. Crear el archivo de entorno en el servidor
Parte de `deploy/citas.env.example`. **Los valores reales solo viven en el servidor.**
```bash
nano ~/citas.env
chmod 600 ~/citas.env
```
- `Jwt__Key`: una clave nueva: `openssl rand -base64 48`
- `NaturalJwt__Key`: debe ser **la misma** de Natural. Cópiala de Brar sin mostrarla:
  `grep '^NaturalJwt__Key=' ~/brar.env >> ~/citas.env`
- La cadena de conexión va en formato `Host=...;Port=5432;...` (no `postgresql://`).
  El script se niega a desplegar si ve la URL o una variable vacía.

## 3. Desplegar
Copia `deploy/desplegar-citas.sh` al servidor (o clona el repo) y ejecútalo:
```bash
chmod +x desplegar-citas.sh
./desplegar-citas.sh 0.1.0
```
Descarga la imagen, deja el contenedor actual como `citas-api-prev`, arranca el nuevo y espera
`/health`. **Si el nuevo no responde, vuelve solo al anterior** y te dice la causa del error.

## 4. nginx y certificado
```bash
cd /home/ubuntu/nginx/conf.d
sed 's/brar-api/citas-api/g; s/brar\.bia\.solutions/citas.bia.solutions/g' brar.conf > citas.conf   # o parte de deploy/nginx-citas.conf.example
# Certificado: el mismo procedimiento que usaste para brar.bia.solutions, con  -d citas.bia.solutions
docker exec nginx nginx -t && docker restart nginx
```
Primero el contenedor (paso 3) y **después** la configuración de nginx.

## 5. Comprobar
```bash
curl -s https://citas.bia.solutions/health          # {"status":"ok",...}
curl -s -o /dev/null -w "%{http_code}\n" https://citas.bia.solutions/Citas   # 401 (pide token)
docker ps --format 'table {{.Names}}\t{{.Image}}\t{{.Status}}'
docker logs citas-api --since 5m 2>&1 | tail -20
```
`/scalar` y `/openapi` deben dar 404 (solo existen en Development).

## 6. Después del primer arranque
La primera vez, un administrador de Natural **activa la agenda** de su empresa
(`POST /Empresas`) y luego **sincroniza** (`POST /Sincronizacion/Natural`). Ver `README.md`.

## Actualizar a una versión nueva
Construir y subir la imagen con otra etiqueta (paso 1) y `./desplegar-citas.sh 0.2.0`.

## Volver atrás manualmente
```bash
docker rm -f citas-api && docker rename citas-api-prev citas-api && docker start citas-api
```

## Notas
- `RunMigrationsOnStartup=true` aplica las migraciones al arrancar; es lo correcto mientras haya
  **una sola copia** del contenedor. Si algún día hay varias, hazlo aparte.
- El log puede mostrar el aviso `Failed to determine the https port for redirect`: es inofensivo
  (el TLS lo hace nginx, no la API).
- También puede aparecer `libgssapi_krb5.so.2`: es el driver de PostgreSQL probando un método
  de autenticación que no usamos; es inofensivo.
