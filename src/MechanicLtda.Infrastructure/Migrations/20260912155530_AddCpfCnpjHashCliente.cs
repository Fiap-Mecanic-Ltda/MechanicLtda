using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MechanicLtda.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCpfCnpjHashCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CpfCnpjHash",
                table: "Clientes",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_CpfCnpjHash",
                table: "Clientes",
                column: "CpfCnpjHash",
                unique: true,
                filter: "[CpfCnpjHash] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clientes_CpfCnpjHash",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "CpfCnpjHash",
                table: "Clientes");
        }
    }
}
