using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendence_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGradeToLecture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GradeId",
                table: "Lectures",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lectures_GradeId",
                table: "Lectures",
                column: "GradeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lectures_Grades_GradeId",
                table: "Lectures",
                column: "GradeId",
                principalTable: "Grades",
                principalColumn: "GradeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lectures_Grades_GradeId",
                table: "Lectures");

            migrationBuilder.DropIndex(
                name: "IX_Lectures_GradeId",
                table: "Lectures");

            migrationBuilder.DropColumn(
                name: "GradeId",
                table: "Lectures");
        }
    }
}
