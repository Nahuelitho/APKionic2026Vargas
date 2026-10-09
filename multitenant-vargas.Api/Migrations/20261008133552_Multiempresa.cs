using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace multitenant_vargas.Api.Migrations
{
    /// <inheritdoc />
    public partial class Multiempresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_pedido_items_pedidos_pedido_id",
                table: "pedido_items");

            migrationBuilder.DropForeignKey(
                name: "fk_pedido_items_productos_producto_id",
                table: "pedido_items");

            migrationBuilder.DropIndex(
                name: "ix_pedido_items_pedido_id",
                table: "pedido_items");

            migrationBuilder.DropIndex(
                name: "ix_pedido_items_producto_id",
                table: "pedido_items");

            migrationBuilder.AddColumn<long>(
                name: "empresa_id",
                table: "refresh_tokens",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rol_sesion",
                table: "refresh_tokens",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "version_ambito",
                table: "refresh_tokens",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "empresa_id",
                table: "productos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "empresa_id",
                table: "pedidos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "usuario_id",
                table: "pedidos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "empresa_id",
                table: "pedido_items",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_productos_id_empresa_id",
                table: "productos",
                columns: new[] { "id", "empresa_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_pedidos_id_empresa_id",
                table: "pedidos",
                columns: new[] { "id", "empresa_id" });

            migrationBuilder.CreateTable(
                name: "empresas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    nombre_empresa = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    activo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_empresas", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "usuario_roles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    rol_id = table.Column<long>(type: "bigint", nullable: false),
                    empresa_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuario_roles", x => x.id);
                    table.CheckConstraint("ck_usuario_roles_ambito", "(rol_id = 4 AND empresa_id IS NULL) OR (rol_id IN (1, 2, 3) AND empresa_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_usuario_roles_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_usuario_roles_roles_rol_id",
                        column: x => x.rol_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_usuario_roles_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "empresas",
                columns: new[] { "id", "activo", "nombre_empresa" },
                values: new object[] { 1L, true, "Vargas" });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "activo", "codigo", "nombre" },
                values: new object[] { 4L, true, "superadmin", "Superadmin" });

            // Existing orders have no trustworthy creator. Never expose them as a customer's orders.
            migrationBuilder.Sql("""
                UPDATE productos SET empresa_id = 1;
                UPDATE pedidos SET empresa_id = 1;
                UPDATE pedido_items SET empresa_id = 1;
                INSERT INTO usuarios (nombre, email, password_hash, activo, rol_id)
                SELECT 'Propietario de pedidos historicos', CONCAT('historial-', UUID(), '@vargas.invalid'), 'NO_LOGIN', 0, NULL
                WHERE EXISTS (SELECT 1 FROM pedidos);
                UPDATE pedidos SET usuario_id = LAST_INSERT_ID() WHERE usuario_id = 0;
                INSERT INTO usuario_roles (usuario_id, rol_id, empresa_id)
                SELECT id, rol_id, 1 FROM usuarios WHERE rol_id IN (1, 2, 3);
                UPDATE refresh_tokens SET revocado_en = UTC_TIMESTAMP(6) WHERE revocado_en IS NULL;
                """);

            migrationBuilder.AlterColumn<long>(name: "empresa_id", table: "productos", type: "bigint", nullable: false,
                oldClrType: typeof(long), oldType: "bigint", oldDefaultValue: 0L);
            migrationBuilder.AlterColumn<long>(name: "empresa_id", table: "pedidos", type: "bigint", nullable: false,
                oldClrType: typeof(long), oldType: "bigint", oldDefaultValue: 0L);
            migrationBuilder.AlterColumn<long>(name: "usuario_id", table: "pedidos", type: "bigint", nullable: false,
                oldClrType: typeof(long), oldType: "bigint", oldDefaultValue: 0L);
            migrationBuilder.AlterColumn<long>(name: "empresa_id", table: "pedido_items", type: "bigint", nullable: false,
                oldClrType: typeof(long), oldType: "bigint", oldDefaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "ix_productos_empresa_id",
                table: "productos",
                column: "empresa_id");

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_empresa_id",
                table: "pedidos",
                column: "empresa_id");

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_usuario_id",
                table: "pedidos",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_pedido_items_pedido_id_empresa_id",
                table: "pedido_items",
                columns: new[] { "pedido_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "ix_pedido_items_producto_id_empresa_id",
                table: "pedido_items",
                columns: new[] { "producto_id", "empresa_id" });

            migrationBuilder.CreateIndex(
                name: "ix_usuario_roles_empresa_id",
                table: "usuario_roles",
                column: "empresa_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuario_roles_rol_id",
                table: "usuario_roles",
                column: "rol_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuario_roles_usuario_id_empresa_id",
                table: "usuario_roles",
                columns: new[] { "usuario_id", "empresa_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_pedido_items_pedidos_pedido_id_empresa_id",
                table: "pedido_items",
                columns: new[] { "pedido_id", "empresa_id" },
                principalTable: "pedidos",
                principalColumns: new[] { "id", "empresa_id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_pedido_items_productos_producto_id_empresa_id",
                table: "pedido_items",
                columns: new[] { "producto_id", "empresa_id" },
                principalTable: "productos",
                principalColumns: new[] { "id", "empresa_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pedidos_empresas_empresa_id",
                table: "pedidos",
                column: "empresa_id",
                principalTable: "empresas",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pedidos_usuarios_usuario_id",
                table: "pedidos",
                column: "usuario_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_productos_empresas_empresa_id",
                table: "productos",
                column: "empresa_id",
                principalTable: "empresas",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_pedido_items_pedidos_pedido_id_empresa_id",
                table: "pedido_items");

            migrationBuilder.DropForeignKey(
                name: "fk_pedido_items_productos_producto_id_empresa_id",
                table: "pedido_items");

            migrationBuilder.DropForeignKey(
                name: "fk_pedidos_empresas_empresa_id",
                table: "pedidos");

            migrationBuilder.DropForeignKey(
                name: "fk_pedidos_usuarios_usuario_id",
                table: "pedidos");

            migrationBuilder.DropForeignKey(
                name: "fk_productos_empresas_empresa_id",
                table: "productos");

            migrationBuilder.DropTable(
                name: "usuario_roles");

            migrationBuilder.DropTable(
                name: "empresas");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_productos_id_empresa_id",
                table: "productos");

            migrationBuilder.DropIndex(
                name: "ix_productos_empresa_id",
                table: "productos");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_pedidos_id_empresa_id",
                table: "pedidos");

            migrationBuilder.DropIndex(
                name: "ix_pedidos_empresa_id",
                table: "pedidos");

            migrationBuilder.DropIndex(
                name: "ix_pedidos_usuario_id",
                table: "pedidos");

            migrationBuilder.DropIndex(
                name: "ix_pedido_items_pedido_id_empresa_id",
                table: "pedido_items");

            migrationBuilder.DropIndex(
                name: "ix_pedido_items_producto_id_empresa_id",
                table: "pedido_items");

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: 4L);

            migrationBuilder.DropColumn(
                name: "empresa_id",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "rol_sesion",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "version_ambito",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "empresa_id",
                table: "productos");

            migrationBuilder.DropColumn(
                name: "empresa_id",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "usuario_id",
                table: "pedidos");

            migrationBuilder.DropColumn(
                name: "empresa_id",
                table: "pedido_items");

            migrationBuilder.CreateIndex(
                name: "ix_pedido_items_pedido_id",
                table: "pedido_items",
                column: "pedido_id");

            migrationBuilder.CreateIndex(
                name: "ix_pedido_items_producto_id",
                table: "pedido_items",
                column: "producto_id");

            migrationBuilder.AddForeignKey(
                name: "fk_pedido_items_pedidos_pedido_id",
                table: "pedido_items",
                column: "pedido_id",
                principalTable: "pedidos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_pedido_items_productos_producto_id",
                table: "pedido_items",
                column: "producto_id",
                principalTable: "productos",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
