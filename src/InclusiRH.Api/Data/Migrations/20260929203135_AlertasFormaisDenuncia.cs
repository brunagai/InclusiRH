using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InclusiRH.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AlertasFormaisDenuncia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DataAlertaContestacao",
                table: "Denuncias",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DataAlertaPrazo",
                table: "Denuncias",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "VersaoLinha",
                table: "Denuncias",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataAlertaContestacao",
                table: "Denuncias");

            migrationBuilder.DropColumn(
                name: "DataAlertaPrazo",
                table: "Denuncias");

            migrationBuilder.DropColumn(
                name: "VersaoLinha",
                table: "Denuncias");
        }
    }
}
