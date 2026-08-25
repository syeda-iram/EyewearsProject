using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EyewearsProject.Migrations
{
    /// <inheritdoc />
    public partial class AddPrescriptionUploadField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UploadedFileUrl",
                table: "Prescriptions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UploadedFileUrl",
                table: "Prescriptions");
        }
    }
}
