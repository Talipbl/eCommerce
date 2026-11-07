using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketplaceListings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Marketplace_BrandMappings",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Marketplace = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BrandId = table.Column<int>(type: "int", nullable: false),
                    ExternalBrandId = table.Column<long>(type: "bigint", nullable: false),
                    ExternalBrandName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marketplace_BrandMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Marketplace_BrandMappings_Catalog_Brands_BrandId",
                        column: x => x.BrandId,
                        principalSchema: "dbo",
                        principalTable: "Catalog_Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Marketplace_CategoryMappings",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Marketplace = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    ExternalCategoryId = table.Column<long>(type: "bigint", nullable: false),
                    ExternalCategoryName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marketplace_CategoryMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Marketplace_CategoryMappings_Catalog_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "dbo",
                        principalTable: "Catalog_Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Marketplace_Listings",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Marketplace = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExternalContentId = table.Column<long>(type: "bigint", nullable: true),
                    LastBatchRequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StatusMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DeliveryDuration = table.Column<int>(type: "int", nullable: true),
                    LastSubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastCheckedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProductAttributes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VariantAttributes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marketplace_Listings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Marketplace_Listings_Catalog_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "dbo",
                        principalTable: "Catalog_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Marketplace_BrandMappings_BrandId",
                schema: "dbo",
                table: "Marketplace_BrandMappings",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Marketplace_BrandMappings_Marketplace_BrandId",
                schema: "dbo",
                table: "Marketplace_BrandMappings",
                columns: new[] { "Marketplace", "BrandId" },
                unique: true,
                filter: "[DeletedDate] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Marketplace_CategoryMappings_CategoryId",
                schema: "dbo",
                table: "Marketplace_CategoryMappings",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Marketplace_CategoryMappings_Marketplace_CategoryId",
                schema: "dbo",
                table: "Marketplace_CategoryMappings",
                columns: new[] { "Marketplace", "CategoryId" },
                unique: true,
                filter: "[DeletedDate] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Marketplace_Listings_LastBatchRequestId",
                schema: "dbo",
                table: "Marketplace_Listings",
                column: "LastBatchRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Marketplace_Listings_Marketplace_ProductId",
                schema: "dbo",
                table: "Marketplace_Listings",
                columns: new[] { "Marketplace", "ProductId" },
                unique: true,
                filter: "[DeletedDate] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Marketplace_Listings_ProductId",
                schema: "dbo",
                table: "Marketplace_Listings",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Marketplace_BrandMappings",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Marketplace_CategoryMappings",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Marketplace_Listings",
                schema: "dbo");
        }
    }
}
