using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Attendence_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class LectureMultiGrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.CreateTable(
                name: "LectureGrades",
                columns: table => new
                {
                    LectureId = table.Column<int>(type: "int", nullable: false),
                    GradeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LectureGrades", x => new { x.LectureId, x.GradeId });
                    table.ForeignKey(
                        name: "FK_LectureGrades_Grades_GradeId",
                        column: x => x.GradeId,
                        principalTable: "Grades",
                        principalColumn: "GradeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LectureGrades_Lectures_LectureId",
                        column: x => x.LectureId,
                        principalTable: "Lectures",
                        principalColumn: "LectureId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LectureGrades_GradeId",
                table: "LectureGrades",
                column: "GradeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LectureGrades");

            migrationBuilder.AddColumn<int>(
                name: "GradeId",
                table: "Lectures",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Lectures_GradeId",
                table: "Lectures",
                column: "GradeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lectures_Grades_GradeId",
                table: "Lectures",
                column: "GradeId",
                principalTable: "Grades",
                principalColumn: "GradeId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
