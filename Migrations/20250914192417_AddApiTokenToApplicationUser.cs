using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory_Management_Requirements.Migrations
{
    /// <inheritdoc />
    public partial class AddApiTokenToApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiToken",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiToken",
                table: "AspNetUsers");
        }
    }
}
