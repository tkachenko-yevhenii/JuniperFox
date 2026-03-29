using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JuniperFox.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductListItemIsPurchased : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPurchased",
                table: "product_list_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPurchased",
                table: "product_list_items");
        }
    }
}
