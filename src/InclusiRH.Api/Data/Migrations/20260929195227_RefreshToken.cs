using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InclusiRH.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                table: "Colaboradores",
                type: "varchar(44)",
                unicode: false,
                maxLength: 44,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefreshTokenExpiryTime",
                table: "Colaboradores",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "Colaboradores");

            migrationBuilder.DropColumn(
                name: "RefreshTokenExpiryTime",
                table: "Colaboradores");
        }
    }
}
