#!/bin/bash
set -e

echo "🚀 Iniciando Citas API..."

# Cambiar a la carpeta del proyecto
cd Citas

# Ejecutar la aplicación compilada
# Railway ya compiló el proyecto, solo ejecutamos
dotnet Citas.dll
