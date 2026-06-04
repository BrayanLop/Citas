#!/bin/bash
set -e

echo "Building Citas API..."
cd Citas
dotnet restore
dotnet build -c Release
dotnet publish -c Release -o out

echo "Starting Citas API..."
cd out
dotnet Citas.dll
