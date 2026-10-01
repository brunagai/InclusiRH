using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InclusiRH.Api.Data.Migrations
{
    /// <summary>
    /// Troca as colunas de texto do relato e da justificativa por pacotes
    /// AES-256-GCM. O SQL Server não tem a chave, então não consegue cifrar
    /// linhas antigas: com denúncias na tabela, a migration para em vez de
    /// apagar relatos. Nesse caso, os dados precisam ser migrados pela API.
    /// </summary>
    public partial class ProtecaoConteudoDenuncia : Migration
    {
        private const string BloquearSeHouverDenuncias =
            "IF EXISTS (SELECT 1 FROM [Denuncias]) " +
            "THROW 50001, 'A tabela Denuncias possui registros. Migre o conteúdo pela aplicação antes de alterar as colunas cifradas.', 1;";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(BloquearSeHouverDenuncias);

            migrationBuilder.DropColumn(
                name: "Descricao",
                table: "Denuncias");

            migrationBuilder.DropColumn(
                name: "JustificativaContestacao",
                table: "Denuncias");

            migrationBuilder.AddColumn<byte[]>(
                name: "DescricaoProtegida",
                table: "Denuncias",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "JustificativaContestacaoProtegida",
                table: "Denuncias",
                type: "varbinary(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(BloquearSeHouverDenuncias);

            migrationBuilder.DropColumn(
                name: "DescricaoProtegida",
                table: "Denuncias");

            migrationBuilder.DropColumn(
                name: "JustificativaContestacaoProtegida",
                table: "Denuncias");

            migrationBuilder.AddColumn<string>(
                name: "Descricao",
                table: "Denuncias",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "JustificativaContestacao",
                table: "Denuncias",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }
    }
}
