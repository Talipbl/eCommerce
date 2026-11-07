using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketplaceSyncModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Marketplace_InventoryOutbox_ProcessedAt",
                schema: "dbo",
                table: "Marketplace_InventoryOutbox");

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                schema: "dbo",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ExternalLineId",
                schema: "dbo",
                table: "Order_Lines",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Attempts",
                schema: "dbo",
                table: "Marketplace_InventoryOutbox",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptAt",
                schema: "dbo",
                table: "Marketplace_InventoryOutbox",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "dbo",
                table: "Marketplace_InventoryOutbox",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            // Önceki modelde işlenmiş kayıt hata mesajı varsa kalıcı hata sayılıyordu.
            migrationBuilder.Sql("""
                UPDATE [dbo].[Marketplace_InventoryOutbox]
                SET [Status] = CASE WHEN [ProcessedAt] IS NULL THEN 'Pending' WHEN [Error] IS NULL THEN 'Succeeded' ELSE 'Failed' END
                """);

            migrationBuilder.AddColumn<string>(
                name: "Attributes",
                schema: "dbo",
                table: "Marketplace_CategoryMappings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateTable(
                name: "Marketplace_OrderStatusOutbox",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Marketplace = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ExternalReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marketplace_OrderStatusOutbox", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Marketplace_OrderStatusOutbox_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "dbo",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Marketplace_SyncLogs",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Marketplace = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Succeeded = table.Column<bool>(type: "bit", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marketplace_SyncLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Marketplace_SyncStates",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Marketplace = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Job = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Cursor = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PausedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastRunAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marketplace_SyncStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Marketplace_InventoryOutbox_Status_NextAttemptAt",
                schema: "dbo",
                table: "Marketplace_InventoryOutbox",
                columns: new[] { "Status", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Marketplace_OrderStatusOutbox_OrderId",
                schema: "dbo",
                table: "Marketplace_OrderStatusOutbox",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Marketplace_OrderStatusOutbox_Status_NextAttemptAt",
                schema: "dbo",
                table: "Marketplace_OrderStatusOutbox",
                columns: new[] { "Status", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Marketplace_SyncLogs_Marketplace_Operation_Succeeded",
                schema: "dbo",
                table: "Marketplace_SyncLogs",
                columns: new[] { "Marketplace", "Operation", "Succeeded" });

            migrationBuilder.CreateIndex(
                name: "IX_Marketplace_SyncLogs_OccurredAt",
                schema: "dbo",
                table: "Marketplace_SyncLogs",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_Marketplace_SyncStates_Marketplace_Job",
                schema: "dbo",
                table: "Marketplace_SyncStates",
                columns: new[] { "Marketplace", "Job" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Marketplace_OrderStatusOutbox",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Marketplace_SyncLogs",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Marketplace_SyncStates",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "IX_Marketplace_InventoryOutbox_Status_NextAttemptAt",
                schema: "dbo",
                table: "Marketplace_InventoryOutbox");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                schema: "dbo",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ExternalLineId",
                schema: "dbo",
                table: "Order_Lines");

            migrationBuilder.DropColumn(
                name: "Attempts",
                schema: "dbo",
                table: "Marketplace_InventoryOutbox");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                schema: "dbo",
                table: "Marketplace_InventoryOutbox");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "dbo",
                table: "Marketplace_InventoryOutbox");

            migrationBuilder.DropColumn(
                name: "Attributes",
                schema: "dbo",
                table: "Marketplace_CategoryMappings");

            migrationBuilder.CreateIndex(
                name: "IX_Marketplace_InventoryOutbox_ProcessedAt",
                schema: "dbo",
                table: "Marketplace_InventoryOutbox",
                column: "ProcessedAt");
        }
    }
}
