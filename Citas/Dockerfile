# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar archivo del proyecto
COPY ["Citas/Citas.csproj", "Citas/"]

# Restaurar dependencias
RUN dotnet restore "Citas/Citas.csproj"

# Copiar todo el código
COPY . .

# Compilar la aplicación
RUN dotnet build "Citas/Citas.csproj" -c Release -o /app/build

# Etapa 2: Publish
FROM build AS publish
RUN dotnet publish "Citas/Citas.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Copiar la aplicación publicada
COPY --from=publish /app/publish .

# Exponer puerto
EXPOSE 8080
EXPOSE 8443

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/openapi/v1.json || exit 1

# Comando para ejecutar
ENTRYPOINT ["dotnet", "Citas.dll"]
