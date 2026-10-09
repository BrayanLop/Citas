using Citas.Application;
using Citas.Autenticacion;
using Citas.Autorizacion;
using Citas.Configuracion;
using Citas.Data;
using Citas.Infrastructure;
using Citas.Middleware;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Falla al arrancar (con el nombre de la variable) si falta configuración obligatoria:
// cadena de conexión, JWT de clientes de Citas, clave JWT de Natural y URL de la API de Natural.
ConfiguracionRequerida.Validar(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
    options.AddDocumentTransformer<Citas.OpenApi.BearerSecuritySchemeTransformer>());

// Capas de la arquitectura (DDD): aplicación e infraestructura.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Autenticación dual (token de Natural para el personal, token de Citas para clientes)
// y políticas de autorización centralizadas.
builder.Services.AddAutenticacionCitas(builder.Configuration);
builder.Services.AddPoliticasCitas();

// CORS: orígenes en Cors:AllowedOrigins (Cors__AllowedOrigins__0, __1...). Si no hay
// ninguno se permite cualquier origen, pero sin credenciales (cookies).
const string PoliticaCors = "Citas";
var origenes = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()?
    .Where(o => !string.IsNullOrWhiteSpace(o))
    .ToArray() ?? [];
builder.Services.AddCors(options => options.AddPolicy(PoliticaCors, policy =>
{
    if (origenes.Length > 0)
        policy.WithOrigins(origenes).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    else
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
}));

// Traduce las AppException (400/401/403/404/409) a ProblemDetails.
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// En Docker arrancamos contra una base vacía, así que aplicamos las migraciones al
// iniciar (se activa con RunMigrationsOnStartup=true, que pone el docker-compose).
if (app.Configuration.GetValue<bool>("RunMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<CitasContext>().Database.Migrate();
}

app.UseExceptionHandler();

// Documentación (OpenAPI + Scalar en /scalar/v1) solo en desarrollo.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseCors(PoliticaCors);

app.UseAuthentication();
// Tras autenticar: alta automática del personal de Natural como Usuario de Citas.
app.UseMiddleware<AltaPersonalMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
