using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ntk.NumberPlate.Hub.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Nodes",
                columns: table => new
                {
                    NodeId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NodeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LastHeartbeat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsOnline = table.Column<bool>(type: "bit", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalDetections = table.Column<int>(type: "int", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastDetectionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nodes", x => x.NodeId);
                });

            migrationBuilder.CreateTable(
                name: "VehicleDetections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NodeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlateNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Speed = table.Column<double>(type: "float", nullable: false),
                    DetectionTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Confidence = table.Column<float>(type: "real", nullable: false),
                    VehicleType = table.Column<int>(type: "int", nullable: false),
                    VehicleColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSpeedViolation = table.Column<bool>(type: "bit", nullable: false),
                    SpeedLimit = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleDetections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDetections_DetectionTime",
                table: "VehicleDetections",
                column: "DetectionTime");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDetections_NodeId",
                table: "VehicleDetections",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDetections_PlateNumber",
                table: "VehicleDetections",
                column: "PlateNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Nodes");

            migrationBuilder.DropTable(
                name: "VehicleDetections");
        }
    }
}
