using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIHelpdeskSupport.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalizedNamesJsonToChatbot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocalizedNamesJson",
                table: "Chatbots",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocalizedNamesJson",
                table: "Chatbots");
        }
    }
}
