using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIHelpdeskSupport.Migrations
{
    /// <inheritdoc />
    public partial class Newdeletehid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVisible",
                table: "ChatMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVisible",
                table: "ChatMessages");
        }
    }
}
