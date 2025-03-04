using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIHelpdeskSupport.Migrations
{
    /// <inheritdoc />
    public partial class usersetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SupportEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultLanguage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimeZone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateFormat = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SessionTimeout = table.Column<int>(type: "int", nullable: false),
                    RememberSessions = table.Column<bool>(type: "bit", nullable: false),
                    Theme = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccentColor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FlowiseApiUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FlowiseApiKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultAiModel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultTemperature = table.Column<double>(type: "float", nullable: false),
                    DefaultMaxTokens = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPermissions",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PermissionName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsGranted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissions", x => new { x.UserId, x.PermissionName });
                    table.ForeignKey(
                        name: "FK_UserPermissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "UserPermissions");
        }
    }
}
