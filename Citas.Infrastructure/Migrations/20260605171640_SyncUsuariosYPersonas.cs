using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Citas.Migrations
{
    /// <inheritdoc />
    public partial class SyncUsuariosYPersonas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Estos cambios (columnas ClaveHash/Estado/PersonaId en Usuarios, tabla personas
            // y su FK) ya existian en la base de datos porque se aplicaron manualmente.
            // Se deja el Up() vacio a proposito: esta migracion solo sincroniza el modelo y
            // el snapshot de EF, registrandose en __EFMigrationsHistory sin re-ejecutar el SQL.
            //
            // Nota: el indice IX_Usuarios_PersonaId podria no existir en la BD. Si lo quieres
            // crear, descomenta lo siguiente (es seguro, solo agrega un indice de apoyo a la FK):
            // migrationBuilder.CreateIndex(
            //     name: "IX_Usuarios_PersonaId",
            //     table: "Usuarios",
            //     column: "PersonaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_personas_PersonaId",
                table: "Usuarios");

            migrationBuilder.DropTable(
                name: "personas");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_PersonaId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ClaveHash",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "PersonaId",
                table: "Usuarios");
        }
    }
}
