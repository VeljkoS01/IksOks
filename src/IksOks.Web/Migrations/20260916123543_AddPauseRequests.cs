using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IksOks.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPauseRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PauseRequestedAt",
                table: "Matches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PauseRequestedByUserId",
                table: "Matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_PauseRequestedByUserId",
                table: "Matches",
                column: "PauseRequestedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Users_PauseRequestedByUserId",
                table: "Matches",
                column: "PauseRequestedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Users_PauseRequestedByUserId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_PauseRequestedByUserId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "PauseRequestedAt",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "PauseRequestedByUserId",
                table: "Matches");
        }
    }
}
