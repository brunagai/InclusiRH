using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InclusiRH.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProtecaoAutodeclaracao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Etnia",
                table: "Autodeclaracoes");

            migrationBuilder.DropColumn(
                name: "Genero",
                table: "Autodeclaracoes");

            migrationBuilder.DropColumn(
                name: "OrientacaoSexual",
                table: "Autodeclaracoes");

            migrationBuilder.DropColumn(
                name: "SituacaoPcd",
                table: "Autodeclaracoes");

            migrationBuilder.DropColumn(
                name: "TipoDeficiencia",
                table: "Autodeclaracoes");

            migrationBuilder.AddColumn<byte[]>(
                name: "DadosProtegidos",
                table: "Autodeclaracoes",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DadosProtegidos",
                table: "Autodeclaracoes");

            migrationBuilder.AddColumn<string>(
                name: "Etnia",
                table: "Autodeclaracoes",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Genero",
                table: "Autodeclaracoes",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OrientacaoSexual",
                table: "Autodeclaracoes",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SituacaoPcd",
                table: "Autodeclaracoes",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TipoDeficiencia",
                table: "Autodeclaracoes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
