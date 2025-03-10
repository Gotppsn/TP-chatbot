using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIHelpdeskSupport.Migrations
{
    /// <inheritdoc />
    public partial class editdept : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessType",
                table: "Chatbots",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AllowedUsers",
                table: "Chatbots",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "Departments",
                table: "Chatbots",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessType",
                table: "Chatbots");

            migrationBuilder.DropColumn(
                name: "AllowedUsers",
                table: "Chatbots");

            migrationBuilder.DropColumn(
                name: "Departments",
                table: "Chatbots");
        }
    }
}
