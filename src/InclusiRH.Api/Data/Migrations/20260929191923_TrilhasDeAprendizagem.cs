using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InclusiRH.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TrilhasDeAprendizagem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConclusoesTreinamento");

            migrationBuilder.AddColumn<decimal>(
                name: "CargaHoraria",
                table: "Treinamentos",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Conteudo",
                table: "Treinamentos",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Descricao",
                table: "Treinamentos",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Modulo",
                table: "Treinamentos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Obrigatorio",
                table: "Treinamentos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ProgressosTreinamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TreinamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ColaboradorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataInicio = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DataConclusao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressosTreinamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgressosTreinamento_Colaboradores_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProgressosTreinamento_Treinamentos_TreinamentoId",
                        column: x => x.TreinamentoId,
                        principalTable: "Treinamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProgressosTreinamento_ColaboradorId",
                table: "ProgressosTreinamento",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressosTreinamento_TreinamentoId_ColaboradorId",
                table: "ProgressosTreinamento",
                columns: new[] { "TreinamentoId", "ColaboradorId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProgressosTreinamento");

            migrationBuilder.DropColumn(
                name: "CargaHoraria",
                table: "Treinamentos");

            migrationBuilder.DropColumn(
                name: "Conteudo",
                table: "Treinamentos");

            migrationBuilder.DropColumn(
                name: "Descricao",
                table: "Treinamentos");

            migrationBuilder.DropColumn(
                name: "Modulo",
                table: "Treinamentos");

            migrationBuilder.DropColumn(
                name: "Obrigatorio",
                table: "Treinamentos");

            migrationBuilder.CreateTable(
                name: "ConclusoesTreinamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ColaboradorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TreinamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataAtualizacao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DataConclusao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConclusoesTreinamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConclusoesTreinamento_Colaboradores_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConclusoesTreinamento_Treinamentos_TreinamentoId",
                        column: x => x.TreinamentoId,
                        principalTable: "Treinamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConclusoesTreinamento_ColaboradorId",
                table: "ConclusoesTreinamento",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_ConclusoesTreinamento_TreinamentoId_ColaboradorId",
                table: "ConclusoesTreinamento",
                columns: new[] { "TreinamentoId", "ColaboradorId" },
                unique: true);
        }
    }
}
