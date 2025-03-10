using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIHelpdeskSupport.Migrations
{
    /// <inheritdoc />
    public partial class UpdateHistory2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatbotUpdate_Chatbots_ChatbotId",
                table: "ChatbotUpdate");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatbotUpdate",
                table: "ChatbotUpdate");

            migrationBuilder.RenameTable(
                name: "ChatbotUpdate",
                newName: "ChatbotUpdates");

            migrationBuilder.RenameIndex(
                name: "IX_ChatbotUpdate_ChatbotId",
                table: "ChatbotUpdates",
                newName: "IX_ChatbotUpdates_ChatbotId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatbotUpdates",
                table: "ChatbotUpdates",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatbotUpdates_Chatbots_ChatbotId",
                table: "ChatbotUpdates",
                column: "ChatbotId",
                principalTable: "Chatbots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatbotUpdates_Chatbots_ChatbotId",
                table: "ChatbotUpdates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatbotUpdates",
                table: "ChatbotUpdates");

            migrationBuilder.RenameTable(
                name: "ChatbotUpdates",
                newName: "ChatbotUpdate");

            migrationBuilder.RenameIndex(
                name: "IX_ChatbotUpdates_ChatbotId",
                table: "ChatbotUpdate",
                newName: "IX_ChatbotUpdate_ChatbotId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatbotUpdate",
                table: "ChatbotUpdate",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatbotUpdate_Chatbots_ChatbotId",
                table: "ChatbotUpdate",
                column: "ChatbotId",
                principalTable: "Chatbots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
