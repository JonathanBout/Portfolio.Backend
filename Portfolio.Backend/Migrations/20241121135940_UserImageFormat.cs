using Microsoft.EntityFrameworkCore.Migrations;
using System.Net.Mime;

#nullable disable

namespace Portfolio.Backend.Migrations
{
    /// <inheritdoc />
    public partial class UserImageFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "profile_image_format",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: 1L,
                column: "profile_image_format",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "profile_image_format",
                table: "users");
        }
    }
}
