using System.Text;
using Citas.Data;
using Citas.Multitenancy;
using Citas.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
    options.AddDocumentTransformer<Citas.OpenApi.BearerSecuritySchemeTransformer>());

// Add Entity Framework Core with PostgreSQL
builder.Services.AddDbContext<CitasContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Capa de abstracción de acceso a datos (patrón Repository genérico)
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Multitenancy: el tenant se resuelve desde el claim 'tenant' del JWT.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, JwtTenantProvider>();

// Autenticación JWT
var jwt = builder.Configuration.GetSection("Jwt");
var jwtKey = jwt["Key"] ?? throw new InvalidOperationException("Falta configurar Jwt:Key.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // UI interactiva en /scalar/v1
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
