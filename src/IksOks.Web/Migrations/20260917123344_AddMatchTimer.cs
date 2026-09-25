using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IksOks.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchTimer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PausedTurnSecondsRemaining",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TurnDeadlineAt",
                table: "Matches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TurnDurationSeconds",
                table: "Matches",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PausedTurnSecondsRemaining",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "TurnDeadlineAt",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "TurnDurationSeconds",
                table: "Matches");
        }
    }
}
