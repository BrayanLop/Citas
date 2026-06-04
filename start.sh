#!/bin/bash
set -e

# Especificar que usamos .NET 10
export DOTNET_SDK_VERSION=10.0

# Cambiar a la carpeta del proyecto
cd Citas

# Restaurar dependencias
echo "📦 Restaurando dependencias..."
dotnet restore

# Compilar
echo "🔨 Compilando proyecto..."
dotnet build -c Release -o /tmp/build

# Publicar
echo "📤 Publicando..."
dotnet publish -c Release -o /tmp/publish /p:UseAppHost=false

# Ejecutar
echo "🚀 Iniciando aplicación..."
cd /tmp/publish
dotnet Citas.dll
