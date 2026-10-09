using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Citas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Personas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Apellido = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Cedula = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Residencia = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Edad = table.Column<int>(type: "integer", nullable: true),
                    Celular = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    FechaNacimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClaveHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreRol = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Servicios",
                columns: table => new
                {
                    IdServicio = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServicioNaturalId = table.Column<int>(type: "integer", nullable: false),
                    NombreServicio = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TiempoEstimado = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    DisponibleEnNatural = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servicios", x => x.IdServicio);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreUsuario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ClaveHash = table.Column<string>(type: "text", nullable: true),
                    Estado = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    PersonaId = table.Column<int>(type: "integer", nullable: true),
                    PersonaNaturalId = table.Column<int>(type: "integer", nullable: true),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Celular = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuarios_Personas_PersonaId",
                        column: x => x.PersonaId,
                        principalTable: "Personas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AgendaEmpleados",
                columns: table => new
                {
                    IdHorario = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdUsuario = table.Column<int>(type: "integer", nullable: false),
                    FechaReservada = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HoraEstimadaCita = table.Column<TimeSpan>(type: "interval", nullable: false),
                    HoraEstimadaFin = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EstadoHorario = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgendaEmpleados", x => x.IdHorario);
                    table.ForeignKey(
                        name: "FK_AgendaEmpleados_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Citas",
                columns: table => new
                {
                    IdCita = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdCliente = table.Column<int>(type: "integer", nullable: false),
                    IdEmpleado = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FechaCita = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HoraEstimadaCita = table.Column<TimeSpan>(type: "interval", nullable: false),
                    HoraEstimadaFin = table.Column<TimeSpan>(type: "interval", nullable: true),
                    Observaciones = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Citas", x => x.IdCita);
                    table.ForeignKey(
                        name: "FK_Citas_Usuarios_IdCliente",
                        column: x => x.IdCliente,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Citas_Usuarios_IdEmpleado",
                        column: x => x.IdEmpleado,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolUsuarios",
                columns: table => new
                {
                    IdRolUsuario = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdUsuario = table.Column<int>(type: "integer", nullable: false),
                    IdRol = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolUsuarios", x => x.IdRolUsuario);
                    table.ForeignKey(
                        name: "FK_RolUsuarios_Roles_IdRol",
                        column: x => x.IdRol,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolUsuarios_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DetalleCitas",
                columns: table => new
                {
                    IdDetalle = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdCita = table.Column<int>(type: "integer", nullable: false),
                    IdServicio = table.Column<int>(type: "integer", nullable: false),
                    ValorServicio = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TiempoEstimado = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetalleCitas", x => x.IdDetalle);
                    table.ForeignKey(
                        name: "FK_DetalleCitas_Citas_IdCita",
                        column: x => x.IdCita,
                        principalTable: "Citas",
                        principalColumn: "IdCita",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetalleCitas_Servicios_IdServicio",
                        column: x => x.IdServicio,
                        principalTable: "Servicios",
                        principalColumn: "IdServicio",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgendaEmpleados_IdUsuario",
                table: "AgendaEmpleados",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_AgendaEmpleados_TenantId",
                table: "AgendaEmpleados",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Citas_IdCliente",
                table: "Citas",
                column: "IdCliente");

            migrationBuilder.CreateIndex(
                name: "IX_Citas_IdEmpleado",
                table: "Citas",
                column: "IdEmpleado");

            migrationBuilder.CreateIndex(
                name: "IX_Citas_TenantId",
                table: "Citas",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleCitas_IdCita",
                table: "DetalleCitas",
                column: "IdCita");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleCitas_IdServicio",
                table: "DetalleCitas",
                column: "IdServicio");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleCitas_TenantId",
                table: "DetalleCitas",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Personas_Email",
                table: "Personas",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_TenantId",
                table: "Roles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RolUsuarios_IdRol",
                table: "RolUsuarios",
                column: "IdRol");

            migrationBuilder.CreateIndex(
                name: "IX_RolUsuarios_IdUsuario",
                table: "RolUsuarios",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_RolUsuarios_TenantId",
                table: "RolUsuarios",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Servicios_TenantId",
                table: "Servicios",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Servicios_TenantId_ServicioNaturalId",
                table: "Servicios",
                columns: new[] { "TenantId", "ServicioNaturalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_PersonaId",
                table: "Usuarios",
                column: "PersonaId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_TenantId",
                table: "Usuarios",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_TenantId_PersonaId",
                table: "Usuarios",
                columns: new[] { "TenantId", "PersonaId" },
                unique: true,
                filter: "\"PersonaId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_TenantId_PersonaNaturalId",
                table: "Usuarios",
                columns: new[] { "TenantId", "PersonaNaturalId" },
                unique: true,
                filter: "\"PersonaNaturalId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgendaEmpleados");

            migrationBuilder.DropTable(
                name: "DetalleCitas");

            migrationBuilder.DropTable(
                name: "Empresas");

            migrationBuilder.DropTable(
                name: "RolUsuarios");

            migrationBuilder.DropTable(
                name: "Citas");

            migrationBuilder.DropTable(
                name: "Servicios");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Personas");
        }
    }
}
