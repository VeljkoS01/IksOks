using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IksOks.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchMoveConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchMoves_Users_PlayerUserId",
                table: "MatchMoves");

            migrationBuilder.DropIndex(
                name: "IX_MatchMoves_MatchId",
                table: "MatchMoves");

            migrationBuilder.AlterColumn<string>(
                name: "Symbol",
                table: "MatchMoves",
                type: "character varying(1)",
                maxLength: 1,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_MatchMoves_MatchId_MoveNumber",
                table: "MatchMoves",
                columns: new[] { "MatchId", "MoveNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchMoves_MatchId_Row_Column",
                table: "MatchMoves",
                columns: new[] { "MatchId", "Row", "Column" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchMoves_Users_PlayerUserId",
                table: "MatchMoves",
                column: "PlayerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchMoves_Users_PlayerUserId",
                table: "MatchMoves");

            migrationBuilder.DropIndex(
                name: "IX_MatchMoves_MatchId_MoveNumber",
                table: "MatchMoves");

            migrationBuilder.DropIndex(
                name: "IX_MatchMoves_MatchId_Row_Column",
                table: "MatchMoves");

            migrationBuilder.AlterColumn<string>(
                name: "Symbol",
                table: "MatchMoves",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1)",
                oldMaxLength: 1);

            migrationBuilder.CreateIndex(
                name: "IX_MatchMoves_MatchId",
                table: "MatchMoves",
                column: "MatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_MatchMoves_Users_PlayerUserId",
                table: "MatchMoves",
                column: "PlayerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
