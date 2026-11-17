using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace multitenant_vargas.Api.Migrations
{
    /// <inheritdoc />
    public partial class SuperadminSeed : Migration
    {
        private const string Hash = "wmucZlygLRQkEjd2avm1Hg==.KzdxfKP9IcqlweQErG1WHvK4U32Zy2PjxEmW5WrNo58=";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotente: si la cuenta ya se creo a mano en el hosting, no falla.
            migrationBuilder.Sql($"""
                INSERT INTO usuarios (id, activo, email, nombre, password_hash, rol_id)
                SELECT 9, 1, 'superadmin@vargas.com', 'Superadmin', '{Hash}', 4
                WHERE NOT EXISTS (SELECT 1 FROM usuarios WHERE email = 'superadmin@vargas.com');
                """);

            migrationBuilder.Sql("""
                INSERT INTO usuario_roles (usuario_id, rol_id, empresa_id)
                SELECT u.id, 4, NULL
                FROM usuarios u
                WHERE u.email = 'superadmin@vargas.com'
                  AND NOT EXISTS (
                      SELECT 1 FROM usuario_roles ur
                      WHERE ur.usuario_id = u.id AND ur.rol_id = 4 AND ur.empresa_id IS NULL);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE ur FROM usuario_roles ur
                INNER JOIN usuarios u ON u.id = ur.usuario_id
                WHERE u.email = 'superadmin@vargas.com' AND ur.rol_id = 4 AND ur.empresa_id IS NULL;
                """);

            migrationBuilder.Sql("DELETE FROM usuarios WHERE email = 'superadmin@vargas.com' AND id = 9;");
        }
    }
}
