using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InclusiRH.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class EvidenciasDenuncia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvidenciasDenuncia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DenunciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HistoricoTramitacaoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NomeArquivo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CaminhoArquivo = table.Column<string>(type: "varchar(260)", unicode: false, maxLength: 260, nullable: false),
                    TipoArquivo = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    TamanhoBytes = table.Column<long>(type: "bigint", nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataUpload = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenciasDenuncia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvidenciasDenuncia_Denuncias_DenunciaId",
                        column: x => x.DenunciaId,
                        principalTable: "Denuncias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EvidenciasDenuncia_HistoricosTramitacao_HistoricoTramitacaoId",
                        column: x => x.HistoricoTramitacaoId,
                        principalTable: "HistoricosTramitacao",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenciasDenuncia_CaminhoArquivo",
                table: "EvidenciasDenuncia",
                column: "CaminhoArquivo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvidenciasDenuncia_DenunciaId",
                table: "EvidenciasDenuncia",
                column: "DenunciaId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenciasDenuncia_HistoricoTramitacaoId",
                table: "EvidenciasDenuncia",
                column: "HistoricoTramitacaoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvidenciasDenuncia");
        }
    }
}
