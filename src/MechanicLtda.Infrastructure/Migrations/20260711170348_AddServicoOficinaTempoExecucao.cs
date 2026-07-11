using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MechanicLtda.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServicoOficinaTempoExecucao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataFimExecucao",
                table: "OrdensServico",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataInicioExecucao",
                table: "OrdensServico",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescricaoServico",
                table: "ItensOrdemServico",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServicoOficinaId",
                table: "ItensOrdemServico",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServicosOficina",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ValorBase = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicosOficina", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItensOrdemServico_ServicoOficinaId",
                table: "ItensOrdemServico",
                column: "ServicoOficinaId");

            migrationBuilder.AddForeignKey(
                name: "FK_ItensOrdemServico_ServicosOficina_ServicoOficinaId",
                table: "ItensOrdemServico",
                column: "ServicoOficinaId",
                principalTable: "ServicosOficina",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItensOrdemServico_ServicosOficina_ServicoOficinaId",
                table: "ItensOrdemServico");

            migrationBuilder.DropTable(
                name: "ServicosOficina");

            migrationBuilder.DropIndex(
                name: "IX_ItensOrdemServico_ServicoOficinaId",
                table: "ItensOrdemServico");

            migrationBuilder.DropColumn(
                name: "DataFimExecucao",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "DataInicioExecucao",
                table: "OrdensServico");

            migrationBuilder.DropColumn(
                name: "DescricaoServico",
                table: "ItensOrdemServico");

            migrationBuilder.DropColumn(
                name: "ServicoOficinaId",
                table: "ItensOrdemServico");
        }
    }
}
