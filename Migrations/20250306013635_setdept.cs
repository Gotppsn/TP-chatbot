using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIHelpdeskSupport.Migrations
{
    /// <inheritdoc />
    public partial class setdept : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SqlServerDatabase",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SqlServerHost",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SqlServerMultipleActiveResultSets",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SqlServerPassword",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SqlServerTrustServerCertificate",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SqlServerUsername",
                table: "SystemSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SqlServerDatabase",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SqlServerHost",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "SqlServerMultipleActiveResultSets",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SqlServerPassword",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "SqlServerTrustServerCertificate",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SqlServerUsername",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
