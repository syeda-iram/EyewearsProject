using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EyewearsProject.Migrations
{
    /// <inheritdoc />
    public partial class AddTryOnOverlayField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TryOnOverlayImageUrl",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TryOnOverlayImageUrl",
                table: "Products");
        }
    }
}
