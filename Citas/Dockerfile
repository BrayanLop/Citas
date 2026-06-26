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

# La API escucha en 8080 dentro del contenedor (lo fija ASPNETCORE_URLS en el compose)
EXPOSE 8080

ENTRYPOINT ["dotnet", "Citas.dll"]
