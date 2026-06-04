@echo off
REM Script para construir y ejecutar Docker en Windows

echo.
echo 🐳 Construyendo imagen Docker...
docker-compose build

echo.
echo 🚀 Iniciando contenedores...
docker-compose up -d

echo.
echo ⏳ Esperando que la base de datos esté lista...
timeout /t 10

echo.
echo 📊 Estado de los contenedores:
docker-compose ps

echo.
echo ✅ API disponible en: http://localhost:8080
echo 📖 Documentación en: http://localhost:8080/scalar/v1
echo 💾 PostgreSQL en: localhost:5432

echo.
echo Para ver los logs: docker-compose logs -f api
echo Para detener: docker-compose down
echo.
pause
