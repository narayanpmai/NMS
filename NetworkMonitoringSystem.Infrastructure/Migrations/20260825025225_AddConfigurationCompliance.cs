using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetworkMonitoringSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurationCompliance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DesiredConfiguration",
                table: "Devices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsConfigCompliant",
                table: "Devices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ConfigurationBackups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    ConfigContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BackupDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsCompliant = table.Column<bool>(type: "bit", nullable: false),
                    ConfigVersion = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationBackups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfigurationBackups_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationBackups_DeviceId",
                table: "ConfigurationBackups",
                column: "DeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfigurationBackups");

            migrationBuilder.DropColumn(
                name: "DesiredConfiguration",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "IsConfigCompliant",
                table: "Devices");
        }
    }
}
