using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InclusiRH.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ValidacaoBilateral : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Denuncia_Anonimato",
                table: "Denuncias");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DataValidacao",
                table: "Denuncias",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JustificativaContestacao",
                table: "Denuncias",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataValidacao",
                table: "Denuncias");

            migrationBuilder.DropColumn(
                name: "JustificativaContestacao",
                table: "Denuncias");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Denuncia_Anonimato",
                table: "Denuncias",
                sql: "([IsAnonima] = 1 AND [ColaboradorId] IS NULL) OR ([IsAnonima] = 0 AND [ColaboradorId] IS NOT NULL)");
        }
    }
}
