using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalFlowScheduler.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed das Roles essenciais
            migrationBuilder.InsertData(
                table: "AspNetRoles", // Nome da tabela de roles do Identity
                columns: new[] { "Id", "Name", "NormalizedName", "ConcurrencyStamp" }, // Colunas a serem inseridas
                values: new object[,]
                {
                    { 1, "Admin", "ADMIN", Guid.NewGuid().ToString() }, // ID 1 para Admin
                    { 2, "Owner", "OWNER", Guid.NewGuid().ToString() }, // ID 3 para Owner
                    { 3, "Developer", "DEVELOPER", Guid.NewGuid().ToString() }, // ID 4 para Developer
                    { 4, "User", "USER", Guid.NewGuid().ToString() }  // ID 5 para User (role padrão)
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove as roles no método Down (para rollback da migration)
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValues: [1, 2, 3, 4]
            );
        }
    }
}
