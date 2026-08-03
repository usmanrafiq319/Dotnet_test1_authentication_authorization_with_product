using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dotnet_test1_authentication_authorization_with_product.Migrations
{
    /// <inheritdoc />
    public partial class AddAiChatSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Mode",
                table: "Conversations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SenderType",
                table: "ChatMessages",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Mode",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "SenderType",
                table: "ChatMessages");
        }
    }
}
