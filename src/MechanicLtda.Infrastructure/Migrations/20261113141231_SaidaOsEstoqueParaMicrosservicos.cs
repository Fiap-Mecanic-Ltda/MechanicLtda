using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MechanicLtda.Infrastructure.Migrations
{
    /// <summary>
    /// OS, itens, orçamento, tokens de aprovação, estoque e serviços da oficina saíram deste
    /// repositório (OrdemServico-service e ExecucaoProducao-service). As tabelas ficam no banco
    /// como histórico, sem perda de dados: nada aqui as apaga. Só caem as chaves estrangeiras
    /// de OrdensServico para Clientes e Veiculos, que travariam a exclusão de um cliente ou
    /// veículo por causa de uma OS antiga que este código nem lê mais.
    /// </summary>
    public partial class SaidaOsEstoqueParaMicrosservicos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrdensServico_Clientes_ClienteId",
                table: "OrdensServico");

            migrationBuilder.DropForeignKey(
                name: "FK_OrdensServico_Veiculos_VeiculoId",
                table: "OrdensServico");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_OrdensServico_Clientes_ClienteId",
                table: "OrdensServico",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrdensServico_Veiculos_VeiculoId",
                table: "OrdensServico",
                column: "VeiculoId",
                principalTable: "Veiculos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
