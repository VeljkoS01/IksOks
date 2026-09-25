using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IksOks.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchFinishedEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchFinishedEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    WinnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDraw = table.Column<bool>(type: "boolean", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchFinishedEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchFinishedEvents_EventId",
                table: "MatchFinishedEvents",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchFinishedEvents_MatchId",
                table: "MatchFinishedEvents",
                column: "MatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchFinishedEvents");
        }
    }
}
