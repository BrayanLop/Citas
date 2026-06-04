#!/bin/bash

# Script para construir y ejecutar Docker

echo "🐳 Construyendo imagen Docker..."
docker-compose build

echo "🚀 Iniciando contenedores..."
docker-compose up -d

echo "⏳ Esperando que la base de datos esté lista..."
sleep 10

echo "📊 Estado de los contenedores:"
docker-compose ps

echo "✅ API disponible en: http://localhost:8080"
echo "📖 Documentación en: http://localhost:8080/scalar/v1"
echo "💾 PostgreSQL en: localhost:5432"

echo ""
echo "Para ver los logs: docker-compose logs -f api"
echo "Para detener: docker-compose down"
