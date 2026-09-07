using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce522.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImgPropToBrandModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Img",
                table: "Brands",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Img",
                table: "Brands");
        }
    }
}
