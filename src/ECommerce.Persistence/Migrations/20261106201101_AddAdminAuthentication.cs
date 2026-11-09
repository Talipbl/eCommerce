using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordSalt",
                schema: "dbo",
                table: "Def_Admins");

            migrationBuilder.DropColumn(
                name: "SecurityLevel",
                schema: "dbo",
                table: "Def_Admins");

            // varbinary -> nvarchar dönüşümü ALTER ile yapılamaz; eski hash formatı zaten uyumsuz olduğu için sütun yeniden oluşturulur.
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                schema: "dbo",
                table: "Def_Admins");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                schema: "dbo",
                table: "Def_Admins",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "dbo",
                table: "Def_Admins",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginCount",
                schema: "dbo",
                table: "Def_Admins",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEndDate",
                schema: "dbo",
                table: "Def_Admins",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                schema: "dbo",
                table: "Def_Admins",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Def_AdminRefreshTokens",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Def_AdminRefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Def_AdminRefreshTokens_Def_Admins_AdminId",
                        column: x => x.AdminId,
                        principalSchema: "dbo",
                        principalTable: "Def_Admins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Def_Admins_UserName",
                schema: "dbo",
                table: "Def_Admins",
                column: "UserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Def_AdminRefreshTokens_AdminId",
                schema: "dbo",
                table: "Def_AdminRefreshTokens",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Def_AdminRefreshTokens_TokenHash",
                schema: "dbo",
                table: "Def_AdminRefreshTokens",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Def_AdminRefreshTokens",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "IX_Def_Admins_UserName",
                schema: "dbo",
                table: "Def_Admins");

            migrationBuilder.DropColumn(
                name: "FailedLoginCount",
                schema: "dbo",
                table: "Def_Admins");

            migrationBuilder.DropColumn(
                name: "LockoutEndDate",
                schema: "dbo",
                table: "Def_Admins");

            migrationBuilder.DropColumn(
                name: "Role",
                schema: "dbo",
                table: "Def_Admins");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                schema: "dbo",
                table: "Def_Admins");

            migrationBuilder.AddColumn<byte[]>(
                name: "PasswordHash",
                schema: "dbo",
                table: "Def_Admins",
                type: "varbinary(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                schema: "dbo",
                table: "Def_Admins",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PasswordSalt",
                schema: "dbo",
                table: "Def_Admins",
                type: "varbinary(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SecurityLevel",
                schema: "dbo",
                table: "Def_Admins",
                type: "int",
                nullable: false,
                defaultValueSql: "((1))");
        }
    }
}
