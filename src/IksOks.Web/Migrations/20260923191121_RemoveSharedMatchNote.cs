using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IksOks.Web.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSharedMatchNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SharedNote",
                table: "Matches");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SharedNote",
                table: "Matches",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");
        }
    }
}
