using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendence_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateQRTokenIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Students_QRToken",
                table: "Students");

            migrationBuilder.CreateIndex(
                name: "IX_Students_QRToken_TenantId",
                table: "Students",
                columns: new[] { "QRToken", "TenantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Students_QRToken_TenantId",
                table: "Students");

            migrationBuilder.CreateIndex(
                name: "IX_Students_QRToken",
                table: "Students",
                column: "QRToken",
                unique: true);
        }
    }
}
