using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IksOks.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddBordersAndMatchVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActiveBorderKey",
                table: "Users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JoinCode",
                table: "Matches",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                table: "Matches",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Public");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_JoinCode",
                table: "Matches",
                column: "JoinCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Matches_JoinCode",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "ActiveBorderKey",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "JoinCode",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "Matches");
        }
    }
}
