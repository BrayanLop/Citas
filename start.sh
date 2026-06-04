#!/bin/bash
set -e

# Compilar la solución
dotnet build "Citas/Citas.csproj" -c Release

# Ejecutar migraciones (opcional, si las tienes)
# dotnet ef database update --project Citas/Citas.csproj

# Ejecutar la aplicación
cd Citas
dotnet Citas.dll
