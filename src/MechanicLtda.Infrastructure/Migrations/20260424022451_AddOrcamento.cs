using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MechanicLtda.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrcamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orcamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrdemServicoId = table.Column<int>(type: "int", nullable: false),
                    ValorTotalPecas = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorTotalInsumos = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorTotalGeral = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DataGeracao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Validade = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orcamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orcamentos_OrdensServico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "OrdensServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orcamentos_OrdemServicoId",
                table: "Orcamentos",
                column: "OrdemServicoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Orcamentos");
        }
    }
}
