using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MechanicLtda.Infrastructure.Migrations
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddEstoque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Estoques",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    QuantidadeAtual = table.Column<int>(type: "int", nullable: false),
                    QuantidadeMinima = table.Column<int>(type: "int", nullable: false),
                    DataUltimaAtualizacao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estoques", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItensOrdemServico_EstoqueId",
                table: "ItensOrdemServico",
                column: "EstoqueId");

            migrationBuilder.AddForeignKey(
                name: "FK_ItensOrdemServico_Estoques_EstoqueId",
                table: "ItensOrdemServico",
                column: "EstoqueId",
                principalTable: "Estoques",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItensOrdemServico_Estoques_EstoqueId",
                table: "ItensOrdemServico");

            migrationBuilder.DropTable(
                name: "Estoques");

            migrationBuilder.DropIndex(
                name: "IX_ItensOrdemServico_EstoqueId",
                table: "ItensOrdemServico");
        }
    }
}
