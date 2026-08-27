using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EyewearsProject.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneVerifiedToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PhoneVerified",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneVerified",
                table: "AspNetUsers");
        }
    }
}
