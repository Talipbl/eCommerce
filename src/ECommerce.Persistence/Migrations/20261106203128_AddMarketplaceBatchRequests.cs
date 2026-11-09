using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketplaceBatchRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Gnl_MarketplaceBatchRequests",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Marketplace = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BatchRequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ItemCount = table.Column<int>(type: "int", nullable: false),
                    FailedItemCount = table.Column<int>(type: "int", nullable: false),
                    LastCheckedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Failures = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gnl_MarketplaceBatchRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_MarketplaceBatchRequests_Marketplace_BatchRequestId",
                schema: "dbo",
                table: "Gnl_MarketplaceBatchRequests",
                columns: new[] { "Marketplace", "BatchRequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_MarketplaceBatchRequests_Status",
                schema: "dbo",
                table: "Gnl_MarketplaceBatchRequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Gnl_MarketplaceBatchRequests",
                schema: "dbo");
        }
    }
}
