using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Citas.Migrations
{
    /// <inheritdoc />
    public partial class AddMultitenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Usuarios",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "DEFAULT");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Servicios",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "DEFAULT");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "RolUsuarios",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "DEFAULT");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Roles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "DEFAULT");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "DetalleCitas",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "DEFAULT");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Citas",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "DEFAULT");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "AgendaEmpleados",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "DEFAULT");

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

            migrationBuilder.InsertData(
                table: "Empresas",
                columns: new[] { "Id", "Activo", "Nombre" },
                values: new object[] { "DEFAULT", true, "Empresa por defecto" });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_TenantId",
                table: "Usuarios",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Servicios_TenantId",
                table: "Servicios",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RolUsuarios_TenantId",
                table: "RolUsuarios",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_TenantId",
                table: "Roles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleCitas_TenantId",
                table: "DetalleCitas",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Citas_TenantId",
                table: "Citas",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AgendaEmpleados_TenantId",
                table: "AgendaEmpleados",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Empresas");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_TenantId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Servicios_TenantId",
                table: "Servicios");

            migrationBuilder.DropIndex(
                name: "IX_RolUsuarios_TenantId",
                table: "RolUsuarios");

            migrationBuilder.DropIndex(
                name: "IX_Roles_TenantId",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_DetalleCitas_TenantId",
                table: "DetalleCitas");

            migrationBuilder.DropIndex(
                name: "IX_Citas_TenantId",
                table: "Citas");

            migrationBuilder.DropIndex(
                name: "IX_AgendaEmpleados_TenantId",
                table: "AgendaEmpleados");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Servicios");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "RolUsuarios");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "DetalleCitas");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AgendaEmpleados");
        }
    }
}
