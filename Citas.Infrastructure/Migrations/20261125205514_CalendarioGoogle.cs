using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Citas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CalendarioGoogle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CuentasGoogle",
                columns: table => new
                {
                    IdCuenta = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdEmpleado = table.Column<int>(type: "integer", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    RefreshTokenCifrado = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CalendarioId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SyncToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UltimaSincronizacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CambioRemotoPendiente = table.Column<bool>(type: "boolean", nullable: false),
                    Intentos = table.Column<int>(type: "integer", nullable: false),
                    ProximoIntentoUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UltimoError = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CanalId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CanalExpiraUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConectadaUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuentasGoogle", x => x.IdCuenta);
                    table.ForeignKey(
                        name: "FK_CuentasGoogle_Usuarios_IdEmpleado",
                        column: x => x.IdEmpleado,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoogleBloqueos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdBloqueo = table.Column<int>(type: "integer", nullable: false),
                    IdEmpleado = table.Column<int>(type: "integer", nullable: false),
                    GoogleEventId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleBloqueos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoogleBloqueos_BloqueosAgenda_IdBloqueo",
                        column: x => x.IdBloqueo,
                        principalTable: "BloqueosAgenda",
                        principalColumn: "IdBloqueo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoogleEventosCita",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdCita = table.Column<int>(type: "integer", nullable: false),
                    IdEmpleado = table.Column<int>(type: "integer", nullable: false),
                    GoogleEventId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Huella = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleEventosCita", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CuentasGoogle_CanalId",
                table: "CuentasGoogle",
                column: "CanalId",
                filter: "\"CanalId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CuentasGoogle_IdEmpleado",
                table: "CuentasGoogle",
                column: "IdEmpleado");

            migrationBuilder.CreateIndex(
                name: "IX_CuentasGoogle_TenantId",
                table: "CuentasGoogle",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CuentasGoogle_TenantId_IdEmpleado",
                table: "CuentasGoogle",
                columns: new[] { "TenantId", "IdEmpleado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoogleBloqueos_IdBloqueo",
                table: "GoogleBloqueos",
                column: "IdBloqueo");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleBloqueos_TenantId",
                table: "GoogleBloqueos",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleBloqueos_TenantId_IdEmpleado_GoogleEventId",
                table: "GoogleBloqueos",
                columns: new[] { "TenantId", "IdEmpleado", "GoogleEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoogleEventosCita_TenantId",
                table: "GoogleEventosCita",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_GoogleEventosCita_TenantId_IdCita",
                table: "GoogleEventosCita",
                columns: new[] { "TenantId", "IdCita" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoogleEventosCita_TenantId_IdEmpleado",
                table: "GoogleEventosCita",
                columns: new[] { "TenantId", "IdEmpleado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CuentasGoogle");

            migrationBuilder.DropTable(
                name: "GoogleBloqueos");

            migrationBuilder.DropTable(
                name: "GoogleEventosCita");
        }
    }
}
