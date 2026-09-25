using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IksOks.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchMoves : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FinishedAt",
                table: "Matches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WinnerUserId",
                table: "Matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MatchMoves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Row = table.Column<int>(type: "integer", nullable: false),
                    Column = table.Column<int>(type: "integer", nullable: false),
                    MoveNumber = table.Column<int>(type: "integer", nullable: false),
                    Symbol = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchMoves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchMoves_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchMoves_Users_PlayerUserId",
                        column: x => x.PlayerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_WinnerUserId",
                table: "Matches",
                column: "WinnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchMoves_MatchId",
                table: "MatchMoves",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchMoves_PlayerUserId",
                table: "MatchMoves",
                column: "PlayerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Users_WinnerUserId",
                table: "Matches",
                column: "WinnerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Users_WinnerUserId",
                table: "Matches");

            migrationBuilder.DropTable(
                name: "MatchMoves");

            migrationBuilder.DropIndex(
                name: "IX_Matches_WinnerUserId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "FinishedAt",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "WinnerUserId",
                table: "Matches");
        }
    }
}
