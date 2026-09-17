using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IksOks.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddResumeRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResumeRequestedAt",
                table: "Matches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResumeRequestedByUserId",
                table: "Matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_ResumeRequestedByUserId",
                table: "Matches",
                column: "ResumeRequestedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Users_ResumeRequestedByUserId",
                table: "Matches",
                column: "ResumeRequestedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Users_ResumeRequestedByUserId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_ResumeRequestedByUserId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "ResumeRequestedAt",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "ResumeRequestedByUserId",
                table: "Matches");
        }
    }
}
