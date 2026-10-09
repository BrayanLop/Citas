# Construir desde la raíz del repositorio:
#   docker build -f Citas/Dockerfile -t citas-api .

# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar los proyectos de la solución (la API referencia Application, Domain e Infrastructure)
# para restaurar dependencias en una capa cacheable.
COPY ["global.json", "./"]
COPY ["Citas/Citas.csproj", "Citas/"]
COPY ["Citas.Application/Citas.Application.csproj", "Citas.Application/"]
COPY ["Citas.Domain/Citas.Domain.csproj", "Citas.Domain/"]
COPY ["Citas.Infrastructure/Citas.Infrastructure.csproj", "Citas.Infrastructure/"]

# Restaurar dependencias
RUN dotnet restore "Citas/Citas.csproj"

# Copiar todo el código
COPY . .

# Etapa 2: Publish
FROM build AS publish
RUN dotnet publish "Citas/Citas.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Etapa 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Copiar la aplicación publicada
COPY --from=publish /app/publish .

# La API escucha en 8080 dentro del contenedor; se puede sobrescribir con ASPNETCORE_URLS.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Usuario sin privilegios que trae la imagen oficial de .NET
USER $APP_UID

ENTRYPOINT ["dotnet", "Citas.dll"]
