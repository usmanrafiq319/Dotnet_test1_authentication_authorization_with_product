using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dotnet_test1_authentication_authorization_with_product.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddImageVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ImageVersion",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageVersion",
                table: "Products");
        }
    }
}
