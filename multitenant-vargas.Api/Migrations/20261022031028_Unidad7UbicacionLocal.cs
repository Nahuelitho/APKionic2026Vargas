using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace multitenant_vargas.Api.Migrations
{
    /// <inheritdoc />
    public partial class Unidad7UbicacionLocal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "direccion_linea",
                table: "empresas",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "latitud",
                table: "empresas",
                type: "decimal(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "longitud",
                table: "empresas",
                type: "decimal(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "referencia",
                table: "empresas",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "empresas",
                keyColumn: "id",
                keyValue: 1L,
                columns: new[] { "direccion_linea", "latitud", "longitud", "referencia" },
                values: new object[] { null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "direccion_linea",
                table: "empresas");

            migrationBuilder.DropColumn(
                name: "latitud",
                table: "empresas");

            migrationBuilder.DropColumn(
                name: "longitud",
                table: "empresas");

            migrationBuilder.DropColumn(
                name: "referencia",
                table: "empresas");
        }
    }
}
