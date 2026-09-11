using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dotnet_test1_authentication_authorization_with_product.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddImageVersionToSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ImageVersion",
                table: "Products",
                type: "int",
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
