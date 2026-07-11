using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MechanicLtda.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdemServicoAprovacaoToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrdemServicoAprovacaoTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrdemServicoId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataExpiracao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Utilizado = table.Column<bool>(type: "bit", nullable: false),
                    DataUtilizacao = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdemServicoAprovacaoTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdemServicoAprovacaoTokens_OrdensServico_OrdemServicoId",
                        column: x => x.OrdemServicoId,
                        principalTable: "OrdensServico",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrdemServicoAprovacaoTokens_OrdemServicoId",
                table: "OrdemServicoAprovacaoTokens",
                column: "OrdemServicoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdemServicoAprovacaoTokens_Token",
                table: "OrdemServicoAprovacaoTokens",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrdemServicoAprovacaoTokens");
        }
    }
}
