using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaceioBot.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceAndWhatsappSentAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Respondents",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "web");

            migrationBuilder.AddColumn<DateTime>(
                name: "WhatsappSentAt",
                table: "Respondents",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Source",
                table: "Respondents");

            migrationBuilder.DropColumn(
                name: "WhatsappSentAt",
                table: "Respondents");
        }
    }
}
