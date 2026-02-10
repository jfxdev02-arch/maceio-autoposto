using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MaceioBot.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Respondents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PushName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FirstContactAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CurrentStep = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FrequencyAnswer = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ConvenienceAnswer = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    FuelAnswer = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    RatingAnswer = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    LuckyNumber = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Respondents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Respondents_LuckyNumber",
                table: "Respondents",
                column: "LuckyNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Respondents_PhoneNumber",
                table: "Respondents",
                column: "PhoneNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Respondents");
        }
    }
}
