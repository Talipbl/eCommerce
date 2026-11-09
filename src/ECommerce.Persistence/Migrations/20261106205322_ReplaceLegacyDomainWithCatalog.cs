using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceLegacyDomainWithCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Def_Carriers",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Gnl_OrderPayments",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Gnl_OrderProducts",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Gnl_ProductCategories",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Gnl_ProductImages",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Gnl_ProductPrices",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Gnl_UserAddress",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Gnl_UserCoupons",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Gnl_UserMailQueues",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Gnl_XmlSchedulerLogs",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Sys_ErrorLogs",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Gnl_Orders",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Gnl_ProductOptions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Sys_Currencies",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Sys_VatRatios",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Def_DiscountCoupons",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Sys_MailTypes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Gnl_XmlSchedulers",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Gnl_Users",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Sys_PaymentTypes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Sys_UnitTypes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Def_SubCategories",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Gnl_Products",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Gnl_ProductXmls",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Def_Brands",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Def_Categories",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Def_Manufacturers",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Sys_StatusTypes",
                schema: "dbo");

            migrationBuilder.CreateTable(
                name: "Catalog_Brands",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Catalog_Brands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Catalog_Categories",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Catalog_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Catalog_Categories_Catalog_Categories_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "dbo",
                        principalTable: "Catalog_Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Catalog_Products",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    BrandId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    VatRate = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Catalog_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Catalog_Products_Catalog_Brands_BrandId",
                        column: x => x.BrandId,
                        principalSchema: "dbo",
                        principalTable: "Catalog_Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Catalog_Products_Catalog_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "dbo",
                        principalTable: "Catalog_Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Catalog_ProductImages",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    AltText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Catalog_ProductImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Catalog_ProductImages_Catalog_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "dbo",
                        principalTable: "Catalog_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Catalog_ProductVariants",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ListPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Attributes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Catalog_ProductVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Catalog_ProductVariants_Catalog_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "dbo",
                        principalTable: "Catalog_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Catalog_Brands_Slug",
                schema: "dbo",
                table: "Catalog_Brands",
                column: "Slug",
                unique: true,
                filter: "[DeletedDate] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Catalog_Categories_ParentId",
                schema: "dbo",
                table: "Catalog_Categories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Catalog_Categories_Slug",
                schema: "dbo",
                table: "Catalog_Categories",
                column: "Slug",
                unique: true,
                filter: "[DeletedDate] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Catalog_ProductImages_ProductId",
                schema: "dbo",
                table: "Catalog_ProductImages",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Catalog_Products_BrandId",
                schema: "dbo",
                table: "Catalog_Products",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Catalog_Products_CategoryId",
                schema: "dbo",
                table: "Catalog_Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Catalog_Products_IsActive_CategoryId",
                schema: "dbo",
                table: "Catalog_Products",
                columns: new[] { "IsActive", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Catalog_Products_Slug",
                schema: "dbo",
                table: "Catalog_Products",
                column: "Slug",
                unique: true,
                filter: "[DeletedDate] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Catalog_ProductVariants_Barcode",
                schema: "dbo",
                table: "Catalog_ProductVariants",
                column: "Barcode",
                unique: true,
                filter: "[DeletedDate] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Catalog_ProductVariants_ProductId",
                schema: "dbo",
                table: "Catalog_ProductVariants",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Catalog_ProductVariants_Sku",
                schema: "dbo",
                table: "Catalog_ProductVariants",
                column: "Sku",
                unique: true,
                filter: "[DeletedDate] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Catalog_ProductImages",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Catalog_ProductVariants",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Catalog_Products",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Catalog_Brands",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Catalog_Categories",
                schema: "dbo");

            migrationBuilder.CreateTable(
                name: "Def_Brands",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrandName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Def_Brands_pk", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Def_Carriers",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CompanyPhone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    ContractedPrice = table.Column<decimal>(type: "decimal(18,0)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsContracted = table.Column<bool>(type: "bit", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Def_Carriers_pk", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Def_Categories",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Descriptipn = table.Column<string>(type: "nvarchar(350)", maxLength: 350, nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Categories_pk", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Def_Manufacturers",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContacInfo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ManufacturersName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Def_Manufacturers_pk", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Def_SubCategories",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsParent = table.Column<bool>(type: "bit", nullable: true, defaultValueSql: "((0))"),
                    ParentId = table.Column<int>(type: "int", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Def_SubCategories_pk", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Gnl_UserAddress",
                schema: "dbo",
                columns: table => new
                {
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FixedPhoneNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Identity = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    IsCorporate = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<int>(type: "int", nullable: false, defaultValueSql: "((1))"),
                    MobilePhoneNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    TaxNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ZipCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "Sys_Currencies",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CurrencyName = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Sys_Currencies_pk", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sys_ErrorLogs",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2500)", maxLength: 2500, nullable: false),
                    MethodName = table.Column<string>(type: "nvarchar(2500)", maxLength: 2500, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Sys_ErrorLogs_pk", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sys_MailTypes",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MailTypeName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Sys_MailTypes_pk", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sys_PaymentTypes",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentName = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Sys_PaymentTypes_pk", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sys_StatusTypes",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatusName = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Sys_StatusTypes_pk", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sys_UnitTypes",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TypeName = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Sys_UnitTypeId_pk", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sys_VatRatios",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VatRatioValue = table.Column<decimal>(type: "decimal(18,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Sys_VatRatios_pk", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Gnl_Products",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrandId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    ManufacturerId = table.Column<int>(type: "int", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false, defaultValueSql: "((1))"),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EAN = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ISBN = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    JAN = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MinmumOrder = table.Column<int>(type: "int", nullable: false, defaultValueSql: "((1))"),
                    MPN = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SellerCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SellerUrl = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    StockCode = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    UPC = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    XmlId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Gnl_Products_pk", x => x.Id);
                    table.ForeignKey(
                        name: "Gnl_Products_Def_Brands_Id_fk",
                        column: x => x.BrandId,
                        principalSchema: "dbo",
                        principalTable: "Def_Brands",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "Gnl_Products_Def_Categories_Id_fk",
                        column: x => x.CategoryId,
                        principalSchema: "dbo",
                        principalTable: "Def_Categories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "Gnl_Products_Def_Manufacturers_Id_fk",
                        column: x => x.ManufacturerId,
                        principalSchema: "dbo",
                        principalTable: "Def_Manufacturers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "Gnl_Products_Sys_StatusTypes_Id_fk",
                        column: x => x.StatusId,
                        principalSchema: "dbo",
                        principalTable: "Sys_StatusTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Gnl_ProductXmls",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    AvailableDateTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    BrandTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    CategoryNameTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    CategoryTreeSplitChar = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    CategoryTreeTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CurrencyTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DescriptionTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    EANTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    HeadTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    HeightTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    ISBNTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    JANTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    LengthTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    LengthTypeTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    ManufacturerTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    MetaDescriptionTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    MetaKeywordsTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    MetaTitleTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    MPNTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    OptionTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    OptionsTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    PictureTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    PicturesTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    PriceTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    ProductCodeTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    ProductTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    PurchasePriceTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    SKUTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    StatusTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    StockQuantityTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    StockUnitTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    TitleTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    UPCTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    UrlTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    VatRatioTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    WeightTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    WidthTag = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    XmlLink = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Gnl_ProductXmls_pk", x => x.Id);
                    table.ForeignKey(
                        name: "Gnl_ProductXmls_Sys_StatusTypes_Id_fk",
                        column: x => x.StatusId,
                        principalSchema: "dbo",
                        principalTable: "Sys_StatusTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Gnl_Users",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusId = table.Column<int>(type: "int", nullable: false, defaultValueSql: "((1))"),
                    City = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FixedPhoneNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Identity = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    IsCorporate = table.Column<bool>(type: "bit", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MailAddress = table.Column<string>(type: "nvarchar(75)", maxLength: 75, nullable: false),
                    MobilePhoneNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    PasswordHash = table.Column<byte[]>(type: "varbinary(500)", maxLength: 500, nullable: false),
                    PasswordSalt = table.Column<byte[]>(type: "varbinary(500)", maxLength: 500, nullable: false),
                    TaxNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Gnl_Users_pk", x => x.Id);
                    table.ForeignKey(
                        name: "Gnl_Users_Sys_StatusTypes_Id_fk",
                        column: x => x.StatusId,
                        principalSchema: "dbo",
                        principalTable: "Sys_StatusTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Def_DiscountCoupons",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    SubCategoryId = table.Column<int>(type: "int", nullable: true),
                    CouponCode = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    CouponName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DiscountRate = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    DiscountType = table.Column<bool>(type: "bit", nullable: false),
                    MinumumOrder = table.Column<decimal>(type: "decimal(18,0)", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Def_DiscountCoupons_pk", x => x.Id);
                    table.ForeignKey(
                        name: "Def_DiscountCoupons_Def_Categories_Id_fk",
                        column: x => x.CategoryId,
                        principalSchema: "dbo",
                        principalTable: "Def_Categories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "Def_DiscountCoupons_Def_SubCategories_Id_fk",
                        column: x => x.SubCategoryId,
                        principalSchema: "dbo",
                        principalTable: "Def_SubCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "Def_DiscountCoupons_Gnl_Products_Id_fk",
                        column: x => x.ProductId,
                        principalSchema: "dbo",
                        principalTable: "Gnl_Products",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Gnl_ProductCategories",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    SubCategoryId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Gnl_ProductCategories_pk", x => x.Id);
                    table.ForeignKey(
                        name: "Gnl_ProductCategories_Def_SubCategories_Id_fk",
                        column: x => x.SubCategoryId,
                        principalSchema: "dbo",
                        principalTable: "Def_SubCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "Gnl_ProductCategories_Gnl_Products_Id_fk",
                        column: x => x.ProductId,
                        principalSchema: "dbo",
                        principalTable: "Gnl_Products",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Gnl_ProductOptions",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false, defaultValueSql: "((1))"),
                    UnitTypeId = table.Column<int>(type: "int", nullable: false),
                    AvailableDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Height = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "((1))"),
                    Length = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    OptionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StockCode = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    UnitOfStock = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    Width = table.Column<decimal>(type: "decimal(18,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Gnl_ProductOptions_pk", x => x.Id);
                    table.ForeignKey(
                        name: "Gnl_ProductOptions_Gnl_Products_Id_fk",
                        column: x => x.ProductId,
                        principalSchema: "dbo",
                        principalTable: "Gnl_Products",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "Gnl_ProductOptions_Sys_StatusTypes_Id_fk",
                        column: x => x.StatusId,
                        principalSchema: "dbo",
                        principalTable: "Sys_StatusTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "Gnl_ProductOptions_Sys_UnitTypeId_Id_fk",
                        column: x => x.UnitTypeId,
                        principalSchema: "dbo",
                        principalTable: "Sys_UnitTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Gnl_XmlSchedulers",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusId = table.Column<int>(type: "int", nullable: true),
                    XmlId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Frequency = table.Column<int>(type: "int", nullable: true),
                    FrequnecyPeriod = table.Column<int>(type: "int", nullable: true),
                    LastWorkingTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Gnl_XmlScheduleres_pk", x => x.Id);
                    table.ForeignKey(
                        name: "Gnl_XmlScheduleres_Gnl_ProductXmls_Id_fk",
                        column: x => x.XmlId,
                        principalSchema: "dbo",
                        principalTable: "Gnl_ProductXmls",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "Gnl_XmlScheduleres_Sys_StatusTypes_Id_fk",
                        column: x => x.StatusId,
                        principalSchema: "dbo",
                        principalTable: "Sys_StatusTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Gnl_Orders",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentTypeId = table.Column<int>(type: "int", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CarrierId = table.Column<int>(type: "int", nullable: true),
                    CouponId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,0)", nullable: true),
                    OrderAmount = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    OrderCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    ShipAmount = table.Column<decimal>(type: "decimal(18,0)", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    VatAmount = table.Column<decimal>(type: "decimal(18,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Gnl_Orders_pk", x => x.Id);
                    table.ForeignKey(
                        name: "Gnl_Orders_Gnl_Users_Id_fk",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "Gnl_Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "Gnl_Orders_Sys_PaymentTypes_Id_fk",
                        column: x => x.PaymentTypeId,
                        principalSchema: "dbo",
                        principalTable: "Sys_PaymentTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "Gnl_Orders_Sys_StatusTypes_Id_fk",
                        column: x => x.StatusId,
                        principalSchema: "dbo",
                        principalTable: "Sys_StatusTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Gnl_UserMailQueues",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MailTypeId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsSend = table.Column<bool>(type: "bit", nullable: false),
                    MailAddress = table.Column<string>(type: "nvarchar(75)", maxLength: 75, nullable: true),
                    NameSurname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SendDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Gnl_UserMailQueues_pk", x => x.Id);
                    table.ForeignKey(
                        name: "Gnl_UserMailQueues_Gnl_Users_Id_fk",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "Gnl_Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "Gnl_UserMailQueues_Sys_MailTypes_Id_fk",
                        column: x => x.MailTypeId,
                        principalSchema: "dbo",
                        principalTable: "Sys_MailTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Gnl_UserCoupons",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CouponId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpireDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsUsed = table.Column<bool>(type: "bit", nullable: true, defaultValueSql: "((0))"),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsedDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Gnl_UserCoupons_pk", x => x.Id);
                    table.ForeignKey(
                        name: "Gnl_UserCoupons_Def_DiscountCoupons_Id_fk",
                        column: x => x.CouponId,
                        principalSchema: "dbo",
                        principalTable: "Def_DiscountCoupons",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "Gnl_UserCoupons_Gnl_Users_Id_fk",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "Gnl_Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Gnl_ProductImages",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OptionId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Gnl_ProductImages_pk", x => x.Id);
                    table.ForeignKey(
                        name: "Gnl_ProductImages_Gnl_ProductOptions_Id_fk",
                        column: x => x.OptionId,
                        principalSchema: "dbo",
                        principalTable: "Gnl_ProductOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "Gnl_ProductImages_Gnl_Products_Id_fk",
                        column: x => x.ProductId,
                        principalSchema: "dbo",
                        principalTable: "Gnl_Products",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Gnl_ProductPrices",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    OptionId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false, defaultValueSql: "((1))"),
                    VatRatioId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DiscountExpireDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    DiscountPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountRatio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsSale = table.Column<bool>(type: "bit", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Gnl_ProductPrices_pk", x => x.Id);
                    table.ForeignKey(
                        name: "Gnl_ProductPrices_Gnl_ProductOptions_Id_fk",
                        column: x => x.OptionId,
                        principalSchema: "dbo",
                        principalTable: "Gnl_ProductOptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "Gnl_ProductPrices_Gnl_Products_Id_fk",
                        column: x => x.ProductId,
                        principalSchema: "dbo",
                        principalTable: "Gnl_Products",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "Gnl_ProductPrices_Sys_Currencies_Id_fk",
                        column: x => x.CurrencyId,
                        principalSchema: "dbo",
                        principalTable: "Sys_Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "Gnl_ProductPrices_Sys_StatusTypes_Id_fk",
                        column: x => x.StatusId,
                        principalSchema: "dbo",
                        principalTable: "Sys_StatusTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "Gnl_ProductPrices_Sys_VatRatios_Id_fk",
                        column: x => x.VatRatioId,
                        principalSchema: "dbo",
                        principalTable: "Sys_VatRatios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Gnl_XmlSchedulerLogs",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchedulereId = table.Column<int>(type: "int", nullable: false),
                    XmlId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WorkedTime = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Gnl_XmlSchedulereLogs_pk", x => x.Id);
                    table.ForeignKey(
                        name: "Gnl_XmlSchedulereLogs_Gnl_ProductXmls_Id_fk",
                        column: x => x.XmlId,
                        principalSchema: "dbo",
                        principalTable: "Gnl_ProductXmls",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "Gnl_XmlSchedulereLogs_Gnl_XmlScheduleres_Id_fk",
                        column: x => x.SchedulereId,
                        principalSchema: "dbo",
                        principalTable: "Gnl_XmlSchedulers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Gnl_OrderPayments",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CurrenciesId = table.Column<int>(type: "int", nullable: true),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    PaymentTypeId = table.Column<int>(type: "int", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,0)", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Gnl_OrderPayments_pk", x => x.Id);
                    table.ForeignKey(
                        name: "Gnl_OrderPayments_Gnl_Orders_Id_fk",
                        column: x => x.OrderId,
                        principalSchema: "dbo",
                        principalTable: "Gnl_Orders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "Gnl_OrderPayments_Sys_Currencies_Id_fk",
                        column: x => x.CurrenciesId,
                        principalSchema: "dbo",
                        principalTable: "Sys_Currencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "Gnl_OrderPayments_Sys_PaymentTypes_Id_fk",
                        column: x => x.PaymentTypeId,
                        principalSchema: "dbo",
                        principalTable: "Sys_PaymentTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "Gnl_OrderPayments_Sys_StatusTypes_Id_fk",
                        column: x => x.StatusId,
                        principalSchema: "dbo",
                        principalTable: "Sys_StatusTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Gnl_OrderProducts",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,0)", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,0)", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,0)", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VatAmount = table.Column<decimal>(type: "decimal(18,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Gnl_OrderProducts_pk", x => x.Id);
                    table.ForeignKey(
                        name: "Gnl_OrderProducts_Gnl_Orders_Id_fk",
                        column: x => x.OrderId,
                        principalSchema: "dbo",
                        principalTable: "Gnl_Orders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "Gnl_OrderProducts_Gnl_Products_Id_fk",
                        column: x => x.ProductId,
                        principalSchema: "dbo",
                        principalTable: "Gnl_Products",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Def_DiscountCoupons_CategoryId",
                schema: "dbo",
                table: "Def_DiscountCoupons",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Def_DiscountCoupons_ProductId",
                schema: "dbo",
                table: "Def_DiscountCoupons",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Def_DiscountCoupons_SubCategoryId",
                schema: "dbo",
                table: "Def_DiscountCoupons",
                column: "SubCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_OrderPayments_CurrenciesId",
                schema: "dbo",
                table: "Gnl_OrderPayments",
                column: "CurrenciesId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_OrderPayments_OrderId",
                schema: "dbo",
                table: "Gnl_OrderPayments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_OrderPayments_PaymentTypeId",
                schema: "dbo",
                table: "Gnl_OrderPayments",
                column: "PaymentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_OrderPayments_StatusId",
                schema: "dbo",
                table: "Gnl_OrderPayments",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_OrderProducts_OrderId",
                schema: "dbo",
                table: "Gnl_OrderProducts",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_OrderProducts_ProductId",
                schema: "dbo",
                table: "Gnl_OrderProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_Orders_PaymentTypeId",
                schema: "dbo",
                table: "Gnl_Orders",
                column: "PaymentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_Orders_StatusId",
                schema: "dbo",
                table: "Gnl_Orders",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_Orders_UserId",
                schema: "dbo",
                table: "Gnl_Orders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_ProductCategories_ProductId",
                schema: "dbo",
                table: "Gnl_ProductCategories",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_ProductCategories_SubCategoryId",
                schema: "dbo",
                table: "Gnl_ProductCategories",
                column: "SubCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_ProductImages_OptionId",
                schema: "dbo",
                table: "Gnl_ProductImages",
                column: "OptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_ProductImages_ProductId",
                schema: "dbo",
                table: "Gnl_ProductImages",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_ProductOptions_ProductId",
                schema: "dbo",
                table: "Gnl_ProductOptions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_ProductOptions_StatusId",
                schema: "dbo",
                table: "Gnl_ProductOptions",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_ProductOptions_UnitTypeId",
                schema: "dbo",
                table: "Gnl_ProductOptions",
                column: "UnitTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_ProductPrices_CurrencyId",
                schema: "dbo",
                table: "Gnl_ProductPrices",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_ProductPrices_OptionId",
                schema: "dbo",
                table: "Gnl_ProductPrices",
                column: "OptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_ProductPrices_ProductId",
                schema: "dbo",
                table: "Gnl_ProductPrices",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_ProductPrices_StatusId",
                schema: "dbo",
                table: "Gnl_ProductPrices",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_ProductPrices_VatRatioId",
                schema: "dbo",
                table: "Gnl_ProductPrices",
                column: "VatRatioId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_Products_BrandId",
                schema: "dbo",
                table: "Gnl_Products",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_Products_CategoryId",
                schema: "dbo",
                table: "Gnl_Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_Products_ManufacturerId",
                schema: "dbo",
                table: "Gnl_Products",
                column: "ManufacturerId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_Products_StatusId",
                schema: "dbo",
                table: "Gnl_Products",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_ProductXmls_StatusId",
                schema: "dbo",
                table: "Gnl_ProductXmls",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_UserCoupons_CouponId",
                schema: "dbo",
                table: "Gnl_UserCoupons",
                column: "CouponId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_UserCoupons_UserId",
                schema: "dbo",
                table: "Gnl_UserCoupons",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_UserMailQueues_MailTypeId",
                schema: "dbo",
                table: "Gnl_UserMailQueues",
                column: "MailTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_UserMailQueues_UserId",
                schema: "dbo",
                table: "Gnl_UserMailQueues",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_Users_StatusId",
                schema: "dbo",
                table: "Gnl_Users",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_XmlSchedulerLogs_SchedulereId",
                schema: "dbo",
                table: "Gnl_XmlSchedulerLogs",
                column: "SchedulereId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_XmlSchedulerLogs_XmlId",
                schema: "dbo",
                table: "Gnl_XmlSchedulerLogs",
                column: "XmlId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_XmlSchedulers_StatusId",
                schema: "dbo",
                table: "Gnl_XmlSchedulers",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Gnl_XmlSchedulers_XmlId",
                schema: "dbo",
                table: "Gnl_XmlSchedulers",
                column: "XmlId");
        }
    }
}
