using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JuniperFox.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ListSharing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_list_shares",
                columns: table => new
                {
                    ProductListId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SharedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_list_shares", x => new { x.ProductListId, x.UserId });
                    table.ForeignKey(
                        name: "FK_product_list_shares_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_list_shares_product_lists_ProductListId",
                        column: x => x.ProductListId,
                        principalTable: "product_lists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_list_shares_UserId",
                table: "product_list_shares",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_list_shares");
        }
    }
}
