#!/usr/bin/env bash
# Despliega (o actualiza) el contenedor de Citas en el servidor, con rollback automático.
#
# Uso:  ./desplegar-citas.sh 0.1.0
#
# Qué hace, en orden:
#   1. Comprueba que ~/citas.env existe y que las variables obligatorias no están vacías
#      (sin imprimir ningún valor).
#   2. Descarga la imagen bray09/citas-api:<versión>.
#   3. Deja el contenedor actual (si lo hay) como "citas-api-prev", detenido.
#   4. Arranca el nuevo y espera a que /health responda.
#   5. Si no responde, lo elimina y vuelve a arrancar el anterior (rollback).
# No toca Natural, Brar ni nginx.
set -euo pipefail

VERSION="${1:?Indica la versión de la imagen, p. ej.: ./desplegar-citas.sh 0.1.0}"
IMAGEN="bray09/citas-api:${VERSION}"
NOMBRE="citas-api"
ANTERIOR="citas-api-prev"
ARCHIVO_ENV="${CITAS_ENV:-$HOME/citas.env}"
RED="natural-net"
PUERTO_LOCAL="8083"   # solo accesible desde el propio servidor (Natural usa 8081, Brar 8082)

OBLIGATORIAS=(ConnectionStrings__DefaultConnection Jwt__Issuer Jwt__Audience Jwt__Key NaturalJwt__Key NaturalApi__BaseUrl)

[ -f "$ARCHIVO_ENV" ] || { echo "ERROR: no existe $ARCHIVO_ENV (crea uno a partir de deploy/citas.env.example)"; exit 1; }
for v in "${OBLIGATORIAS[@]}"; do
  grep -qE "^${v}=.+" "$ARCHIVO_ENV" || { echo "ERROR: falta $v (o está vacía) en $ARCHIVO_ENV"; exit 1; }
done
grep -qE "^ConnectionStrings__DefaultConnection=Host=" "$ARCHIVO_ENV" \
  || { echo "ERROR: la cadena de conexión debe empezar por 'Host=' (formato Npgsql), no 'postgresql://'"; exit 1; }
grep -qE "^Jwt__Key=.{32,}" "$ARCHIVO_ENV" || { echo "ERROR: Jwt__Key debe tener 32 caracteres o más"; exit 1; }
docker network inspect "$RED" >/dev/null 2>&1 || { echo "ERROR: no existe la red Docker '$RED'"; exit 1; }

echo "1/4 Descargando $IMAGEN ..."
docker pull "$IMAGEN" >/dev/null 2>&1 || docker image inspect "$IMAGEN" >/dev/null 2>&1 || { echo "ERROR: no se pudo descargar $IMAGEN (¿está subida a Docker Hub?)"; exit 1; }

echo "2/4 Guardando el contenedor actual como respaldo ..."
docker rm -f "$ANTERIOR" >/dev/null 2>&1 || true
if docker inspect "$NOMBRE" >/dev/null 2>&1; then
  docker stop "$NOMBRE" >/dev/null
  docker rename "$NOMBRE" "$ANTERIOR"
  HAY_ANTERIOR=1
else
  HAY_ANTERIOR=0
fi

echo "3/4 Arrancando la versión nueva ..."
docker run -d --name "$NOMBRE" --restart unless-stopped \
  --network "$RED" \
  -p "127.0.0.1:${PUERTO_LOCAL}:8080" \
  --env-file "$ARCHIVO_ENV" \
  "$IMAGEN" >/dev/null

echo "4/4 Esperando /health ..."
OK=0
for _ in $(seq 1 30); do
  if curl -fsS -m 3 "http://127.0.0.1:${PUERTO_LOCAL}/health" >/dev/null 2>&1; then OK=1; break; fi
  # Si el contenedor ya se cayó no tiene sentido seguir esperando.
  [ "$(docker inspect -f '{{.State.Running}}' "$NOMBRE" 2>/dev/null)" = "true" ] || break
  sleep 2
done

if [ "$OK" -ne 1 ]; then
  echo "FALLÓ: el contenedor nuevo no respondió. Mensajes de error del log (no se imprimen las variables):"
  docker logs "$NOMBRE" 2>&1 | grep -E "Exception|fail:|crit:|Error" | cut -c1-200 | sort -u | head -8
  docker rm -f "$NOMBRE" >/dev/null 2>&1 || true
  if [ "$HAY_ANTERIOR" -eq 1 ]; then
    docker rename "$ANTERIOR" "$NOMBRE" && docker start "$NOMBRE" >/dev/null
    echo "ROLLBACK hecho: sigue corriendo la versión anterior."
  fi
  exit 1
fi

echo "LISTO: $IMAGEN corriendo en 127.0.0.1:${PUERTO_LOCAL}"
[ "$HAY_ANTERIOR" -eq 1 ] && echo "Respaldo: contenedor '$ANTERIOR' (detenido). Para volver: docker rm -f $NOMBRE && docker rename $ANTERIOR $NOMBRE && docker start $NOMBRE"
docker ps --filter "name=^${NOMBRE}$" --format 'table {{.Names}}\t{{.Image}}\t{{.Status}}'
