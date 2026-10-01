using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InclusiRH.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class EstruturaInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Colaboradores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SenhaHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Cargo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Departamento = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Papel = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DataCadastro = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Colaboradores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Treinamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Treinamentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Autodeclaracoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ColaboradorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Etnia = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Genero = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    OrientacaoSexual = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SituacaoPcd = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TipoDeficiencia = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ConsentimentoLgpd = table.Column<bool>(type: "bit", nullable: false),
                    DataConsentimento = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DataAtualizacao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Autodeclaracoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Autodeclaracoes_Colaboradores_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Denuncias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Protocolo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TipoOcorrencia = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    IsAnonima = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataCriacao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ColaboradorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Denuncias", x => x.Id);
                    table.CheckConstraint("CK_Denuncia_Anonimato", "([IsAnonima] = 1 AND [ColaboradorId] IS NULL) OR ([IsAnonima] = 0 AND [ColaboradorId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Denuncias_Colaboradores_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConclusoesTreinamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TreinamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ColaboradorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataConclusao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DataAtualizacao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "HistoricosTramitacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DenunciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParecerRH = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    EvidenciasAnexadas = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DataTramitacao = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Autor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StatusResultante = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricosTramitacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricosTramitacao_Denuncias_DenunciaId",
                        column: x => x.DenunciaId,
                        principalTable: "Denuncias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Autodeclaracoes_ColaboradorId",
                table: "Autodeclaracoes",
                column: "ColaboradorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Colaboradores_Email",
                table: "Colaboradores",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConclusoesTreinamento_ColaboradorId",
                table: "ConclusoesTreinamento",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_ConclusoesTreinamento_TreinamentoId_ColaboradorId",
                table: "ConclusoesTreinamento",
                columns: new[] { "TreinamentoId", "ColaboradorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Denuncias_ColaboradorId",
                table: "Denuncias",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_Denuncias_Protocolo",
                table: "Denuncias",
                column: "Protocolo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Denuncias_Status_DataCriacao",
                table: "Denuncias",
                columns: new[] { "Status", "DataCriacao" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricosTramitacao_DenunciaId",
                table: "HistoricosTramitacao",
                column: "DenunciaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Autodeclaracoes");

            migrationBuilder.DropTable(
                name: "ConclusoesTreinamento");

            migrationBuilder.DropTable(
                name: "HistoricosTramitacao");

            migrationBuilder.DropTable(
                name: "Treinamentos");

            migrationBuilder.DropTable(
                name: "Denuncias");

            migrationBuilder.DropTable(
                name: "Colaboradores");
        }
    }
}
